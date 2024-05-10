using Cwipc;

namespace VRT.Core
{
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;

    abstract public class TransportProtocol {

    }

    public interface IAsyncWriter
    {
        public string Name();
        public SyncConfig.ClockCorrespondence GetSyncInfo();
        public void Stop();
        public void StopAndWait();

    }

    public interface ITransportProtocolWriter : IAsyncWriter {
        public ITransportProtocolWriter Init(string _url, string _streamName, string fourcc, OutgoingStreamDescription[] _descriptions);
        
    }

    public interface IAsyncReader
    {
        public string Name();
        public void SetSyncInfo(SyncConfig.ClockCorrespondence _clockCorrespondence);
        public void StopAndWait();

    }

    public interface ITransportProtocolReader : IAsyncReader {
        public ITransportProtocolReader Init(string _url, string _streamName, int streamIndex, string fourcc, QueueThreadSafe outQueue);

    }

    public interface ITransportProtocolReader_Tiled : IAsyncReader {
        public ITransportProtocolReader_Tiled Init(string _url, string _streamName, string fourcc, IncomingTileDescription[] _tileDescriptors);

    }

   
}
