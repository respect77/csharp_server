using Common;
using MemoryPack;
using System.Buffers;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Channels;
using TcpServer.Common.Packet;

namespace TcpServer.Context
{
    public class ClientContext
    {
        private readonly static int SendChannelCount = 1000;
        private readonly TcpClient _client;
        private readonly int _clientId;
        private readonly NetworkStream _stream;
        private readonly CancellationTokenSource _cts = new();
        private readonly ServerContext _serverContext;

        private readonly Channel<byte[]> _sendChannel = Channel.CreateBounded<byte[]>(SendChannelCount);

        //public ClientContext() { }
        public ClientContext(TcpClient client, int clientId, ServerContext serverContext)
        {
            _client = client;
            _client.NoDelay = true;
            _clientId = clientId;
            _stream = client.GetStream();
            _serverContext = serverContext;

            SendExecAsync();
            ReadExecAsync();
        }

        private async void ReadExecAsync()
        {
            byte[] header = new byte[4];
            int read_byte_count;
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    read_byte_count = 0;
                    while (read_byte_count < header.Length)
                    {
                        int byteCount = await _stream.ReadAsync(header.AsMemory(read_byte_count, header.Length - read_byte_count), _cts.Token).ConfigureAwait(false);
                        if (byteCount <= 0)
                        {
                            throw new IOException("end of stream");
                        }
                        read_byte_count += byteCount;
                    }
                    int packet_size = BitConverter.ToInt32(header, 0);

                    if (packet_size <= 0)
                    {
                        //Error
                        Close();
                        return;
                    }

                    read_byte_count = 0;
                    byte[] packet_buffer = ArrayPool<byte>.Shared.Rent(packet_size);

                    while (read_byte_count < packet_size)
                    {
                        int byteCount = await _stream.ReadAsync(packet_buffer.AsMemory(read_byte_count, packet_size - read_byte_count), _cts.Token).ConfigureAwait(false);
                        if (byteCount <= 0)
                        {
                            throw new IOException("end of stream");
                        }
                        read_byte_count += byteCount;
                    }

                    using var packet_buffer_stream = new MemoryStream(packet_buffer);
                    var basePacket = await MemoryPackSerializer.DeserializeAsync<BasePacket>(packet_buffer_stream, null, _cts.Token).ConfigureAwait(false);
                    if (basePacket == null)
                    {
                        ArrayPool<byte>.Shared.Return(packet_buffer);
                        continue;
                    }
                    _serverContext.RecvPacket(this, basePacket.Type, basePacket);
                    ArrayPool<byte>.Shared.Return(packet_buffer);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogManager.Instance.Error($"Read Error: {ex.Message}");
                Close();
            }
        }
        
        private async void SendExecAsync()
        {
            try
            {
                await foreach (var packet in _sendChannel.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
                {
                    await _stream.WriteAsync(packet, _cts.Token).ConfigureAwait(false);
                    //await _stream.FlushAsync(_cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                LogManager.Instance.Error($"Write Error: {ex.Message}");
                Close();
            }
        }
        public bool SendPacket<T>(T packet) where T: BasePacket
        {
            if (!_sendChannel.Writer.TryWrite(MemoryPackSerializer.Serialize(packet)))
            {
                LogManager.Instance.Error("!_sendChannel.Writer.TryWrite(packet)");
                Close();
                return false;
            }
            return true;
        }

        public void Close()
        {
            _cts.Cancel();
            //_writeChannel.Writer.Complete(); // 채널을 완료하여 작업 종료
            _client.Close();
            _serverContext.OnClientDisconnted(_clientId);
        }
    }
}
