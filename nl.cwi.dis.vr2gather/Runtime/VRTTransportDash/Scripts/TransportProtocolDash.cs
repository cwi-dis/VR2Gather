using VRT.Core;

namespace VRT.Transport.Dash
{
    public class TransportProtocolDash : TransportProtocol
    {
        public static void Register()
        {
            RegisterTransportProtocol("dash", AsyncDashWriter.Factory, AsyncDashReader.Factory, AsyncDashReader_Tiled.Factory);
        }

        public static string CombineUrl(string url, string streamName, bool wantMpd)
        {
            if (!url.EndsWith("/"))
            {
                url += "/";
            }
            url += streamName + "/";
            if (wantMpd)
            {
                url += streamName + ".mpd";
            }

            return url;
        }
    }

}
