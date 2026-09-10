using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Apron
{
    /// <summary>
    /// What a spawned object is, what it is called, and which train it belongs to.
    ///
    /// Every machine in a session needs all of this, not just the one that built the apron. A
    /// client that joins later receives a vehicle as a bare networked object and has to be told
    /// which profile to configure it from, what to write on its label, and which train it is part
    /// of -- because a profile is a local asset and cannot be sent over the wire, and because a
    /// machine that does not know a tractor is towing four carts will happily take the tractor and
    /// drive off without them.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class RampObject : NetworkBehaviour
    {
        /// <summary>Which kind of equipment this is.</summary>
        public enum Kind
        {
            Tractor = 0,
            Cart = 1,
            Aircraft = 2,
            Crew = 3
        }

        /// <summary>Given as the train index for anything that is not part of a train.</summary>
        public const int NoTrain = -1;

        readonly NetworkVariable<int> m_Kind = new NetworkVariable<int>(
            writePerm: NetworkVariableWritePermission.Owner);

        readonly NetworkVariable<FixedString64Bytes> m_DisplayName = new NetworkVariable<FixedString64Bytes>(
            writePerm: NetworkVariableWritePermission.Owner);

        readonly NetworkVariable<int> m_TrainIndex = new NetworkVariable<int>(
            NoTrain, writePerm: NetworkVariableWritePermission.Owner);

        readonly NetworkVariable<int> m_PlaceInTrain = new NetworkVariable<int>(
            writePerm: NetworkVariableWritePermission.Owner);

        RampSpawner m_Apron;

        /// <summary>What this object is called, on its label and in the prompt to drive it.</summary>
        public string DisplayName => m_DisplayName.Value.ToString();

        /// <summary>Which kind of equipment this is.</summary>
        public Kind EquipmentKind => (Kind)m_Kind.Value;

        /// <summary>
        /// Which train this belongs to, or <see cref="NoTrain"/>. Trains are identified by number
        /// rather than by object reference because the identity has to survive being sent to a
        /// machine that has not received the other members yet.
        /// </summary>
        public int TrainIndex => m_TrainIndex.Value;

        /// <summary>Where in its train this sits: 0 is the tractor, 1 the first cart, and so on.</summary>
        public int PlaceInTrain => m_PlaceInTrain.Value;

        /// <summary>
        /// Says what this object is. Called by whichever machine spawned it, before anybody else
        /// has had a chance to look at it.
        /// </summary>
        public void Describe(Kind kind, string displayName, int trainIndex = NoTrain, int placeInTrain = 0)
        {
            m_Kind.Value = (int)kind;
            m_DisplayName.Value = displayName;
            m_TrainIndex.Value = trainIndex;
            m_PlaceInTrain.Value = placeInTrain;
        }

        public override void OnNetworkSpawn()
        {
            m_Apron = FindAnyObjectByType<RampSpawner>();

            // The values arrive with the spawn for a late joiner and a moment afterwards for
            // everybody else, so both paths have to end up here.
            m_DisplayName.OnValueChanged += OnDescriptionChanged;
            m_TrainIndex.OnValueChanged += OnDescriptionChanged;

            m_Apron?.Adopt(this);
        }

        public override void OnNetworkDespawn()
        {
            m_DisplayName.OnValueChanged -= OnDescriptionChanged;
            m_TrainIndex.OnValueChanged -= OnDescriptionChanged;

            m_Apron?.Abandon(this);
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            // Which machine holds a train decides which machine holds its couplings, so a change of
            // hands has to be noticed by the apron rather than only by the netcode layer.
            m_Apron?.OwnershipMoved();
        }

        /// <summary>
        /// Gives this object the appearance that goes with what it is: a primitive of the right
        /// real-world size, and its name floating above it.
        /// </summary>
        public void Dress()
        {
            var called = DisplayName;
            if (string.IsNullOrEmpty(called))
            {
                return;
            }

            name = called;

            if (transform.Find(GreyboxShape.ShapeName) == null)
            {
                DrawGreybox();
            }

            if (GetComponentInChildren<WorldLabel>() == null)
            {
                WorldLabel.Attach(transform, called, LabelHeightMetres());
            }
        }

        void OnDescriptionChanged<T>(T _, T __) => m_Apron?.Adopt(this);

        void DrawGreybox()
        {
            switch (EquipmentKind)
            {
                case Kind.Aircraft:
                    var aircraft = GetComponent<AircraftBody>();
                    if (aircraft != null && aircraft.Profile != null)
                    {
                        GreyboxShape.AttachLyingCapsule(
                            transform,
                            aircraft.Profile.lengthMetres,
                            aircraft.Profile.fuselageDiameterMetres,
                            new Color(0.82f, 0.82f, 0.85f));
                    }

                    break;

                case Kind.Crew:
                    var crew = GetComponent<CrewCharacter>();
                    if (crew != null && crew.Profile != null)
                    {
                        GreyboxShape.AttachCapsule(
                            transform,
                            crew.Profile.heightMetres,
                            crew.Profile.radiusMetres * 2f,
                            new Color(0.95f, 0.75f, 0.15f));
                    }

                    break;

                default:
                    var vehicle = GetComponent<VehicleController>();
                    if (vehicle != null && vehicle.Profile != null)
                    {
                        GreyboxShape.AttachBox(
                            transform,
                            vehicle.Profile.bodySizeMetres,
                            EquipmentKind == Kind.Tractor
                                ? new Color(0.35f, 0.55f, 0.75f)
                                : new Color(0.55f, 0.55f, 0.58f));
                    }

                    break;
            }
        }

        float LabelHeightMetres()
        {
            switch (EquipmentKind)
            {
                case Kind.Aircraft:
                    var aircraft = GetComponent<AircraftBody>();
                    return aircraft != null && aircraft.Profile != null
                        ? aircraft.Profile.fuselageDiameterMetres
                        : 2f;

                case Kind.Crew:
                    var crew = GetComponent<CrewCharacter>();
                    return crew != null && crew.Profile != null ? crew.Profile.heightMetres * 0.7f : 1.4f;

                default:
                    var vehicle = GetComponent<VehicleController>();
                    return vehicle != null && vehicle.Profile != null
                        ? vehicle.Profile.bodySizeMetres.y * 0.7f
                        : 1.4f;
            }
        }
    }
}
