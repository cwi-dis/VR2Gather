using VRT.Core;

namespace VRT.Transport.TCP
{
    public class TransportProtocolTCPDirect : TransportProtocol
    {
        public static void Register()
        {
            RegisterTransportProtocol("tcp", AsyncTCPDirectWriter.Factory, AsyncTCPDirectReader.Factory, AsyncTCPDirectReader_Tiled.Factory);
        }
    }

}
