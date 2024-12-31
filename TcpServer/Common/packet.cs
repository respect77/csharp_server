using MemoryPack;

namespace TcpServer.Common.Packet
{
    public enum PacketType
    {
        LoginClient,
        LoginServer,
    }

    public enum ResultType
    {
        Success,
        Error1,
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
    public partial class ReaponseBasePacket: BasePacket
    {
        public ResultType Result { get; set; }
        public ReaponseBasePacket(PacketType type, ResultType result) : base(type)
        {
            Result = result;
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
    public partial class LoginServerPacket : ReaponseBasePacket
    {
        public LoginServerPacket(ResultType result = ResultType.Success) : base(PacketType.LoginServer, result)
        {
        }
    }
}
