using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftUtils.Api.Impl
{
    internal class SlpTcpClient : TcpClient
    {
        int tcpBufferOffset;
        NetworkStream tcpClientStream;
        List<byte> tcpBuffer;
        private readonly string serverHost;
        private readonly int serverPort;

        public SlpTcpClient(string serverHost, int serverPort)
        {
            this.serverHost = serverHost;
            this.serverPort = serverPort;
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
                tcpClientStream.Close();
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

            // 1. Handshake
            WriteVarInt(755); // If client breaks, check this version first
            WriteString(serverHost);
            WriteShort(25565);
            WriteVarInt(1);
            await Flush(0, cancellationToken);

            // 2. Empty message
            await Flush(0, cancellationToken);

            var buffer = new byte[Int16.MaxValue];

            // 3. Server response
            await Read(buffer, cancellationToken);    // Servers seem to send an empty packet and read an empty packet 
            await Flush(0, cancellationToken);        // before actually sending the server status. This is the main
            await Read(buffer, cancellationToken);    // modification from the initial code

            ReadVarInt(buffer); //length
            ReadVarInt(buffer); //packet

            var jsonLength = ReadVarInt(buffer);
            var json = ReadString(buffer, jsonLength);
            return JsonConvert.DeserializeObject<PingPayload>(json);
        }

        private Task Read(byte[] buffer, CancellationToken cancellationToken)
        {
            return tcpClientStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        private async Task<bool> ConnectAsync(CancellationToken cancellationToken)
        {
            ValueTask task = this.ConnectAsync(serverHost, serverPort, cancellationToken);

            while (!task.IsCompleted)
            {
                await Task.Delay(250);
            }

            if (!Connected)
            {
                return false;
            }

            return true;
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
            [JsonProperty(PropertyName = "version")]
            public VersionPayload Version { get; set; }

            [JsonProperty(PropertyName = "players")]
            public PlayersPayload Players { get; set; }

            [JsonProperty(PropertyName = "description")]
            public MotdPayload Motd { get; set; }

            /// <summary>
            /// Server icon, important to note that it's encoded in base 64
            /// </summary>
            [JsonProperty(PropertyName = "favicon")]
            public string Icon { get; set; }
        }

        internal class MotdPayload
        {
            [JsonProperty(PropertyName = "text")]
            public string Text { get; set; }
        }

        internal class VersionPayload
        {
            [JsonProperty(PropertyName = "protocol")]
            public int Protocol { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
        }

        internal class PlayersPayload
        {
            [JsonProperty(PropertyName = "max")]
            public int Max { get; set; }

            [JsonProperty(PropertyName = "online")]
            public int Online { get; set; }

            [JsonProperty(PropertyName = "sample")]
            public List<Player> Sample { get; set; }
        }

        internal class Player
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
        }
    }
}
