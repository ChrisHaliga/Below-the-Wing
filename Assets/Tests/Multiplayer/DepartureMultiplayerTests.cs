using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
    /// <summary>
    /// How a machine finds out that somebody else has left.
    ///
    /// This matters because nothing hands out a departed player's train automatically. Vehicles
    /// change hands only when somebody asks for them, deliberately -- handing them out one at a time
    /// is what split trains across machines. The cost of that is that a train belonging to somebody
    /// who quits is nobody's until the session owner reclaims it, and the session owner can only do
    /// that if it is told.
    ///
    /// The obvious callback is the wrong one. Netcode raises <c>OnClientDisconnectCallback</c> on a
    /// server and on the machine that itself disconnected. Under distributed authority nobody is a
    /// server, so somebody else leaving never reaches it, and a reclaim hung on it never runs: the
    /// train freezes mid-apron, simulated by no-one, for the rest of the session.
    /// </summary>
    public sealed class DepartureMultiplayerTests : RampMultiplayerTest
    {
        [UnityTest]
        public IEnumerator EverybodyStillHereHearsThatSomebodyLeft()
        {
            var leaving = m_ClientNetworkManagers[0];
            var staying = m_ClientNetworkManagers[1];
            var departedId = leaving.LocalClientId;

            var heardByStayingClient = new List<ulong>();
            var heardBySessionOwner = new List<ulong>();

            staying.OnConnectionEvent += (_, what) => Record(what, heardByStayingClient);
            m_ServerNetworkManager.OnConnectionEvent += (_, what) => Record(what, heardBySessionOwner);

            yield return StopOneClient(leaving);

            yield return WaitForConditionOrTimeOut(
                () => heardByStayingClient.Contains(departedId) && heardBySessionOwner.Contains(departedId));

            AssertOnTimeout(
                $"client {departedId} left and nobody else was told. Whatever that player was holding " +
                "is now owned by a machine that is gone: with nothing to reclaim it, their train sits " +
                "on the apron simulated by nobody for the rest of the session");
        }

        /// <summary>
        /// Records a departure the way the game listens for one: any event meaning "somebody who
        /// was here is not here now", not only the one that fires on a server.
        /// </summary>
        static void Record(ConnectionEventData what, ICollection<ulong> into)
        {
            if (what.EventType == ConnectionEvent.PeerDisconnected
                || what.EventType == ConnectionEvent.ClientDisconnected)
            {
                into.Add(what.ClientId);
            }
        }
    }
}
