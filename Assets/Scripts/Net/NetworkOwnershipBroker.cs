using System;
using System.Collections.Generic;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Net
{
    /// <summary>
    /// Ownership of vehicles, expressed in terms of the netcode layer.
    ///
    /// This is the only place that connects the driving code's idea of "who is simulating this" to
    /// the networking library's. Everything above it -- vehicles, trains, players getting in and
    /// out -- is written against <see cref="IOwnershipBroker"/> and would work just as well against
    /// a different networking library, or none.
    ///
    /// Requests for a whole train are made together and reported together. If any member is refused
    /// the whole request is refused and every member that was granted is handed straight back,
    /// because a train split between two machines has couplings whose two ends are being simulated
    /// by different physics engines, and neither engine can move the far end.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkOwnershipBroker : MonoBehaviour, IOwnershipBroker
    {
        NetworkManager Manager => NetworkManager.Singleton;

        public ulong LocalClientId => Manager != null ? Manager.LocalClientId : 0;

        public ulong OwnerOf(VehicleController vehicle)
        {
            var networked = vehicle != null ? vehicle.GetComponent<NetworkObject>() : null;
            return networked != null ? networked.OwnerClientId : 0;
        }

        public void RequestAll(IReadOnlyList<VehicleController> vehicles, Action<bool> onResult)
        {
            var members = new List<NetworkObject>(vehicles.Count);

            foreach (var vehicle in vehicles)
            {
                var networked = vehicle != null ? vehicle.GetComponent<NetworkObject>() : null;
                if (networked == null || !networked.IsSpawned)
                {
                    onResult?.Invoke(false);
                    return;
                }

                members.Add(networked);
            }

            new TrainHandover(members, LocalClientId, onResult).Begin();
        }

        /// <summary>
        /// One attempt to take a whole train, tracking each member's answer until they have all
        /// come back.
        ///
        /// Answers arrive one at a time and out of order, so the result cannot be known until the
        /// last one lands. Anything granted before a refusal arrives is given back rather than kept,
        /// which is what makes the operation all-or-nothing from the caller's point of view.
        /// </summary>
        sealed class TrainHandover
        {
            readonly List<NetworkObject> m_Members;
            readonly List<NetworkObject> m_Granted = new List<NetworkObject>();
            readonly Dictionary<NetworkObject, ulong> m_PreviousOwners = new Dictionary<NetworkObject, ulong>();
            readonly ulong m_Claimant;
            readonly Action<bool> m_OnResult;

            int m_Outstanding;
            bool m_AnyRefused;
            bool m_Reported;

            public TrainHandover(List<NetworkObject> members, ulong claimant, Action<bool> onResult)
            {
                m_Members = members;
                m_Claimant = claimant;
                m_OnResult = onResult;
            }

            public void Begin()
            {
                m_Outstanding = m_Members.Count;

                foreach (var member in m_Members)
                {
                    m_PreviousOwners[member] = member.OwnerClientId;

                    if (member.OwnerClientId == m_Claimant)
                    {
                        Answer(member, approved: true);
                        continue;
                    }

                    member.OnOwnershipRequestResponse += response => OnResponse(member, response);

                    var status = member.RequestOwnership();
                    if (status != NetworkObject.OwnershipRequestStatus.RequestSent)
                    {
                        // Turned down before it left this machine -- locked, or not a kind of
                        // object whose ownership moves at all.
                        Answer(member, approved: false);
                    }
                }
            }

            void OnResponse(NetworkObject member, NetworkObject.OwnershipRequestResponseStatus response)
                => Answer(member, response == NetworkObject.OwnershipRequestResponseStatus.Approved);

            void Answer(NetworkObject member, bool approved)
            {
                if (m_Reported)
                {
                    return;
                }

                if (approved)
                {
                    m_Granted.Add(member);
                }
                else
                {
                    m_AnyRefused = true;
                }

                m_Outstanding--;
                if (m_Outstanding > 0)
                {
                    return;
                }

                if (m_AnyRefused)
                {
                    GiveBackWhatWasGranted();
                }

                m_Reported = true;
                m_OnResult?.Invoke(!m_AnyRefused);
            }

            void GiveBackWhatWasGranted()
            {
                foreach (var member in m_Granted)
                {
                    if (m_PreviousOwners.TryGetValue(member, out var previous) && previous != m_Claimant)
                    {
                        member.ChangeOwnership(previous);
                    }
                }
            }
        }
    }
}
