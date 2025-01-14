using TcpServer.Common.Packet;
using TcpServer.Context;

namespace TcpServer.Procedures
{
    public partial class Procedure
    {
        [Procedure(PacketType.LoginClient)]
        public void Login(ClientContext clientContext, LoginClientPacket loginPacket)
        {
            clientContext.SendPacket(new LoginServerPacket());

            _logManager.Info($"login.UserId: {loginPacket.UserId}");
        }
    }
}
