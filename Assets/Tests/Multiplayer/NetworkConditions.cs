using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;

namespace BelowTheWing.Tests.Multiplayer
{
    public readonly struct NetworkCondition
    {
        public readonly string Name;

        public readonly int RoundTripMilliseconds;

        public readonly int JitterMilliseconds;

        public readonly int LossPercent;

        public NetworkCondition(string name, int roundTripMilliseconds, int jitterMilliseconds, int lossPercent)
        {
            Name = name;
            RoundTripMilliseconds = roundTripMilliseconds;
            JitterMilliseconds = jitterMilliseconds;
            LossPercent = lossPercent;
        }

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

    public static class NetworkConditions
    {
        public static readonly NetworkCondition Clean =
            new NetworkCondition("Clean", roundTripMilliseconds: 0, jitterMilliseconds: 0, lossPercent: 0);

        public static readonly NetworkCondition Typical =
            new NetworkCondition("Typical", roundTripMilliseconds: 80, jitterMilliseconds: 10, lossPercent: 1);

        public static readonly NetworkCondition Bad =
            new NetworkCondition("Bad", roundTripMilliseconds: 150, jitterMilliseconds: 30, lossPercent: 5);
    }
}
