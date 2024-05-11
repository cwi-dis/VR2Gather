using System.Collections.Generic;
using UnityEngine;
using Cwipc;

namespace VRT.Core
{
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;

    abstract public class TransportProtocol {
        private static Dictionary<string, ITransportProtocolWriter.Factory> writers = new();
        private static Dictionary<string, ITransportProtocolReader.Factory> readers = new();
        private static Dictionary<string, ITransportProtocolReader_Tiled.Factory> readers_tiled = new();
        static protected void RegisterTransportProtocol(string protocol, ITransportProtocolWriter.Factory writer, ITransportProtocolReader.Factory reader, ITransportProtocolReader_Tiled.Factory reader_tiled)
        {
            writers[protocol] = writer;
            readers[protocol] = reader;
            readers_tiled[protocol] = reader_tiled;
            Debug.Log($"TransportProtocol: Registered {protocol}");
        }

        public ITransportProtocolWriter NewWriter(string protocol)
        {
            return writers[protocol]();
        }

        public ITransportProtocolReader NewReader(string protocol)
        {
            return readers[protocol]();
        }

        public ITransportProtocolReader_Tiled NewReader_Tiled(string protocol)
        {
            return readers_tiled[protocol]();
        }
    }

    public interface IAsyncWriter
    {
        public string Name();
        public SyncConfig.ClockCorrespondence GetSyncInfo();
        public void Stop();
        public void StopAndWait();

    }

    public interface ITransportProtocolWriter : IAsyncWriter {
        public delegate ITransportProtocolWriter Factory();
        public ITransportProtocolWriter Init(string _url, string _streamName, string fourcc, OutgoingStreamDescription[] _descriptions);
        
    }

    public interface IAsyncReader
    {
        public string Name();
        public void SetSyncInfo(SyncConfig.ClockCorrespondence _clockCorrespondence);
        public void StopAndWait();

    }

    public interface ITransportProtocolReader : IAsyncReader {
        public delegate ITransportProtocolReader Factory();
        public ITransportProtocolReader Init(string _url, string _streamName, int streamIndex, string fourcc, QueueThreadSafe outQueue);

    }

    public interface ITransportProtocolReader_Tiled : IAsyncReader {
        public delegate ITransportProtocolReader_Tiled Factory();
        public ITransportProtocolReader_Tiled Init(string _url, string _streamName, string fourcc, IncomingTileDescription[] _tileDescriptors);

    }

   
}
