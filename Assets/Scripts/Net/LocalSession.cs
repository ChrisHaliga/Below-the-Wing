using BelowTheWing.Wiring;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace BelowTheWing.Net
{
    public static class LocalSession
    {
        public static bool Start(NetworkManager netcode) => Start(netcode, out _);

        public static bool Start(NetworkManager netcode, out string refusal)
        {
            if (netcode == null)
            {
                throw new MisbuiltException(
                    "There is no NetworkManager in the scene, so there is no session to start.");
            }

            refusal = "";

            if (netcode.IsListening)
            {
                return true;
            }

            OnAPortNobodyIsUsing(netcode);

            if (netcode.StartHost())
            {
                return true;
            }

            refusal = "Netcode refused to start a session on this machine. Playing alone asks for " +
                      "the same distributed authority session as playing with others, without the " +
                      "service behind it.";

            return false;
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
