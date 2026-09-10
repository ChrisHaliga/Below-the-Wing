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
    /// Asking for a train is asking for all of it. Whether that succeeded is decided by
    /// <see cref="AllOrNothingRequest{T}"/>; this class only sends the requests, reports the
    /// answers, and hands things back when told to.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkOwnershipBroker : MonoBehaviour, IOwnershipBroker
    {
        /// <summary>
        /// Stands for "no machine at all". Client ids start at zero and zero is a real client, so
        /// answering an unanswerable question with zero would have a machine conclude it owned
        /// everything on the apron.
        /// </summary>
        public const ulong Nobody = ulong.MaxValue;

        NetworkManager Manager => NetworkManager.Singleton;

        public ulong LocalClientId => Manager != null && Manager.IsListening ? Manager.LocalClientId : Nobody;

        public ulong OwnerOf(VehicleController vehicle)
        {
            var networked = vehicle != null ? vehicle.GetComponent<NetworkObject>() : null;

            if (networked == null || !networked.IsSpawned)
            {
                return Nobody;
            }

            return networked.OwnerClientId;
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

            var claimant = LocalClientId;
            var previousOwners = new Dictionary<NetworkObject, ulong>(members.Count);
            foreach (var member in members)
            {
                previousOwners[member] = member.OwnerClientId;
            }

            var request = new AllOrNothingRequest<NetworkObject>(
                members,
                giveBack: member =>
                {
                    if (previousOwners.TryGetValue(member, out var previous) && previous != claimant)
                    {
                        member.ChangeOwnership(previous);
                    }
                },
                onResult);

            foreach (var member in members)
            {
                Ask(member, claimant, request);
            }
        }

        public void HandBack(IReadOnlyList<VehicleController> vehicles)
        {
            var sessionOwner = Manager != null ? Manager.CurrentSessionOwner : 0;

            foreach (var vehicle in vehicles)
            {
                var networked = vehicle != null ? vehicle.GetComponent<NetworkObject>() : null;
                if (networked != null && networked.IsSpawned && networked.OwnerClientId == LocalClientId)
                {
                    networked.ChangeOwnership(sessionOwner);
                }
            }
        }

        static void Ask(NetworkObject member, ulong claimant, AllOrNothingRequest<NetworkObject> request)
        {
            if (member.OwnerClientId == claimant)
            {
                request.Answer(member, granted: true);
                return;
            }

            // The handler takes itself off the moment it fires. Left subscribed, every attempt to
            // get into a vehicle would leave another closure behind on it, each one holding the
            // whole request and the seat that made it.
            NetworkObject.OnOwnershipRequestResponseDelegateHandler handler = null;
            handler = response =>
            {
                member.OnOwnershipRequestResponse -= handler;
                request.Answer(member, response == NetworkObject.OwnershipRequestResponseStatus.Approved);
            };

            member.OnOwnershipRequestResponse += handler;

            var status = member.RequestOwnership();
            if (status == NetworkObject.OwnershipRequestStatus.RequestSent)
            {
                return;
            }

            // Turned down before it left this machine -- locked, or not a kind of object whose
            // ownership moves at all. No response is coming, so the handler has to go now.
            member.OnOwnershipRequestResponse -= handler;
            request.Answer(member, granted: false);
        }
    }
}
