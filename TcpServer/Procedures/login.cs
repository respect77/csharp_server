using MemoryPack;
using TcpServer.Common.Packet;
using TcpServer.Context;

namespace TcpServer.Procedures
{
    public partial class Procedure
    {
        [PacketExec(PacketType.LoginClient)]
        public void Login(ClientContext clientContext, byte[] packet_buffer)
        {
            var loginPacket = MemoryPackSerializer.Deserialize<LoginClientPacket>(packet_buffer);
            if (loginPacket == null)
            {
                return;
            }

            clientContext.SendPacket(new LoginServerPacket());

            _logManager.Info($"login.UserId: {loginPacket.UserId}");
        }
    }
}
