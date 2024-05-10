using Cwipc;

namespace VRT.Core
{
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;

    abstract public class TransportProtocol {

    }

    abstract public class TransportProtocolWriter : AsyncWriter {
        abstract public TransportProtocolWriter Init(string _url, string _streamName, string fourcc, OutgoingStreamDescription[] _descriptions);
        
    }

    public interface ITransportProtocolReader {
        abstract public TransportProtocolReader Init(string _url, string _streamName, int streamIndex, string fourcc, QueueThreadSafe outQueue);

    }

    abstract public class TransportProtocolReader : AsyncReader, ITransportProtocolReader {
        abstract public TransportProtocolReader Init(string _url, string _streamName, int streamIndex, string fourcc, QueueThreadSafe outQueue);
    }

    public interface ITransportProtocolReader_PC {
        abstract public TransportProtocolReader Init(string _url, string _streamName, string fourcc, IncomingTileDescription[] _tileDescriptors);

    }

    public interface ITransportProtocolReader_AV  {
        abstract public TransportProtocolReader Init(string url, string streamName, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue);

    }
   
}
