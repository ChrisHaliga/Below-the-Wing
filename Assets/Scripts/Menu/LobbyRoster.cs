using System;
using System.Collections.Generic;
using BelowTheWing.Wiring;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Menu
{
    public struct SeatedCrew : INetworkSerializable, IEquatable<SeatedCrew>
    {
        public ulong Player;
        public bool Ready;
        public FixedString32Bytes Called;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Player);
            serializer.SerializeValue(ref Ready);
            serializer.SerializeValue(ref Called);
        }

        public bool Equals(SeatedCrew other)
            => Player == other.Player && Ready == other.Ready && Called.Equals(other.Called);
    }

    [DisallowMultipleComponent]
    public sealed class LobbyRoster : NetworkBehaviour
    {
        readonly NetworkList<SeatedCrew> m_Seated = new NetworkList<SeatedCrew>();

        public event Action Changed;

        public ulong Host { get; private set; }

        public int Filled => m_Seated.Count;

        public IReadOnlyList<SeatedCrew> Seats
        {
            get
            {
                m_Reading.Clear();

                foreach (var seat in m_Seated)
                {
                    m_Reading.Add(seat);
                }

                return m_Reading;
            }
        }

        readonly List<SeatedCrew> m_Reading = new List<SeatedCrew>(Shift.MostCrew);

        public bool AmIReady => Where(NetworkManager.LocalClientId) >= 0
                                && m_Seated[Where(NetworkManager.LocalClientId)].Ready;

        public bool CanStart => Slots().CanStart(NetworkManager.LocalClientId);

        public bool EveryoneReady => Slots().EveryoneReady;

        public override void OnNetworkSpawn()
        {
            m_Seated.OnListChanged += _ => Changed?.Invoke();

            Host = NetworkManager.LocalClient.IsSessionOwner
                ? NetworkManager.LocalClientId
                : FirstSeatedOr(NetworkManager.LocalClientId);

            if (NetworkManager.LocalClient.IsSessionOwner)
            {
                NetworkManager.OnConnectionEvent += OnSomebodyCameOrWent;
            }

            TakeASeatRpc(NetworkManager.LocalClientId, Shift.NameFor(NetworkManager.LocalClientId));
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager != null)
            {
                NetworkManager.OnConnectionEvent -= OnSomebodyCameOrWent;
            }
        }

        public void ReadyUp(bool ready) => SetReadyRpc(NetworkManager.LocalClientId, ready);

        [Rpc(SendTo.Authority)]
        void TakeASeatRpc(ulong player, FixedString32Bytes called)
        {
            if (Where(player) >= 0 || m_Seated.Count >= Shift.MostCrew)
            {
                return;
            }

            m_Seated.Add(new SeatedCrew { Player = player, Ready = false, Called = called });
        }

        [Rpc(SendTo.Authority)]
        void SetReadyRpc(ulong player, bool ready)
        {
            var seat = Where(player);

            if (seat < 0)
            {
                return;
            }

            var sitting = m_Seated[seat];
            sitting.Ready = ready;
            m_Seated[seat] = sitting;
        }

        void OnSomebodyCameOrWent(NetworkManager manager, ConnectionEventData what)
        {
            if (what.EventType != ConnectionEvent.PeerDisconnected
                && what.EventType != ConnectionEvent.ClientDisconnected)
            {
                return;
            }

            var seat = Where(what.ClientId);

            if (seat >= 0)
            {
                m_Seated.RemoveAt(seat);
            }
        }

        int Where(ulong player)
        {
            for (var seat = 0; seat < m_Seated.Count; seat++)
            {
                if (m_Seated[seat].Player == player)
                {
                    return seat;
                }
            }

            return -1;
        }

        ulong FirstSeatedOr(ulong fallback) => m_Seated.Count > 0 ? m_Seated[0].Player : fallback;

        LobbySlots Slots()
        {
            var slots = new LobbySlots(Host);

            foreach (var seat in m_Seated)
            {
                slots.Arrived(seat.Player);
                slots.Ready(seat.Player, seat.Ready);
            }

            return slots;
        }
    }
}
