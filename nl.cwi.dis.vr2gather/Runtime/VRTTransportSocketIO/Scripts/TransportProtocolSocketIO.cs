using VRT.Core;

namespace VRT.Transport.SocketIO
{
    public class TransportProtocolSocketIO : TransportProtocol
    {
        public static void Register()
        {
            RegisterTransportProtocol("socketio", AsyncSocketIOWriter.Factory, AsyncSocketIOReader.Factory, AsyncSocketIOReader.Factory_Tiled);
        }
    }

}
