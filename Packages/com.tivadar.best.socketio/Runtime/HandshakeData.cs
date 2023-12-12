using System.Collections.Generic;

namespace Best.SocketIO
{
    /// <summary>
    /// Helper class to parse and hold handshake information.
    /// </summary>
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public sealed class HandshakeData
    {
        /// <summary>
        /// Session ID of this connection.
        /// </summary>
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
        public string Sid { get; private set; }

        /// <summary>
        /// List of possible upgrades.
        /// </summary>
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
        public List<string> Upgrades { get; private set; }

        /// <summary>
        /// What interval we have to set a ping message.
        /// </summary>
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
        public int PingInterval { get; private set; }

        /// <summary>
        /// What time have to pass without an answer to our ping request when we can consider the connection disconnected.
        /// </summary>
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
        public int PingTimeout { get; private set; }

        /// <summary>
        /// This defines how many bytes a single message can be, before the server closes the socket.
        /// </summary>
        public int MaxPayload { get; private set; }
    }
}
