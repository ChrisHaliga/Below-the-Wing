using System.Collections.Generic;
using BelowTheWing.Cargo;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>Where something being carried is riding, as its owner reports it.</summary>
    public struct RideReport : INetworkSerializable
    {
        /// <summary>Whether it is riding on anything at all.</summary>
        public bool Riding;

        /// <summary>The networked object the carrier belongs to.</summary>
        public ulong CarrierObject;

        /// <summary>Which carrier on that object, as numbered by <see cref="CarrierSlots"/>.</summary>
        public int CarrierSlot;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Riding);
            serializer.SerializeValue(ref CarrierObject);
            serializer.SerializeValue(ref CarrierSlot);
        }

        /// <summary>
        /// Whether these two reports say the same thing.
        ///
        /// Two reports of riding nothing are the same report whatever else they hold. A variable
        /// nobody has written yet reads as all zeroes, and treating that as different from "riding
        /// nothing" would have every machine conclude, on the first step of every bag's life, that
        /// somebody here had just picked it up.
        /// </summary>
        public bool Matches(RideReport other)
        {
            if (!Riding || !other.Riding)
            {
                return Riding == other.Riding;
            }

            return CarrierObject == other.CarrierObject && CarrierSlot == other.CarrierSlot;
        }

        /// <summary>Loose in the world, riding on nothing.</summary>
        public static RideReport Nothing => new RideReport { Riding = false, CarrierSlot = CarrierSlots.None };
    }

    /// <summary>
    /// Keeping every machine's copy of one piece of cargo in the same place: riding on the same
    /// thing, or loose and moving the same way.
    ///
    /// Cargo has two quite different lives and needs both replicated. While it rides on something it
    /// has no motion of its own -- it is part of a cart or a pair of hands, and all that has to
    /// agree is which thing it is part of. While it is loose it is an ordinary rigidbody tumbling
    /// across the apron, and what has to agree is where it is and how fast.
    ///
    /// One machine decides and everybody else follows. Letting each machine work out for itself
    /// whether a bag has been shaken off a cart produces the worst outcome available: the bag flies
    /// on one screen and rides on for another ten seconds on the next, and the two never reconcile.
    /// A bag that comes off two hundred milliseconds late everywhere is a far smaller problem, and
    /// it is the one this takes.
    ///
    /// The machine that decides is whichever one owns the cargo, and that moves about. Picking a bag
    /// up happens immediately on the machine of whoever reached for it -- waiting for a round trip
    /// before the bag leaves the ground would make the apron feel like treacle -- and ownership is
    /// asked for in the same moment. Until it is granted, this machine keeps asking, so a refusal or
    /// an answer that never comes cannot leave somebody holding a bag nobody else agrees they hold.
    /// </summary>
    [RequireComponent(typeof(Carried))]
    [DisallowMultipleComponent]
    public sealed class CargoMotion : MotionReplication
    {
        [SerializeField, Tooltip("Seconds before asking again for cargo somebody here has taken hold of.")]
        float m_AskAgainAfterSeconds = 0.5f;

        readonly NetworkVariable<RideReport> m_Ride =
            new NetworkVariable<RideReport>(default, NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        readonly List<Carrier> m_Scratch = new List<Carrier>();

        Carried m_Carried;
        SettlesOntoCarriers m_Settling;

        RideReport m_LastApplied;
        float m_SinceLastAsked;
        bool m_Asking;

        void Awake()
        {
            m_Carried = GetComponent<Carried>();
            m_Settling = GetComponent<SettlesOntoCarriers>();
            m_LastApplied = RideReport.Nothing;
        }

        protected override Rigidbody Body => m_Carried.Body;

        /// <summary>
        /// Zero while it rides on something, because then it is not being steered at all -- it is
        /// wherever the thing carrying it has taken it.
        /// </summary>
        public override float MetresOutOfPlace => m_Carried.Attached ? 0f : base.MetresOutOfPlace;

        void FixedUpdate()
        {
            if (Body == null)
            {
                return;
            }

            // Read afresh every step rather than caught when it changes, for the same reason
            // vehicles and characters do: an object spawned elsewhere runs its spawn callback before
            // ownership has been applied, and no later change event arrives to correct it.
            var ours = IsOwner;

            if (m_Settling != null)
            {
                // Only the deciding machine judges that a bag has come to rest on a deck. Every
                // machine judging separately is how the same bag ends up aboard different carts.
                m_Settling.OursToDecide = ours;
            }

            if (ours)
            {
                m_Asking = false;
                m_LastApplied = WhatItIsActuallyRiding();

                // Only when it has actually changed. What a bag is riding on changes a handful of
                // times a minute; writing it every step would put fifty messages a second on the
                // wire for every bag on the apron to say nothing at all.
                if (!m_Ride.Value.Matches(m_LastApplied))
                {
                    m_Ride.Value = m_LastApplied;
                }

                // Riding cargo has no motion worth sending. It is wherever the cart took it, and
                // the cart already reports that -- sending it again would spend bandwidth saying the
                // same thing twice and give the two reports a chance to disagree.
                if (!m_Carried.Attached)
                {
                    Report();
                }

                return;
            }

            Follow();
        }

        /// <summary>
        /// Take what the owner says -- or, if somebody on this machine has taken hold of it, ask to
        /// be given it so that this machine's version becomes the one everybody follows.
        /// </summary>
        void Follow()
        {
            var said = m_Ride.Value;

            if (!said.Matches(m_LastApplied))
            {
                Apply(said);
                m_LastApplied = said;
                m_Asking = false;
            }

            if (!WhatItIsActuallyRiding().Matches(m_LastApplied))
            {
                KeepAskingForIt();
                return;
            }

            KeepUp();
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

        /// <summary>Puts this copy onto whatever the report names, or takes it off.</summary>
        void Apply(RideReport said)
        {
            if (!said.Riding)
            {
                // Shaken loose rather than simply unparented, so this copy takes the same moment of
                // grace before settling again that the deciding machine took. A copy free to settle
                // immediately re-attaches to the deck the bag has just left.
                m_Carried.Wake(Time.time);
                return;
            }

            var carrier = CarrierSlots.Resolve(NetworkManager, said.CarrierObject, said.CarrierSlot, m_Scratch);
            if (carrier == null)
            {
                // Whatever it rides on has not been spawned here yet. Leaving it loose for now is
                // right: nothing has been applied, so this will be tried again next step, and it
                // will keep being tried until the cart arrives.
                return;
            }

            if (m_Carried.On == carrier)
            {
                return;
            }

            if (m_Carried.Attached)
            {
                m_Carried.Wake(Time.time);
            }

            m_Carried.AttachTo(carrier);
        }

        RideReport WhatItIsActuallyRiding()
        {
            if (!m_Carried.Attached)
            {
                return RideReport.Nothing;
            }

            var networked = m_Carried.On.GetComponentInParent<NetworkObject>();
            var slot = CarrierSlots.SlotOf(m_Carried.On, m_Scratch);

            if (networked == null || slot == CarrierSlots.None)
            {
                // Riding something that exists only on this machine -- scenery, or a test rig. There
                // is nothing to say about it that another machine could act on.
                return RideReport.Nothing;
            }

            return new RideReport
            {
                Riding = true,
                CarrierObject = networked.NetworkObjectId,
                CarrierSlot = slot
            };
        }

    }
}
