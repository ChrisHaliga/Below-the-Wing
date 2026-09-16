using System;
using System.Net;
using System.Net.Sockets;

namespace BelowTheWing.Tests.Support
{
    public sealed class Squatting : IDisposable
    {
        readonly Socket m_Held;

        Squatting(Socket held) => m_Held = held;

        public static Squatting On(out ushort port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));

            port = (ushort)((IPEndPoint)socket.LocalEndPoint).Port;

            return new Squatting(socket);
        }

        public void Dispose() => m_Held.Dispose();
    }
}
