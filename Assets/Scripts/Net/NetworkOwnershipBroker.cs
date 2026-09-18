using System.Collections.Generic;
using System.Collections;
using System;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Net
{
    [DisallowMultipleComponent]
    public sealed class NetworkOwnershipBroker : MonoBehaviour, IOwnershipBroker
    {
        const float AnswerDeadlineSeconds = 5f;

        NetworkManager Manager => NetworkManager.Singleton;

        public ulong LocalClientId => Manager != null && Manager.IsListening ? Manager.LocalClientId : Shift.Nobody;

        public ulong OwnerOf(VehicleController vehicle)
        {
            var networked = vehicle != null ? vehicle.GetComponent<NetworkObject>() : null;

            if (networked == null || !networked.IsSpawned)
            {
                return Shift.Nobody;
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
                    if (!previousOwners.TryGetValue(member, out var previous) || previous == claimant)
                    {
                        return;
                    }

                    member.ChangeOwnership(StillHere(previous) ? previous : SessionOwner);
                },
                onResult);

            foreach (var member in members)
            {
                Ask(member, claimant, request);
            }
        }

        ulong SessionOwner => Manager != null ? Manager.CurrentSessionOwner : Shift.Nobody;

        bool StillHere(ulong client)
        {
            if (Manager == null)
            {
                return false;
            }

            foreach (var id in Manager.ConnectedClientsIds)
            {
                if (id == client)
                {
                    return true;
                }
            }

            return false;
        }

        public bool OwnedByUs(VehicleController vehicle)
        {
            var us = LocalClientId;
            return us != Shift.Nobody && OwnerOf(vehicle) == us;
        }

        public void HandBack(IReadOnlyList<VehicleController> vehicles)
        {
            var sessionOwner = SessionOwner;

            foreach (var vehicle in vehicles)
            {
                var networked = vehicle != null ? vehicle.GetComponent<NetworkObject>() : null;
                if (networked != null && networked.IsSpawned && networked.OwnerClientId == LocalClientId)
                {
                    networked.ChangeOwnership(sessionOwner);
                }
            }
        }

        void Ask(NetworkObject member, ulong claimant, AllOrNothingRequest<NetworkObject> request)
        {
            if (member.OwnerClientId == claimant)
            {
                request.Answer(member, granted: true);
                return;
            }

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
                StartCoroutine(GiveUpIfNobodyAnswers(member, handler, request));
                return;
            }

            member.OnOwnershipRequestResponse -= handler;
            request.Answer(member, granted: false);
        }

        static IEnumerator GiveUpIfNobodyAnswers(
            NetworkObject member,
            NetworkObject.OnOwnershipRequestResponseDelegateHandler handler,
            AllOrNothingRequest<NetworkObject> request)
        {
            yield return new WaitForSeconds(AnswerDeadlineSeconds);

            if (request.Settled)
            {
                yield break;
            }

            if (member != null)
            {
                member.OnOwnershipRequestResponse -= handler;
            }

            request.Answer(member, granted: false);
        }
    }
}
