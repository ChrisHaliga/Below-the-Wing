using System.Net;
using System.Net.Sockets;

namespace BelowTheWing.Tests.Support
{
    public static class Ports
    {
        public static ushort NobodyElseIsOn()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return (ushort)((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
    }
}
