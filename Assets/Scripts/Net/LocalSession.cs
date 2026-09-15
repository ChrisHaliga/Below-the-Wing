using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace BelowTheWing.Net
{
    public static class LocalSession
    {
        public static bool Start(NetworkManager netcode)
        {
            if (netcode == null)
            {
                Debug.LogError("There is no NetworkManager in the scene, so there is no session to start.");
                return false;
            }

            if (netcode.IsListening)
            {
                return true;
            }

            OnAPortNobodyIsUsing(netcode);

            if (!netcode.StartHost())
            {
                Debug.LogError(
                    "Netcode refused to start a session on this machine. Playing alone asks for the " +
                    "same distributed authority session as playing with others, without the service " +
                    "behind it.");
                return false;
            }

            return true;
        }

        static void OnAPortNobodyIsUsing(NetworkManager netcode)
        {
            if (netcode.NetworkConfig?.NetworkTransport is UnityTransport transport)
            {
                transport.ConnectionData.Port = AskTheMachineForOne;
            }
        }

        const ushort AskTheMachineForOne = 0;
    }
}
