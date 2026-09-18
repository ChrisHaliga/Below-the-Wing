using System.Collections.Generic;
using BelowTheWing.Cargo;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    [RequireComponent(typeof(Bag))]
    [DisallowMultipleComponent]
    public sealed class CargoMotion : MotionReplication, IMovedFromHere
    {
        [SerializeField, Tooltip("Wait before asking again, s")]
        float m_AskAgainAfterSeconds = 0.5f;

        [SerializeField, Tooltip("At rest below this speed relative to what it lies on, m/s")]
        float m_AtRestBelow = 0.25f;

        [SerializeField, Tooltip("How far below the underside to look, m")]
        float m_UndersideReachMetres = 0.1f;

        [SerializeField, Tooltip("Tallest stack still counted as on the vehicle, m")]
        float m_StackReachMetres = 2f;

        [SerializeField, Tooltip("Still below this speed, m/s")]
        float m_StillBelow = 0.05f;

        [SerializeField, Tooltip("Still below this rate of turn, rad/s")]
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

        bool SomebodyAskedForIt(ulong who) => !HeldHere();

        void Answered(NetworkObject.OwnershipRequestResponseStatus status)
        {
            if (status == NetworkObject.OwnershipRequestResponseStatus.Denied)
            {
                PriseOff();
            }
        }

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
            if (Body == null || !IsSpawned)
            {
                return;
            }

            var ours = IsOwner;
            var us = NetworkManager.LocalClientId;

            var shouldBe = CargoOwnership.WhoShouldOwn(
                OwnerClientId, us, HeldHere(), ours ? RestingOnSomethingOwnedBy() : Shift.Nobody);

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

        ulong RestingOnSomethingOwnedBy()
        {
            var touching = m_Box.bounds.extents.y + m_UndersideReachMetres;
            var found = Physics.RaycastNonAlloc(
                Body.position, Vector3.down, m_Below, m_StackReachMetres, ~0, QueryTriggerInteraction.Ignore);

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
                return Shift.Nobody;
            }

            var relative = Body.linearVelocity - beneath.Value.rigidbody.GetPointVelocity(beneath.Value.point);
            return relative.magnitude > m_AtRestBelow ? Shift.Nobody : vehicle.OwnerClientId;
        }

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
