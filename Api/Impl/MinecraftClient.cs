namespace MinecraftUtils.Api.Impl
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using static MinecraftUtils.Api.Impl.SlpTcpClient;

    public class MinecraftClient : IMinecraftClient
    {
        private readonly int DEFAULT_SERVER_PORT = 25565;
        private readonly int DEFAULT_PROTOCOL = 760;
        private readonly ITaskExecutor executor = new TaskExecutor();

        public Task<ITaskResponse<IMinecraftState>> GetStateAsync(string serverHost, int serverPort, int protocol, CancellationToken cancellationToken)
        {
            return executor.ExecuteAsync("GetServerState", GetServerStateAsyncTask(serverHost, serverPort, protocol, cancellationToken), cancellationToken);
        }

        public Task<ITaskResponse<IMinecraftState>> GetStateAsync(string serverHost, CancellationToken cancellationToken)
        {
            return GetStateAsync(serverHost, DEFAULT_SERVER_PORT, DEFAULT_PROTOCOL, cancellationToken);
        }

        private Func<Task<IMinecraftState>> GetServerStateAsyncTask(string serverHost, int serverPort, int protocol, CancellationToken cancellationToken)
        {
            return async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var slpTcpClient = new SlpTcpClient(serverHost, serverPort, protocol))
                {
                    var ping = await slpTcpClient.Ping(cancellationToken);
                    return CreateMinecraftServerResponse(ping, serverHost, serverPort);
                }
            };
        }

        private IMinecraftState CreateMinecraftServerResponse(PingPayload ping, string serverHost, int serverPort)
        {
            var portSuffix = serverPort == DEFAULT_SERVER_PORT ? "" : $":{serverPort}";
            if (ping == null || ping.Players == null || ping.Version == null || ping.Version.Protocol == 1)
            {
                return new MinecraftState()
                {
                    State = "Offline",
                    MaxPlayers = 0,
                    OnlinePlayers = 0,
                    Hostname = $"{serverHost}{portSuffix}"
                };
            }

            return new MinecraftState()
            {
                State = "Online",
                MaxPlayers = ping?.Players?.Max ?? 0,
                OnlinePlayers = ping?.Players?.Online ?? 0,
                Hostname = serverHost,
                Motd = ping?.Description?.Text,
                Ping = ping
            };
        }

        public void Dispose()
        { }
    }

    internal class MinecraftState : IMinecraftState
    {
        public string Hostname { get; set; }

        public string Version { get; set; }

        public string Motd { get; set; }

        public int MaxPlayers { get; set; }

        public int OnlinePlayers { get; set; }

        public string State { get; set; }

        public string Icon { get; set; }

        public PingPayload Ping { get; set; }
    }
}
