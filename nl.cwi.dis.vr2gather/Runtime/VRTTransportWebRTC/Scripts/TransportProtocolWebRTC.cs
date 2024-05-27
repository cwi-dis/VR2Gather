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
    public class TransportProtocolWebRTC : TransportProtocol
    {
        public static void Register()
        {
            RegisterTransportProtocol("webrtc", AsyncWebRTCWriter.Factory, AsyncWebRTCReader.Factory, AsyncWebRTCReader.Factory_Tiled);
        }
        public static TransportProtocolWebRTC Instance;
        private static string _InstanceURL;

        const string logFileDirectory = "";
        const int debugLevel = 1;
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
        private int nTransmissionTracks = 0;
        private int maxReceiverTracks = 9;
     
        public static string Name() {
            return "TransportProtocolWebRTC";
        }

        public static TransportProtocolWebRTC Connect(string url)
        {
            if (Instance == null)
            {
                
                Instance = new TransportProtocolWebRTC(url);
                _InstanceURL = url;
                return Instance;
            }
            if (_InstanceURL == url)
            {
                return Instance;
            }
            throw new System.Exception($"{Name()}: request connection to {url} but {_InstanceURL} already connected");
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
            if (clientId < 0) {
                Debug.LogError($"{Name()}: AllConnectionsDone called but clientID not yet set");
                return;
            }
            if (peerConnected)
            {
                Debug.LogWarning("WebRTCConnector: second call to AllConnectionsDone");
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
            Debug.Log($"WebRTCConnector: Start {peerProcess.StartInfo.FileName} {peerProcess.StartInfo.Arguments}");
            try
            {
                if (!peerProcess.Start())
                {
                    Debug.LogError($"WebRTCConnector: Cannot start peer");
                    peerProcess = null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"WebRTCConnector: Cannot start peer: {e.Message}");
                peerProcess = null;
            }

            int nTracks = maxReceiverTracks;
            if (nTransmissionTracks > nTracks)
            {
                nTracks = nTransmissionTracks;
            }
            //Thread.Sleep(2000);
            WebRTCConnectorPinvoke.initialize(peerIPAddress, (uint)peerUDPPort, peerIPAddress, (uint)peerUDPPort, (uint)nTracks, (uint)clientId, api_version);
            //Thread.Sleep(1000);
            peerConnected = true;
        }

        // Use this for initialization
      

        public void OnDestroy()
        {
            WebRTCConnectorPinvoke.clean_up();
            if (peerProcess != null)
            {
                Debug.Log("WebRTCConnector: Terminating peer");
                peerProcess.Kill();
            }
        }


        public void PrepareForTransmission(int _nTracks)
        {
            nTransmissionTracks += _nTracks;
        }

    }
}
