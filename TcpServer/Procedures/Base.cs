using System.Reflection;
using TcpServer.Common;
using TcpServer.Common.Packet;
using TcpServer.Context;

namespace TcpServer.Procedures
{
    public partial class Procedure
    {
        private readonly ServerContext ServerContext;
        public delegate void ProcedureExecDelegate<T>(ClientContext clientContext, T packet) where T: BasePacket;
        private readonly Dictionary<PacketType, Action<ClientContext, BasePacket>> _prodcedure = new();
        private readonly LogManager _logManager = LogManager.Instance;
        public Procedure(ServerContext serverContext)
        {
            ServerContext = serverContext;

            MethodInfo RegistMethod = typeof(Procedure).GetMethod("RegistProcedure") ?? throw new Exception("RegistProcedure Not Exists");

            foreach (var method in GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attribute = method.GetCustomAttribute<ProcedureAttribute>();
                if (attribute == null)
                {
                    continue;
                }
                var ParameterT = method.GetParameters()[1].ParameterType;

                MethodInfo RegistGenericMethod = RegistMethod.MakeGenericMethod(ParameterT) ?? throw new Exception("MakeGenericMethod Error");

                RegistGenericMethod.Invoke(this, [attribute.PacketType, method]);
            }
        }
        
        public void RegistProcedure<T>(PacketType PacketType, MethodInfo method) where T:BasePacket
        {
            var del = (ProcedureExecDelegate<T>)Delegate.CreateDelegate(typeof(ProcedureExecDelegate<T>), this, method);
            _prodcedure[PacketType] = (ClientContext client_context, BasePacket packet) => del(client_context, (T)packet);
            //_prodcedure[PacketType] = (ClientContext client_context, BasePacket packet) => method.Invoke(this, [client_context, (T)packet]);
        }
        
        public void Exec(ClientContext client_context, PacketType type, BasePacket base_packet)
        {
            if (_prodcedure.TryGetValue(type, out var func))
            {
                func(client_context, base_packet);
            }
            else
            {
                _logManager.Error($"_prodcedure.TryGetValue(type, out var func): {type}");
            }
        }
    }
}
