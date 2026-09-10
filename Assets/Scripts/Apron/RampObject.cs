using BelowTheWing.Vehicles;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Apron
{
    /// <summary>
    /// What kind of equipment a spawned object is, and what it is called.
    ///
    /// Every machine in a session needs this, not just the one that built the apron. A client that
    /// joins later receives a vehicle as a bare networked object and has to be told which profile
    /// to configure it from and what to write on its label, because a profile is a local asset and
    /// cannot itself be sent over the wire.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class RampObject : NetworkBehaviour
    {
        /// <summary>How far above a vehicle's origin its name floats, in metres.</summary>
        const float LabelHeightMetres = 1.6f;

        /// <summary>Which kind of equipment this is. Indexes the spawner's catalogue.</summary>
        public enum Kind
        {
            Tractor = 0,
            Cart = 1,
            Aircraft = 2,
            Crew = 3
        }

        readonly NetworkVariable<int> m_Kind = new NetworkVariable<int>(
            writePerm: NetworkVariableWritePermission.Owner);

        readonly NetworkVariable<FixedString64Bytes> m_DisplayName = new NetworkVariable<FixedString64Bytes>(
            writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>What this object is called, on its label and in the prompt to drive it.</summary>
        public string DisplayName => m_DisplayName.Value.ToString();

        /// <summary>Which kind of equipment this is.</summary>
        public Kind EquipmentKind => (Kind)m_Kind.Value;

        /// <summary>
        /// Says what this object is. Called by whichever machine spawned it, before anybody else
        /// has had a chance to look at it.
        /// </summary>
        public void Describe(Kind kind, string displayName)
        {
            m_Kind.Value = (int)kind;
            m_DisplayName.Value = displayName;
        }

        public override void OnNetworkSpawn()
        {
            // Late joiners receive the values as part of the spawn, and everyone else sees them
            // change a moment after. Both paths end up here.
            m_DisplayName.OnValueChanged += (_, _) => Dress();
            Dress();
        }

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
                    var crew = GetComponent<Crew.CrewCharacter>();
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

        float LabelAt()
        {
            switch (EquipmentKind)
            {
                case Kind.Aircraft:
                    var aircraft = GetComponent<AircraftBody>();
                    return aircraft != null && aircraft.Profile != null
                        ? aircraft.Profile.fuselageDiameterMetres
                        : LabelHeightMetres;

                case Kind.Crew:
                    var crew = GetComponent<Crew.CrewCharacter>();
                    return crew != null && crew.Profile != null
                        ? crew.Profile.heightMetres * 0.7f
                        : LabelHeightMetres;

                default:
                    var vehicle = GetComponent<VehicleController>();
                    return vehicle != null && vehicle.Profile != null
                        ? vehicle.Profile.bodySizeMetres.y * 0.7f
                        : LabelHeightMetres;
            }
        }

        void Dress()
        {
            var called = DisplayName;
            if (string.IsNullOrEmpty(called))
            {
                return;
            }

            name = called;

            var vehicle = GetComponent<VehicleController>();
            if (vehicle != null && vehicle.Profile == null)
            {
                var catalogue = FindAnyObjectByType<RampSpawner>();
                if (catalogue != null)
                {
                    vehicle.Configure(catalogue.ProfileFor(EquipmentKind), called);
                }
            }

            if (transform.Find("Greybox") == null)
            {
                DrawGreybox();
            }

            if (GetComponentInChildren<WorldLabel>() == null)
            {
                WorldLabel.Attach(transform, called, LabelAt());
            }
        }
    }
}
