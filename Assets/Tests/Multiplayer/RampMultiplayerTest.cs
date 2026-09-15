using System.Collections;
using BelowTheWing.Tests.Support;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace BelowTheWing.Tests.Multiplayer
{
    public abstract class RampMultiplayerTest : NetcodeIntegrationTest
    {
        GameObject m_SimulatorObject;
        NetworkSimulator m_Simulator;
        ushort m_Port;

        protected override int NumberOfClients => 2;

        protected override NetworkTopologyTypes OnGetNetworkTopologyType()
            => NetworkTopologyTypes.DistributedAuthority;

        protected virtual NetworkCondition Conditions => NetworkConditions.Typical;

        protected override void OnServerAndClientsCreated()
        {
            m_Port = Ports.NobodyElseIsOn();

            TalkOn(m_ServerNetworkManager);

            foreach (var client in m_ClientNetworkManagers)
            {
                TalkOn(client);
            }

            base.OnServerAndClientsCreated();
        }

        protected override void OnNewClientCreated(NetworkManager client)
        {
            TalkOn(client);
            base.OnNewClientCreated(client);
        }

        void TalkOn(NetworkManager machine)
            => machine.GetComponent<UnityTransport>().ConnectionData.Port = m_Port;

        protected override IEnumerator OnStartedServerAndClients()
        {
            m_SimulatorObject = new GameObject("Network conditions");
            m_Simulator = m_SimulatorObject.AddComponent<NetworkSimulator>();
            ApplyConditions(Conditions);

            yield return base.OnStartedServerAndClients();
        }

        protected void ApplyConditions(NetworkCondition conditions)
            => m_Simulator.ConnectionPreset = conditions.AsPreset();

        protected override IEnumerator OnTearDown()
        {
            if (m_SimulatorObject != null)
            {
                Object.DestroyImmediate(m_SimulatorObject);
                m_SimulatorObject = null;
                m_Simulator = null;
            }

            yield return base.OnTearDown();
        }
    }
}
