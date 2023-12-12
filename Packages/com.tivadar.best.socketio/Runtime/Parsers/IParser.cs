using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.SocketIO.Parsers
{
    public interface IParser
    {
        IncomingPacket Parse(SocketManager manager, string data);
        IncomingPacket Parse(SocketManager manager, BufferSegment data, TransportEventTypes transportEvent = TransportEventTypes.Unknown);
        IncomingPacket MergeAttachements(SocketManager manager, IncomingPacket packet);

        OutgoingPacket CreateOutgoing(TransportEventTypes transportEvent, string payload);
        OutgoingPacket CreateOutgoing(Socket socket, SocketIOEventTypes socketIOEvent, int id, string name, object arg);
        OutgoingPacket CreateOutgoing(Socket socket, SocketIOEventTypes socketIOEvent, int id, string name, object[] args);
    }
}
