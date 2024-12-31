using MemoryPack;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using TcpServer.Common;
using TcpServer.Common.Packet;
using TcpServer.Procedures;

namespace TcpServer.Context
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PacketExecAttribute : Attribute
    {
        public PacketType PacketType { get; }

        public PacketExecAttribute(PacketType packetType)
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
        private readonly Channel<(ClientContext, PacketType, byte[] packet_buffer)> _requestChannel = Channel.CreateUnbounded<(ClientContext, PacketType, byte[])>();
        private readonly Procedure _procedure;
        private readonly LogManager logManager = LogManager.Instance;
        public ServerContext(IOptions<ServerSettings> setting)
        {
            _listener = new TcpListener(IPAddress.Parse(setting.Value.IpAddress), setting.Value.Port);
            _procedure = new(this);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _listener.Start();
            ScheduleExec(cancellationToken);
            logManager.Info("Server started...");

            int clientId = 1;

            while (_isRunning)
            {
                var client = await _listener.AcceptTcpClientAsync();
                if (!_clients.TryAdd(clientId, new(client, clientId, this)))
                {
                    logManager.Error($"Client {clientId} Exsited");
                }
                else
                {
                    logManager.Info($"Client {clientId} connected.");
                }
                
                clientId++;
            }
        }

        public void RecvPacket(ClientContext client_context, PacketType type, byte[] packet_buffer)
        {
            _requestChannel.Writer.TryWrite((client_context, type, packet_buffer));
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
            logManager.Info("Server Stopped.");
            return Task.CompletedTask;
        }
        private async void ScheduleExec(CancellationToken cancellationToken)
        {
            /*
            var p = new LoginClientPacket();
            p.UserId = 1234;
            _procedure.Exec(new ClientContext(), p.Type, MemoryPackSerializer.Serialize(p));
            */
            try
            {
                await foreach (var request in _requestChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    var (client_context, type, packet_buffer) = request;
                    _procedure.Exec(client_context, type, packet_buffer);
                    ArrayPool<byte>.Shared.Return(packet_buffer);
                }
            }
            catch (OperationCanceledException)
            {
                //종료
                return;
            }
            catch (Exception ex)
            {
                logManager.Error($"ScheduleExec() Error {ex.Message}");
            }
            finally
            {
            }
        }
        public void OnClientDisconnted(int clientId)
        {
            if (_clients.TryRemove(clientId, out _))
            {
                logManager.Info($"Client {clientId} Disconnected.");
            }
        }
    }
}
