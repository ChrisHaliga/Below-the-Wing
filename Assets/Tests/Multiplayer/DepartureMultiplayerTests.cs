using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
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
