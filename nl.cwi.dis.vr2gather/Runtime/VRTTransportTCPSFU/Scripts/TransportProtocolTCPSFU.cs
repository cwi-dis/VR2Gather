using VRT.Core;

namespace VRT.Transport.TCPSFU
{
    public class TransportProtocolTCPSFU : TransportProtocol
    {
        public static void Register()
        {
            RegisterTransportProtocol("tcpsfu", AsyncTCPSFUWriter.Factory, AsyncTCPSFUReader.Factory, AsyncTCPSFUReader.Factory_Tiled);
        }
    }

}
