using System.Collections;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;

namespace BelowTheWing.Tests.Multiplayer
{
    /// <summary>
    /// Three copies of the game in one process, talking to each other over a network worth testing on.
    ///
    /// Distributed authority, because that is the topology the game ships. Two clients and a session
    /// owner, because every problem worth catching here -- a train split across machines, a tractor
    /// taken from the person driving it, a cart configured as a tractor -- is invisible with one.
    ///
    /// And under <see cref="NetworkConditions.Typical"/> unless a test says otherwise. A test that
    /// wants a perfect connection has to ask for one, so that asking is a visible decision rather
    /// than the silent default it used to be.
    /// </summary>
    public abstract class RampMultiplayerTest : NetcodeIntegrationTest
    {
        GameObject m_SimulatorObject;
        NetworkSimulator m_Simulator;

        protected override int NumberOfClients => 2;

        protected override NetworkTopologyTypes OnGetNetworkTopologyType()
            => NetworkTopologyTypes.DistributedAuthority;

        /// <summary>The network this test runs on. Override to ask for something other than typical.</summary>
        protected virtual NetworkCondition Conditions => NetworkConditions.Typical;

        /// <summary>
        /// Puts the conditions on every instance in this process.
        ///
        /// The simulator reaches transports through adapters, and one adapter exists per driver --
        /// so one simulator covers the session owner and both clients rather than only whichever
        /// was created first. That is what makes the delay a property of the network here and not
        /// of one end of it.
        /// </summary>
        protected override IEnumerator OnStartedServerAndClients()
        {
            m_SimulatorObject = new GameObject("Network conditions");
            m_Simulator = m_SimulatorObject.AddComponent<NetworkSimulator>();
            ApplyConditions(Conditions);

            yield return base.OnStartedServerAndClients();
        }

        /// <summary>
        /// Puts a different network under a test that is already running.
        ///
        /// Worth having for one reason: a test that measures the same thing on two networks and
        /// compares them cannot be fooled by whatever floor the tick rate imposes, whereas a test
        /// comparing one measurement against a number somebody wrote down can be, and was.
        /// </summary>
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
