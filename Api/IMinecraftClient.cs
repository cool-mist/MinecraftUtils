using System;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftUtils.Api
{
    public interface IMinecraftClient : IDisposable
    {
        /// <summary>
        /// Get the current <see cref="IMinecraftState"/>.
        /// </summary>
        Task<ITaskResponse<IMinecraftState>> GetStateAsync(string serverHost, int serverPort, CancellationToken cancellationToken);

        /// <summary>
        /// Get the current <see cref="IMinecraftState"/>.
        /// </summary>
        Task<ITaskResponse<IMinecraftState>> GetStateAsync(string serverHost, int serverPort);

        /// <summary>
        /// Get the current <see cref="IMinecraftState"/>. Queries the default port - 25565  
        /// </summary>
        Task<ITaskResponse<IMinecraftState>> GetStateAsync(string serverHost, CancellationToken cancellationToken);

        /// <summary>
        /// Get the current <see cref="IMinecraftState"/>. Queries the default port - 25565  
        /// </summary>
        Task<ITaskResponse<IMinecraftState>> GetStateAsync(string serverHost);
    }

    public interface IMinecraftState
    {
        public string Hostname { get; }
        public string Version { get; }
        public string Motd { get; }
        public int MaxPlayers { get; }
        public int OnlinePlayers { get; }
        public string State { get; }
        public string Icon { get; }
    }
}