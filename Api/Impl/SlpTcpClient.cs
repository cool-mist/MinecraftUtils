namespace MinecraftUtils.Api.Impl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    internal class SlpTcpClient : TcpClient
    {
        int tcpBufferOffset;
        NetworkStream tcpClientStream;
        List<byte> tcpBuffer;
        private readonly string serverHost;
        private readonly int serverPort;
        private readonly int protocol;
        private readonly AutoResetEvent connectionCompletedOrTimedOut;

        public SlpTcpClient(string serverHost, int serverPort, int protocol)
        {
            this.serverHost = serverHost;
            this.serverPort = serverPort;
            this.protocol = protocol;
            this.connectionCompletedOrTimedOut = new AutoResetEvent(false);
        }

        /*
        * Modified from https://gist.github.com/csh/2480d14fbbb33b4bbae3, for newer minecraft versions
        * Implements http://wiki.vg/Server_List_Ping#Ping_Process
        */
        public async Task<PingPayload> Ping(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await PingInternal(cancellationToken);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                tcpClientStream?.Close();
                connectionCompletedOrTimedOut.Dispose();
            }
        }

        private async Task<PingPayload> PingInternal(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await ConnectAsync(cancellationToken))
            {
                return null;
            }

            tcpBuffer = new List<byte>();
            tcpClientStream = GetStream();

            var buffer = new byte[short.MaxValue];

            WriteVarInt(protocol); // If client breaks, check this version first
            WriteString(serverHost);
            WriteShort(25565);
            WriteVarInt(1);

            cancellationToken.ThrowIfCancellationRequested();
            await Flush(0, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            await Flush(0, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            await Read(buffer, cancellationToken);

            ReadVarInt(buffer); //length
            ReadVarInt(buffer); //packet

            var jsonLength = ReadVarInt(buffer);
            var json = ReadString(buffer, jsonLength);
            return JsonSerializer.Deserialize<PingPayload>(json, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
        }

        private async Task Read(byte[] buffer, CancellationToken cancellationToken)
        {
            var readSize = -1;
            var bytesRead = 0;
            while (readSize != 0)
            {
                readSize = await tcpClientStream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead, cancellationToken);
                bytesRead += readSize;
            }
        }

        private async Task<bool> ConnectAsync(CancellationToken cancellationToken)
        {
            await ConnectAsyncInternal(cancellationToken);

            connectionCompletedOrTimedOut.WaitOne();

            if (!Connected)
            {
                return false;
            }

            return true;
        }

        private async Task ConnectAsyncInternal(CancellationToken cancellationToken)
        {
            await this.ConnectAsync(serverHost, serverPort, cancellationToken);
            this.connectionCompletedOrTimedOut.Set();
        }

        internal byte ReadByte(byte[] buffer)
        {
            var b = buffer[tcpBufferOffset];
            tcpBufferOffset += 1;
            return b;
        }

        internal byte[] Read(byte[] buffer, int length)
        {
            var data = new byte[length];
            Array.Copy(buffer, tcpBufferOffset, data, 0, length);
            tcpBufferOffset += length;
            return data;
        }

        internal int ReadVarInt(byte[] buffer)
        {
            var value = 0;
            var size = 0;
            int b;
            while (((b = ReadByte(buffer)) & 0x80) == 0x80)
            {
                value |= (b & 0x7F) << (size++ * 7);
                if (size > 5)
                {
                    throw new IOException("This VarInt is an imposter!");
                }
            }
            return value | ((b & 0x7F) << (size * 7));
        }

        internal string ReadString(byte[] buffer, int length)
        {
            var data = Read(buffer, length);
            return Encoding.UTF8.GetString(data);
        }

        internal void WriteVarInt(int value)
        {
            while ((value & 128) != 0)
            {
                tcpBuffer.Add((byte)(value & 127 | 128));
                value = (int)((uint)value) >> 7;
            }
            tcpBuffer.Add((byte)value);
        }

        internal void WriteShort(short value)
        {
            tcpBuffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void WriteString(string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            WriteVarInt(buffer.Length);
            tcpBuffer.AddRange(buffer);
        }

        internal async Task Flush(int id, CancellationToken cancellationToken)
        {
            var buffer = tcpBuffer.ToArray();
            tcpBuffer.Clear();

            var add = 0;
            var packetData = new[] { (byte)0x00 };
            if (id >= 0)
            {
                WriteVarInt(id);
                packetData = tcpBuffer.ToArray();
                add = packetData.Length;
                tcpBuffer.Clear();
            }

            WriteVarInt(buffer.Length + add);
            var bufferLength = tcpBuffer.ToArray();
            tcpBuffer.Clear();

            await tcpClientStream.WriteAsync(bufferLength, 0, bufferLength.Length, cancellationToken);
            await tcpClientStream.WriteAsync(packetData, 0, packetData.Length, cancellationToken);
            await tcpClientStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        internal class PingPayload
        {
            public VersionPayload Version { get; set; }

            public PlayersPayload Players { get; set; }

            public MotdPayload Description { get; set; }
        }

        internal class MotdPayload
        {
            public string Text { get; set; }
        }

        internal class VersionPayload
        {
            public int Protocol { get; set; }

            public string Name { get; set; }
        }

        internal class PlayersPayload
        {
            public int Max { get; set; }

            public int Online { get; set; }

            public List<Player> Sample { get; set; }
        }

        internal class Player
        {
            public string Name { get; set; }

            public string Id { get; set; }
        }
    }
}
