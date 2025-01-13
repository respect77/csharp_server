using TcpServer.Common.Packet;
using TcpServer.Context;

namespace TcpServer.Procedures
{
    public partial class Procedure
    {
        [Procedure(PacketType.LoginClient)]
        public void Login(ClientContext clientContext, BasePacket base_packet)
        {
            if (base_packet is not LoginClientPacket loginPacket)
            {
                return;
            }

            clientContext.SendPacket(new LoginServerPacket());

            _logManager.Info($"login.UserId: {loginPacket.UserId}");
        }
    }
}
