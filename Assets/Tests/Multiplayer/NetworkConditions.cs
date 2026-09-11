using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;

namespace BelowTheWing.Tests.Multiplayer
{
    /// <summary>
    /// A network worth testing on: how long a message takes to cross, how much that varies, and
    /// how much of it never arrives.
    ///
    /// Stated as round trip because that is how a connection is described everywhere else -- "150
    /// millisecond ping" -- while the simulator underneath is set in one-way delay. Halving it in
    /// one place is what stops the two being confused everywhere else.
    /// </summary>
    public readonly struct NetworkCondition
    {
        public readonly string Name;

        /// <summary>How long a message takes to get there and back, in milliseconds.</summary>
        public readonly int RoundTripMilliseconds;

        /// <summary>How much that time varies from message to message, in milliseconds.</summary>
        public readonly int JitterMilliseconds;

        /// <summary>How many messages in a hundred never arrive at all.</summary>
        public readonly int LossPercent;

        public NetworkCondition(string name, int roundTripMilliseconds, int jitterMilliseconds, int lossPercent)
        {
            Name = name;
            RoundTripMilliseconds = roundTripMilliseconds;
            JitterMilliseconds = jitterMilliseconds;
            LossPercent = lossPercent;
        }

        /// <summary>How the simulator wants the same thing said.</summary>
        public INetworkSimulatorPreset AsPreset()
            => NetworkSimulatorPreset.Create(
                Name,
                description: $"{RoundTripMilliseconds} ms round trip, {LossPercent}% loss",
                packetDelayMs: RoundTripMilliseconds / 2,
                packetJitterMs: JitterMilliseconds,
                packetLossInterval: 0,
                packetLossPercent: LossPercent);

        public override string ToString()
            => $"{Name} ({RoundTripMilliseconds} ms round trip, {JitterMilliseconds} ms jitter, {LossPercent}% loss)";
    }

    /// <summary>
    /// The networks this game is tested on.
    ///
    /// Everything a multiplayer test proves is proved on one of these, and <see cref="Typical"/> is
    /// what a test gets without asking. That default is the point of the whole idea: a suite whose
    /// tests run on a perfect connection has only ever shown that the game works where no message
    /// can arrive late, out of order, or not at all -- which is not a network anybody plays on.
    /// </summary>
    public static class NetworkConditions
    {
        /// <summary>No delay, no loss. Only for a test that is about something else entirely.</summary>
        public static readonly NetworkCondition Clean =
            new NetworkCondition("Clean", roundTripMilliseconds: 0, jitterMilliseconds: 0, lossPercent: 0);

        /// <summary>An ordinary connection between two houses. What tests get unless they ask otherwise.</summary>
        public static readonly NetworkCondition Typical =
            new NetworkCondition("Typical", roundTripMilliseconds: 80, jitterMilliseconds: 10, lossPercent: 1);

        /// <summary>A bad connection, and the one the slice 2 gate is decided on.</summary>
        public static readonly NetworkCondition Bad =
            new NetworkCondition("Bad", roundTripMilliseconds: 150, jitterMilliseconds: 30, lossPercent: 5);
    }
}
