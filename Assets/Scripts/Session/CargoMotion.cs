using System.Collections.Generic;
using BelowTheWing.Cargo;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// Keeping every machine's copy of one bag where the machine simulating it says it is, and
    /// moving that job to whichever machine ought to have it.
    ///
    /// A bag is an ordinary body everywhere. It is never attached to anything and never told where
    /// it is; it is pulled about by joints, carried by friction, and thrown by having a velocity.
    /// What the network carries is the bag's motion, as reported by the one machine simulating it,
    /// and the copies elsewhere are steered toward that with force like every other body.
    ///
    /// Which machine simulates it follows <see cref="CargoOwnership"/>: whoever takes hold of it,
    /// or whoever owns the cart it comes to rest on. Taking hold happens on the machine of the
    /// person reaching -- waiting for a round trip before the bag leaves the ground would make the
    /// apron feel like treacle -- and ownership is asked for in the same moment and kept being
    /// asked for until it arrives. Handing a bag to a cart's owner is the owner's decision alone,
    /// because only the owner's physics knows what the bag is resting on.
    ///
    /// A bag at rest says so once and then falls silent, so that forty parked bags cost nothing on
    /// the wire. Being still is something a bag has to keep doing, though, rather than a state it
    /// enters: nothing here lets one be put to sleep, because a sleeping body is frozen in whatever
    /// pose it had when it went quiet -- including one it was halfway through falling out of a cart.
    ///
    /// Two players reaching for one bag in the same instant is settled by whoever is granted it
    /// first: the machine simulating a bag refuses to hand it over while its own hand holds it, and
    /// a machine whose request is refused prises its own hand off. Without that, both keep asking
    /// and the bag jitters between two screens for as long as neither lets go.
    /// </summary>
    [RequireComponent(typeof(Bag))]
    [DisallowMultipleComponent]
    public sealed class CargoMotion : MotionReplication, IMovedFromHere
    {
        [SerializeField, Tooltip("Seconds before asking again for a bag somebody here has hold of.")]
        float m_AskAgainAfterSeconds = 0.5f;

        [SerializeField, Tooltip("Below this speed relative to whatever it is lying on, a bag counts " +
                                 "as at rest on it, in metres per second.")]
        float m_AtRestBelow = 0.25f;

        [SerializeField, Tooltip("How far below a bag's underside to look for what it is lying on, " +
                                 "in metres.")]
        float m_UndersideReachMetres = 0.1f;

        [SerializeField, Tooltip("How tall a stack of bags may be and still count as lying on the " +
                                 "vehicle at the bottom of it, in metres.")]
        float m_StackReachMetres = 2f;

        [SerializeField, Tooltip("Below this speed a bag is standing still and stops being news, in " +
                                 "metres per second.")]
        float m_StillBelow = 0.05f;

        [SerializeField, Tooltip("Below this rate of turn a bag is standing still, in radians per second.")]
        float m_StillSpinBelow = 0.05f;

        readonly List<Joint> m_Joints = new List<Joint>();
        readonly RaycastHit[] m_Below = new RaycastHit[8];

        Bag m_Bag;
        Collider m_Box;
        bool m_WasMoving;
        bool m_Asking;
        float m_SinceLastAsked;

        void Awake()
        {
            m_Bag = GetComponent<Bag>();
            m_Box = GetComponent<Collider>();
        }

        protected override Rigidbody Body => m_Bag.Body;

        /// <summary>
        /// Whether this machine is the one that says where this bag goes.
        ///
        /// Asked by anything that needs to know whether it is looking at the real bag or a copy of
        /// somebody else's, without caring that it is a bag -- which is how what a player sees knows
        /// not to trail a body this machine is simulating.
        /// </summary>
        public bool OursToMove => IsOwner;

        public override void OnNetworkSpawn()
        {
            NetworkObject.OnOwnershipRequested = SomebodyAskedForIt;
            NetworkObject.OnOwnershipRequestResponse += Answered;
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkObject != null)
            {
                NetworkObject.OnOwnershipRequested = null;
                NetworkObject.OnOwnershipRequestResponse -= Answered;
            }
        }

        /// <summary>Asked only on the machine that owns the bag: a hand here keeps it.</summary>
        bool SomebodyAskedForIt(ulong who) => !HeldHere();

        /// <summary>
        /// Only an outright refusal means somebody else has it. A request that arrives while
        /// another is in flight, or while the bag is locked mid-transfer, is answered with a status
        /// of its own and simply asked again later -- this machine may yet be the one granted it.
        /// </summary>
        void Answered(NetworkObject.OwnershipRequestResponseStatus status)
        {
            if (status == NetworkObject.OwnershipRequestResponseStatus.Denied)
            {
                PriseOff();
            }
        }

        /// <summary>
        /// Takes this machine's hands off the bag. Every joint reaching a body this machine moves
        /// is destroyed, and the hands notice their joint has gone.
        /// </summary>
        void PriseOff()
        {
            GetComponents(m_Joints);

            foreach (var joint in m_Joints)
            {
                var to = joint.connectedBody;
                if (to != null && to.TryGetComponent<IMovedFromHere>(out var mover) && mover.OursToMove)
                {
                    Destroy(joint);
                }
            }
        }

        void FixedUpdate()
        {
            // Nothing to decide before spawn: there is no session to ask which machine this is.
            if (Body == null || !IsSpawned)
            {
                return;
            }

            // Read afresh every step rather than caught when it changes, for the same reason
            // vehicles and characters do: an object spawned elsewhere runs its spawn callback before
            // ownership has been applied, and no later change event arrives to correct it.
            var ours = IsOwner;
            var us = NetworkManager.LocalClientId;

            var shouldBe = CargoOwnership.WhoShouldOwn(
                OwnerClientId, us, HeldHere(), ours ? RestingOnSomethingOwnedBy() : CargoOwnership.Nobody);

            if (ours)
            {
                m_Asking = false;

                if (shouldBe != us)
                {
                    NetworkObject.ChangeOwnership(shouldBe);
                    return;
                }

                SayWhereItIs();
                return;
            }

            if (shouldBe == us)
            {
                KeepAskingForIt();
                return;
            }

            KeepUp();
        }

        /// <summary>
        /// Whether a hand on this machine has hold of this bag: there is a joint on it that reaches
        /// a body this machine moves.
        /// </summary>
        bool HeldHere()
        {
            GetComponents(m_Joints);

            foreach (var joint in m_Joints)
            {
                var to = joint.connectedBody;
                if (to != null && to.TryGetComponent<IMovedFromHere>(out var mover) && mover.OursToMove)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The owner of the vehicle this bag is lying still on, if it is lying still on one. Other
        /// bags between this one and the vehicle are looked through, so that a stack on a cart
        /// ends up on one machine -- and a stack on the tarmac stays where it was.
        /// </summary>
        ulong RestingOnSomethingOwnedBy()
        {
            var touching = m_Box.bounds.extents.y + m_UndersideReachMetres;
            var found = Physics.RaycastNonAlloc(
                Body.position, Vector3.down, m_Below, m_StackReachMetres, ~0, QueryTriggerInteraction.Ignore);

            // Whatever is directly underneath, and the first thing under it that is not a bag.
            var nearest = float.MaxValue;
            RaycastHit? beneath = null;
            for (var i = 0; i < found; i++)
            {
                var hit = m_Below[i];
                nearest = Mathf.Min(nearest, hit.distance);

                var isABag = hit.rigidbody != null && hit.rigidbody.TryGetComponent<CargoMotion>(out _);
                if (!isABag && (beneath == null || hit.distance < beneath.Value.distance))
                {
                    beneath = hit;
                }
            }

            if (nearest > touching || beneath == null || beneath.Value.rigidbody == null
                || !beneath.Value.rigidbody.TryGetComponent<VehicleMotion>(out var vehicle))
            {
                return CargoOwnership.Nobody;
            }

            var relative = Body.linearVelocity - beneath.Value.rigidbody.GetPointVelocity(beneath.Value.point);
            return relative.magnitude > m_AtRestBelow ? CargoOwnership.Nobody : vehicle.OwnerClientId;
        }

        /// <summary>
        /// Reports while moving, and once more on coming to rest so the last word is where it
        /// stopped.
        ///
        /// Moving is measured rather than taken from whether physics has put the body to sleep,
        /// because bags are never put to sleep: a sleeping body is frozen in whatever pose it had
        /// when it went quiet, including one it was halfway through falling out of. Forty still
        /// bags cost nothing on the wire either way -- what matters is that they are still because
        /// nothing is moving them, not because they were stopped.
        /// </summary>
        void SayWhereItIs()
        {
            var moving = Body.linearVelocity.magnitude > m_StillBelow
                         || Body.angularVelocity.magnitude > m_StillSpinBelow;

            if (moving)
            {
                Report();
            }
            else if (m_WasMoving)
            {
                ReportNow();
            }

            m_WasMoving = moving;
        }

        /// <summary>
        /// Asks for ownership, and keeps asking until this machine has it.
        ///
        /// A refusal and an answer that never comes look the same from here, and both are handled by
        /// asking again. Asking once is what leaves two players who grabbed the same bag in the same
        /// instant with one of them holding something no other machine agrees they hold.
        /// </summary>
        void KeepAskingForIt()
        {
            if (m_Asking)
            {
                m_SinceLastAsked += Time.fixedDeltaTime;
                if (m_SinceLastAsked < m_AskAgainAfterSeconds)
                {
                    return;
                }
            }

            m_Asking = true;
            m_SinceLastAsked = 0f;

            NetworkObject.RequestOwnership();
        }
    }
}
