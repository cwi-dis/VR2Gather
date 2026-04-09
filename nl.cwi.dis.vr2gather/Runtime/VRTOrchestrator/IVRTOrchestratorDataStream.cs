using System;
using VRT.Orchestrator.Responses;

namespace VRT.Orchestrator.Wrapping
{
    /// <summary>
    /// Orchestrator interface for binary data streams between participants:
    /// declaring, sending, and receiving raw byte streams. This is the interface
    /// used by the SocketIO transport. The method names match the wire-protocol
    /// commands (DeclareDataStream, RegisterForDataStream, etc.).
    /// </summary>
    public interface IVRTOrchestratorDataStream : IVRTOrchestratorSessionState
    {
        event Action<UserDataStreamPacket> OnDataStreamReceived;
        void DeclareDataStream(string streamType);
        void RemoveDataStream(string streamType);
        void RegisterForDataStream(string userId, string streamType);
        void UnregisterFromDataStream(string userId, string streamType);
        void SendData(string streamType, byte[] data);
    }
}
