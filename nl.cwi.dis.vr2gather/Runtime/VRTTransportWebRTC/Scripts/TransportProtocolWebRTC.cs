using AOT;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using UnityEngine;
using VRT.Core;

using Debug = UnityEngine.Debug;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Policy;
using Cwipc;

namespace VRT.Transport.WebRTC
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
  
   public class TransportProtocolWebRTC : TransportProtocol
    {
        public static void Register()
        {
            RegisterTransportProtocol("webrtc", AsyncWebRTCWriter.Factory, AsyncWebRTCReader.Factory, AsyncWebRTCReader_Tiled.Factory_Tiled);
        }
        private static TransportProtocolWebRTC _Instance;
        private static string _InstanceURL;

        const string logFileDirectory = "";
        const int debugLevel = 1;

        const string api_version = "1.0";
        private string peerSFUAddress;

        // Settable parameters from config.json:
        public string peerExecutablePath;
        public bool peerInWindow = false;
        public bool peerWindowDontClose = false;
        public int peerUDPPort = 8000;
        public string peerIPAddress = "127.0.0.1";
        
        public int clientId = -1;
        public bool peerConnected = false;
        
        private Process peerProcess;

        private int nTransmitters = 0;
        private int nReceivers = 0;
        private int nTransmitterTracks = 0;
        private int nReceiverTracks = 0; // xxxjack needs to be set dynamically
        private int maxReceiverTracks = 9;
     
        public static string Name() {
            return "TransportProtocolWebRTC";
        }

        public static TransportProtocolWebRTC Connect(string url)
        {
            if (_Instance == null)
            {
                
                _Instance = new TransportProtocolWebRTC(url);
                _InstanceURL = url;
                return _Instance;
            }
            if (_InstanceURL == url)
            {
                return _Instance;
            }
            throw new System.Exception($"{Name()}: request connection to {url} but {_InstanceURL} already connected");
        }

        public static TransportProtocolWebRTC GetInstance() {
            return _Instance;
        }
        private TransportProtocolWebRTC(string _url) 
        {
            Uri url = new(_url);
            peerSFUAddress = $"{url.Host}:{url.Port}";
            WebRTCConnectorPinvoke.ConfigureDebug(logFileDirectory, debugLevel);
        }

     

        public void SetClientID(int _clientId)
        {
            clientId = _clientId;
        }

        public void AllConnectionsDone()
        {
            lock(this) {
                if (clientId < 0) {
                    Debug.LogError($"{Name()}: AllConnectionsDone called but clientID not yet set");
                    return;
                }
                if (peerConnected)
                {
                    Debug.LogWarning($"{Name()}: second call to AllConnectionsDone");
                    return;
                }
                // Get settings from the config file
                peerExecutablePath = VRTConfig.Instance.TransportWebRTC.peerExecutablePath;
                peerInWindow = VRTConfig.Instance.TransportWebRTC.peerInWindow;
                peerWindowDontClose = VRTConfig.Instance.TransportWebRTC.peerWindowDontClose;
                peerUDPPort = VRTConfig.Instance.TransportWebRTC.peerUDPPort;
                peerIPAddress = VRTConfig.Instance.TransportWebRTC.peerIPAddress;
                
                // xxxjack this is not correct for built Unity players.
                string appPath = System.IO.Path.GetDirectoryName(Application.dataPath);
                peerExecutablePath = System.IO.Path.Combine(appPath, peerExecutablePath);
                
                // Replace %PLATFORM% in peerExecutablePath
                string platform = "unknown";
                switch(Application.platform) {
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                        platform = "macos";
                        break;
                    case RuntimePlatform.Android:
                        platform = "android";
                        break;
                        case RuntimePlatform.WindowsEditor:
                        case RuntimePlatform.WindowsPlayer:
                        platform = "win";
                        break;
                }
                peerExecutablePath = peerExecutablePath.Replace("%PLATFORM%", platform);

                peerProcess = new Process();
                peerProcess.StartInfo.FileName = peerExecutablePath;
                peerProcess.StartInfo.Arguments = $"-p :{peerUDPPort} -i -o -sfu {peerSFUAddress} -c {clientId}";
                peerProcess.StartInfo.CreateNoWindow = !peerInWindow;
    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                if (peerInWindow && peerWindowDontClose)
                {
                    // xxxjack this will fail if there are spaces in the pathname. But escaping them is impossible on Windows.
                    peerProcess.StartInfo.Arguments = $"/K {peerProcess.StartInfo.FileName} {peerProcess.StartInfo.Arguments}";
                    peerProcess.StartInfo.FileName = "CMD.EXE";
                }
    #endif
                Debug.Log($"{Name()}: Start peer: {peerProcess.StartInfo.FileName} {peerProcess.StartInfo.Arguments}");
                try
                {
                    if (!peerProcess.Start())
                    {
                        Debug.LogError($"{Name()}: Cannot start peer");
                        peerProcess = null;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"{Name()}: Cannot start peer: {e.Message}");
                    peerProcess = null;
                }

                int nTracks = nReceiverTracks;
                if (nTransmitterTracks > nTracks)
                {
                    nTracks = nTransmitterTracks;
                }
                if (maxReceiverTracks > nTracks)
                {
                    nTracks = maxReceiverTracks;
                }
                Debug.Log($"{Name()}: nReceiver={nReceivers}, nReceiverTracks={nReceiverTracks}, maxReceiverTracks={maxReceiverTracks}, nTransmitters={nTransmitters}, nTransmitterTracks={nTransmitterTracks}");
                //Thread.Sleep(2000);
                int status = WebRTCConnectorPinvoke.initialize(peerIPAddress, (uint)peerUDPPort, peerIPAddress, (uint)peerUDPPort, (uint)nTracks, (uint)clientId, api_version);
                //Thread.Sleep(1000);
                peerConnected = true;
                Debug.Log($"{Name()}: WebRTCConnector initialized, status={status}");                
            }
        }

        // Use this for initialization
      

        public void Stop()
        {
            WebRTCConnectorPinvoke.clean_up();
            peerConnected = false;
            if (peerProcess != null)
            {
                Debug.Log($"{Name()}: Terminating peer");
                try {
                    peerProcess.Kill();
                }
                catch(InvalidOperationException)
                {

                }
            }
        }


        public void RegisterTransmitter(int _nTracks)
        {
            if (peerConnected) {
                throw new Exception($"{Name()}: RegisterTransmitter() called after peer started");
            }
            nTransmitterTracks += _nTracks;
            nTransmitters++;
        }

        public void RegisterReceiver(int _nTracks)
        {
#if disabled
            // Unfortunately this doesn't work: the receivers aren't started until the
            // session is well underway.
            if (peerConnected) {
                throw new Exception($"{Name()}: RegisterReceiver() called after peer started");
            }
#endif
            nReceiverTracks += _nTracks;
            if (nReceiverTracks > maxReceiverTracks) {
                Debug.LogWarning($"{Name()}: Too many receiver tracks: {nReceiverTracks}, max={maxReceiverTracks}");
            }
            nReceivers++;
        }

        public void UnregisterTransmitter()
        {
            nTransmitters--;
            if (nTransmitters <= 0 && nReceivers <= 0)
            {
                Stop();
            }
        }

        public void UnregisterReceiver()
        {
            nReceivers--;
            if (nTransmitters <= 0 && nReceivers <= 0)
            {
                Stop();
            }
        }

        public NativeMemoryChunk GetNextTile(int thread_index, uint fourcc)
        {
            lock(this)
            {
                if (!peerConnected)
                {
                    Debug.LogWarning($"{Name()}: GetNextTile() called before peer connection");
                    return null;
                }
                // [jvdhooft]
                // xxxjack the following code is very inefficient (all data is copied).
                // See the comment in AsyncWebRTCWriter for details, but suffice it to say here that it's much better if
                // retreive_tile had two sets of pointer, len.
                int p_size = WebRTCConnectorPinvoke.get_tile_size((uint)clientId, (uint)thread_index);
                if (p_size <= 0)
                {
                    return null;
                }
                Debug.Log($"{Name()}: WebRTC frame available for {thread_index}, size={p_size}");
                byte[] messageBuffer = new byte[p_size];
                unsafe
                {
                    fixed (byte* bufferPointer = messageBuffer)
                    {
                        WebRTCConnectorPinvoke.retrieve_tile(bufferPointer, (uint)p_size, (uint)clientId, (uint)thread_index);
                    }
                }
                int fourccReceived = BitConverter.ToInt32(messageBuffer, 0);
                if (fourccReceived != fourcc)
                {
                    Debug.LogError($"{Name()}: expected 4CC 0x{fourcc:x} got 0x{fourccReceived:x}");
                }
                int dataSize = BitConverter.ToInt32(messageBuffer, 4);
                Timestamp timestamp = BitConverter.ToInt64(messageBuffer, 8);
                NativeMemoryChunk mc = new NativeMemoryChunk(dataSize);
                mc.metadata.timestamp = timestamp;
                System.Runtime.InteropServices.Marshal.Copy(messageBuffer[16..], 0, mc.pointer, dataSize);
                return mc;            }
        }

        public bool SendTile(NativeMemoryChunk mc, int tile_number, uint fourcc)
        {
            lock(this)
            {
                if (!peerConnected)
                {
                    Debug.LogWarning($"{Name()}: SendTile() called before peer connection");
                    return false;
                }
                // [jvdhooft]
                // xxxjack the following code is very inefficient (all data is copied).
                // NativeMemoryChunk has the data in an unmanaged buffer, and here we are copying it back to a
                // managed byte array so that we can prepend the header.
                //
                // It would be better if send_tile would have two ptr, len sets so we could use the first one for the header
                // and the second one for the data.
                byte[] hdr = new byte[16];
                var hdr1 = BitConverter.GetBytes(fourcc);
                hdr1.CopyTo(hdr, 0);
                var hdr2 = BitConverter.GetBytes((Int32)mc.length);
                hdr2.CopyTo(hdr, 4);
                var hdr3 = BitConverter.GetBytes(mc.metadata.timestamp);
                hdr3.CopyTo(hdr, 8);
                var buf = new byte[mc.length];
                System.Runtime.InteropServices.Marshal.Copy(mc.pointer, buf, 0, mc.length);
                mc.free();
                byte[] messageBuffer = new byte[hdr.Length + buf.Length];
                System.Buffer.BlockCopy(hdr, 0, messageBuffer, 0, hdr.Length);
                System.Buffer.BlockCopy(buf, 0, messageBuffer, hdr.Length, buf.Length);
                int actualLength;
                int dataLength = hdr.Length + buf.Length;
                unsafe
                {
                    fixed(byte* bufferPointer = messageBuffer)
                    {
                        actualLength = WebRTCConnectorPinvoke.send_tile(bufferPointer, (uint)dataLength, (uint)tile_number);
                    }
                }
                if (actualLength < dataLength) {
                    Debug.LogWarning($"{Name()}: send_tile(..., {dataLength}) returned {actualLength}");
                }
                return true;                  
            }
        }
    }
}