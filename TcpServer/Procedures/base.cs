using System.Reflection;
using TcpServer.Common;
using TcpServer.Common.Packet;
using TcpServer.Context;

namespace TcpServer.Procedures
{
    public partial class Procedure
    {
        private readonly ServerContext ServerContext;
        public delegate void ProcedureExecDelegate(ClientContext clientContext, byte[] packet_buffer);

        private readonly Dictionary<PacketType, ProcedureExecDelegate> _prodcedure = new();
        private readonly LogManager _logManager = LogManager.Instance;
        public Procedure(ServerContext serverContext)
        {
            ServerContext = serverContext;

            foreach (var method in GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = method.GetCustomAttribute<PacketExecAttribute>();
                if (attr == null)
                {
                    continue;
                }
                var del = (ProcedureExecDelegate)Delegate.CreateDelegate(typeof(ProcedureExecDelegate), this, method);
                _prodcedure[attr.PacketType] = del;
            }
        }

        public void Exec(ClientContext client_context, PacketType type, byte[] packet_buffer)
        {
            if (_prodcedure.TryGetValue(type, out var func))
            {
                func(client_context, packet_buffer);
            }
        }
    }
}
