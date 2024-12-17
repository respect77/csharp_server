using MemoryPack;

namespace TcpServer.Common.Packet
{
    public enum PacketType
    {
        LoginClient,
        LoginServer,
    }

    [MemoryPackable]
    public partial class BasePacket
    {
        public PacketType Type { get; set; }
        public BasePacket(PacketType type)
        {
            Type = type;
        }
    }
    [MemoryPackable]
    public partial class LoginClientPacket: BasePacket
    {
        public int UserId { get; set; }
        //
        public LoginClientPacket(): base(PacketType.LoginClient)
        {
        }
    }

    [MemoryPackable]
    public partial class LoginServerPacket : BasePacket
    {
        public int Result { get; set; }
        public LoginServerPacket() : base(PacketType.LoginServer)
        {
        }
    }
}
