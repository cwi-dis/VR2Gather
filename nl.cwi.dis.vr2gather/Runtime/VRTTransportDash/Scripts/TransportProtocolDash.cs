using VRT.Core;

namespace VRT.Transport.Dash
{
    public class TransportProtocolDash : TransportProtocol
    {
        public static void Register()
        {
            RegisterTransportProtocol("dash", AsyncDashWriter.Factory, AsyncDashReader.Factory, AsyncDashReader_Tiled.Factory);
        }
    }

}
