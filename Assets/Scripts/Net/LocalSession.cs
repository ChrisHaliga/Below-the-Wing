using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Net
{
    /// <summary>
    /// Starting a session on this machine and nobody else's.
    ///
    /// Playing with other people goes through Unity's multiplayer service: an anonymous sign-in, a
    /// relay allocation and a lobby, which is several round trips to the internet before there is
    /// an apron to stand on. None of that is needed to play alone, and paying for it anyway is the
    /// better part of a minute every time somebody wants to try something out.
    ///
    /// What this deliberately does not change is what kind of session it is. Netcode's topology and
    /// its service connection are separate things: a distributed authority session can run with no
    /// service behind it at all, with this machine as the session owner. Every ownership rule in
    /// this game assumes distributed authority -- that no machine is a server, that objects change
    /// hands, that the session owner is one of the clients -- so a solo session that quietly ran as
    /// client-server would be a different game wearing the same scene.
    /// </summary>
    public static class LocalSession
    {
        /// <summary>
        /// Starts one, and says whether it started. Nothing here waits on anything off this machine,
        /// so by the time this returns the session is running or it is not.
        /// </summary>
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
