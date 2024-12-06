using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using TcpServer.Common;

namespace TcpServer.Context
{
    public class ServerContext : IHostedService
    {
        private TcpListener _listener;
        private bool _isRunning = false;

        // 연결된 클라이언트들을 저장할 ConcurrentDictionary
        private ConcurrentDictionary<int, ClientContext> _clients = new();

        public ServerContext(IOptions<ServerSettings> setting)
        {
            _listener = new TcpListener(IPAddress.Parse(setting.Value.IpAddress), setting.Value.Port);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _listener.Start();
            LogManager.Instance.Info("Server started...");

            int clientId = 1;

            while (_isRunning)
            {
                var client = await _listener.AcceptTcpClientAsync();
                if (!_clients.TryAdd(clientId, new(client, clientId, this)))
                {
                    LogManager.Instance.Error($"Client {clientId} Error");
                }
                else
                {
                    LogManager.Instance.Info($"Client {clientId} connected.");
                }
                
                clientId++;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _isRunning = false;
            _listener.Stop();

            // 모든 연결된 클라이언트 종료
            foreach (var client in _clients.Values)
            {
                client.Close();
            }
            _clients.Clear();
            LogManager.Instance.Info("Server stopped.");
            return Task.CompletedTask;
        }
        public void OnClientDisconnted(int clientId)
        {
            if (_clients.TryRemove(clientId, out _))
            {
                LogManager.Instance.Info($"Client {clientId} disconnected.");
            }
        }
    }
}
