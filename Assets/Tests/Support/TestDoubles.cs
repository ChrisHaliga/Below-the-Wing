using System;
using System.Collections.Generic;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    /// <summary>
    /// An ownership broker that grants or refuses whatever it is asked, and remembers what that was.
    ///
    /// Real refusals happen when two players reach for the same tractor at the same moment, which
    /// is almost impossible to arrange deliberately. This makes the refusal an ordinary case to test.
    /// </summary>
    public sealed class RecordingBroker : IOwnershipBroker
    {
        readonly bool m_Grant;
        readonly Dictionary<VehicleController, ulong> m_Owners = new Dictionary<VehicleController, ulong>();

        /// <summary>Every batch of vehicles that has been asked for, in the order they were asked.</summary>
        public List<IReadOnlyList<VehicleController>> Requests { get; } = new List<IReadOnlyList<VehicleController>>();

        public ulong LocalClientId { get; }

        public RecordingBroker(bool grant, ulong localClientId = 1)
        {
            m_Grant = grant;
            LocalClientId = localClientId;
        }

        /// <summary>States who owns a vehicle before any request is made.</summary>
        public void SetOwner(VehicleController vehicle, ulong clientId) => m_Owners[vehicle] = clientId;

        public ulong OwnerOf(VehicleController vehicle)
            => m_Owners.TryGetValue(vehicle, out var id) ? id : 0;

        public bool OwnedByUs(VehicleController vehicle) => OwnerOf(vehicle) == LocalClientId;

        /// <summary>Everything handed back, in the order it was given up.</summary>
        public List<VehicleController> HandedBack { get; } = new List<VehicleController>();

        public void HandBack(IReadOnlyList<VehicleController> vehicles)
        {
            foreach (var vehicle in vehicles)
            {
                HandedBack.Add(vehicle);
                m_Owners[vehicle] = 0;
            }
        }

        public void RequestAll(IReadOnlyList<VehicleController> vehicles, Action<bool> onResult)
        {
            Requests.Add(new List<VehicleController>(vehicles));
            if (m_Grant)
            {
                foreach (var v in vehicles)
                {
                    m_Owners[v] = LocalClientId;
                }
            }

            onResult?.Invoke(m_Grant);
        }
    }

    /// <summary>A driver that always asks for the same thing, so a vehicle can be told to drive.</summary>
    public sealed class FixedIntent : IDriveIntentSource
    {
        public DriveIntent Current { get; set; }

        public FixedIntent(float steer = 0f, float throttle = 0f, float brake = 0f)
            => Current = new DriveIntent(steer, throttle, brake);
    }
}

namespace BelowTheWing.Tests.Support
{
    /// <summary>
    /// A broker that accepts a request and never answers it.
    ///
    /// This is what asking a machine that has left the session looks like. Netcode sends an
    /// ownership request to whichever client the object records as its owner, and if that client is
    /// gone the request simply goes nowhere -- no grant, no refusal, no error.
    /// </summary>
    public sealed class SilentBroker : BelowTheWing.Vehicles.IOwnershipBroker
    {
        public System.Collections.Generic.List<System.Collections.Generic.IReadOnlyList<BelowTheWing.Vehicles.VehicleController>> Requests { get; }
            = new System.Collections.Generic.List<System.Collections.Generic.IReadOnlyList<BelowTheWing.Vehicles.VehicleController>>();

        public ulong LocalClientId => 1;

        public ulong OwnerOf(BelowTheWing.Vehicles.VehicleController vehicle) => 9;

        public bool OwnedByUs(BelowTheWing.Vehicles.VehicleController vehicle) => false;

        public void RequestAll(
            System.Collections.Generic.IReadOnlyList<BelowTheWing.Vehicles.VehicleController> vehicles,
            System.Action<bool> onResult)
            => Requests.Add(new System.Collections.Generic.List<BelowTheWing.Vehicles.VehicleController>(vehicles));

        public void HandBack(System.Collections.Generic.IReadOnlyList<BelowTheWing.Vehicles.VehicleController> vehicles)
        {
        }
    }
}
