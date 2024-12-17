using MemoryPack;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using TcpServer.Common;
using TcpServer.Common.Packet;

namespace TcpServer.Context
{
    public class ServerContext : IHostedService
    {
        private readonly TcpListener _listener;
        private bool _isRunning = false;

        // 연결된 클라이언트들을 저장할 ConcurrentDictionary
        private readonly ConcurrentDictionary<int, ClientContext> _clients = new();
        private readonly LogManager logManager = LogManager.Instance;
        public ServerContext(IOptions<ServerSettings> setting)
        {
            _listener = new TcpListener(IPAddress.Parse(setting.Value.IpAddress), setting.Value.Port);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _listener.Start();
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

        public void SendPacket<T>(ClientContext client_context, T packet) where T: BasePacket
        {
            client_context.SendPacket(MemoryPackSerializer.Serialize(packet));
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
        public void OnClientDisconnted(int clientId)
        {
            if (_clients.TryRemove(clientId, out _))
            {
                logManager.Info($"Client {clientId} Disconnected.");
            }
        }
    }
}
