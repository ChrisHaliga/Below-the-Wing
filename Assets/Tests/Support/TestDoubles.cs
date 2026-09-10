using System;
using System.Collections.Generic;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    /// <summary>
    /// A vehicle that exists only to be near or far, and to say whether it would take a driver.
    /// Used where the question is which vehicle gets offered, not what happens when one is taken.
    /// </summary>
    public sealed class StubDriveable : IDriveable
    {
        public string DisplayName { get; }
        public Vector3 Position { get; }
        public bool AcceptsDriver { get; }

        public StubDriveable(string displayName, Vector3 position, bool acceptsDriver = true)
        {
            DisplayName = displayName;
            Position = position;
            AcceptsDriver = acceptsDriver;
        }
    }

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
