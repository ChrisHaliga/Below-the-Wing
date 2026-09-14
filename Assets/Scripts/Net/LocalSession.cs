using Unity.Netcode;
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
    }
}
