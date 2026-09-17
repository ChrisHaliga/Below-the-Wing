using System;
using System.Collections.Generic;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    public sealed class RecordingBroker : IOwnershipBroker
    {
        readonly bool m_Grant;
        readonly Dictionary<VehicleController, ulong> m_Owners = new Dictionary<VehicleController, ulong>();

        public List<IReadOnlyList<VehicleController>> Requests { get; } = new List<IReadOnlyList<VehicleController>>();

        public ulong LocalClientId { get; }

        public RecordingBroker(bool grant, ulong localClientId = 1)
        {
            m_Grant = grant;
            LocalClientId = localClientId;
        }

        public void SetOwner(VehicleController vehicle, ulong clientId) => m_Owners[vehicle] = clientId;

        public ulong OwnerOf(VehicleController vehicle)
            => m_Owners.TryGetValue(vehicle, out var id) ? id : 0;

        public bool OwnedByUs(VehicleController vehicle) => OwnerOf(vehicle) == LocalClientId;

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

    public sealed class FixedIntent : IDriveIntentSource
    {
        public DriveIntent Current { get; set; }

        public FixedIntent(float steer = 0f, float throttle = 0f, float brake = 0f)
            => Current = new DriveIntent(steer, throttle, brake);
    }
}

namespace BelowTheWing.Tests.Support
{
    public sealed class DeferredBroker : IOwnershipBroker
    {
        readonly List<(IReadOnlyList<VehicleController> Vehicles, Action<bool> Answer)> m_Pending
            = new List<(IReadOnlyList<VehicleController>, Action<bool>)>();

        readonly Dictionary<VehicleController, ulong> m_Owners = new Dictionary<VehicleController, ulong>();

        public ulong LocalClientId => 1;

        public int Waiting => m_Pending.Count;

        public List<VehicleController> HandedBack { get; } = new List<VehicleController>();

        public ulong OwnerOf(VehicleController vehicle)
            => m_Owners.TryGetValue(vehicle, out var id) ? id : 0;

        public bool OwnedByUs(VehicleController vehicle) => OwnerOf(vehicle) == LocalClientId;

        public void RequestAll(IReadOnlyList<VehicleController> vehicles, Action<bool> onResult)
            => m_Pending.Add((new List<VehicleController>(vehicles), onResult));

        public void HandBack(IReadOnlyList<VehicleController> vehicles)
        {
            foreach (var vehicle in vehicles)
            {
                HandedBack.Add(vehicle);
                m_Owners[vehicle] = 0;
            }
        }

        public void Answer(int request, bool granted)
        {
            var (vehicles, answer) = m_Pending[request];

            if (granted)
            {
                foreach (var vehicle in vehicles)
                {
                    m_Owners[vehicle] = LocalClientId;
                }
            }

            answer?.Invoke(granted);
        }
    }

    public sealed class SilentBroker : IOwnershipBroker
    {
        public List<IReadOnlyList<VehicleController>> Requests { get; } = new List<IReadOnlyList<VehicleController>>();

        public ulong LocalClientId => 1;

        public ulong OwnerOf(VehicleController vehicle) => 9;

        public bool OwnedByUs(VehicleController vehicle) => false;

        public void RequestAll(IReadOnlyList<VehicleController> vehicles, Action<bool> onResult)
            => Requests.Add(new List<VehicleController>(vehicles));

        public void HandBack(IReadOnlyList<VehicleController> vehicles)
        {
        }
    }
}
