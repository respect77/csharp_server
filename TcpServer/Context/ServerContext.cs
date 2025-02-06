using Common;
using MemoryPack;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using TcpServer.Common.Packet;
using TcpServer.Procedures;

namespace TcpServer.Context
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ProcedureAttribute : Attribute
    {
        public readonly PacketType PacketType;

        public ProcedureAttribute(PacketType packetType)
        {
            PacketType = packetType;
        }
    }
    public class ServerContext : IHostedService
    {
        private readonly TcpListener _listener;
        private bool _isRunning = false;

        // 연결된 클라이언트들을 저장할 ConcurrentDictionary
        private readonly ConcurrentDictionary<int, ClientContext> _clients = new();
        private readonly Channel<(ClientContext, PacketType, BasePacket base_packet)> _requestChannel = Channel.CreateUnbounded<(ClientContext, PacketType, BasePacket)>();
        private readonly Procedure _procedure;
        private readonly LogManager _logManager = LogManager.Instance;
        public ServerContext(IOptions<ServerSettings> setting)
        {
            _listener = new TcpListener(IPAddress.Parse(setting.Value.IpAddress), setting.Value.Port);
            _procedure = new(this);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            /*
            var sendPacket = new LoginClientPacket();
            _procedure.Exec(new ClientContext(), sendPacket.Type, sendPacket);
            */

            _isRunning = true;
            _listener.Start();
            ScheduleExec(cancellationToken);
            _logManager.Info("Server started...");

            int clientId = 1;

            while (_isRunning)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                if (!_clients.TryAdd(clientId, new(client, clientId, this)))
                {
                    _logManager.Error($"Client {clientId} Exsited");
                }
                else
                {
                    _logManager.Info($"Client {clientId} connected.");
                }
                
                clientId++;
            }
        }

        public void RecvPacket(ClientContext client_context, PacketType type, BasePacket base_packet)
        {
            _requestChannel.Writer.TryWrite((client_context, type, base_packet));
        }
        
        public void SendPacket<T>(ClientContext client_context, T packet) where T: BasePacket
        {
            client_context.SendPacket(packet);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _isRunning = false;
            _listener.Stop();

            foreach (var client in _clients.Values)
            {
                client.Close();
            }
            _clients.Clear();
            _logManager.Info("Server Stopped.");
            return Task.CompletedTask;
        }
        private async void ScheduleExec(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var (client_context, type, base_packet) in _requestChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    _procedure.Exec(client_context, type, base_packet);
                }
            }
            catch (OperationCanceledException)
            {
                //종료
                return;
            }
            catch (Exception ex)
            {
                _logManager.Error($"ScheduleExec() Error {ex.Message}");
            }
            finally
            {
            }
        }
        public void OnClientDisconnted(int clientId)
        {
            if (_clients.TryRemove(clientId, out _))
            {
                _logManager.Info($"Client {clientId} Disconnected.");
            }
        }
    }
}
