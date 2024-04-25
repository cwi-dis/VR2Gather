using AOT;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using UnityEngine;

using Debug = UnityEngine.Debug;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Policy;

namespace VRT.Transport.WebRTC
{
    /// <summary>
    /// MonoBehaviour that controls the WebRTC peer process and the native unmanaged plugin that interfaces to it.
    /// </summary>
    public class WebRTCConnector : MonoBehaviour
    {
        public static WebRTCConnector Instance;

        [Tooltip("Path to WebRTC peer executable")]
        public string peerExecutablePath;
        [Tooltip("Run peer in a window (windows-only)")]
        public bool peerInWindow = false;
        [Tooltip("Don't close window when peer terminates (windows-only")]
        public bool peerWindowDontClose = false;
        [Tooltip("UDP Port this peer will use to communicate with this connector instance")]
        public int peerUDPPort = 8000;
        [Tooltip("IP address where peer process will be running")]
        public string peerIPAddress = "127.0.0.1";
        [Tooltip("API version of the provided WebRTCConnector.dll")]
        string api_version = "1.0";
        [Tooltip("Maximum number of tracks to receive from other WebRTC peers")]
        int maxReceiverTracks = 9;
        [Tooltip("Set to a pathname to enable WebRTCConnector plugin logging")]
        public string logFileDirectory;
        [Tooltip("Higher for more messages")]
        public int debug = 0;
        [Tooltip("(introspection) connected to peer")]
        public bool peerConnected = false;
        [Tooltip("(introspection) SFU that peer is connected to")]
        public string peerSFUAddress;
        [Tooltip("(introspection)Client ID within SFU session")]
        public int clientId = 1;
        private Process peerProcess;
        private int nTransmissionTracks = 0;

        public unsafe class WebRTCConnectorPinvoke
        {
            [DllImport("WebRTCConnector")]
            public static extern void set_logging(string log_directory, int logLevel);
            [DllImport("WebRTCConnector", CallingConvention = CallingConvention.Cdecl)]
            public static extern void RegisterDebugCallback(debugCallback cb);
            [DllImport("WebRTCConnector")]
            public static extern int initialize(string ip_send, UInt32 port_send, string ip_recv, UInt32 port_recv,
                UInt32 number_of_tiles, UInt32 client_id, string api_version);
            [DllImport("WebRTCConnector")]
            public static extern void clean_up();
            [DllImport("WebRTCConnector")]
            public static extern int send_tile(byte* data, UInt32 size, UInt32 tile_number);
            [DllImport("WebRTCConnector")]
            public static extern int get_tile_size(UInt32 client_id, UInt32 tile_number);
            [DllImport("WebRTCConnector")]
            public static extern void retrieve_tile(byte* buffer, UInt32 size, UInt32 client_id, UInt32 tile_number);

            [DllImport("WebRTCConnector")]
            public static extern int send_control(byte* data, UInt32 size);
            [DllImport("WebRTCConnector")]
            public static extern int get_control_size();
            [DllImport("WebRTCConnector")]
            public static extern void retrieve_control(byte* buffer);
        }

        // Create string param callback delegate
        public delegate void debugCallback(IntPtr request, int color, int size);
        enum Color { red, green, blue, black, white, yellow, orange };
        [MonoPInvokeCallback(typeof(debugCallback))]
        static void OnDebugCallback(IntPtr request, int color, int size)
        {
            // Ptr to string
            string debug_string = Marshal.PtrToStringAnsi(request, size);
            // Add specified color
            debug_string =
                String.Format("WebRTCConnector: {0}{1}{2}{3}{4}",
                "<color=",
                ((Color)color).ToString(), ">", debug_string, "</color>");
            // Log the string
            Debug.Log(debug_string);
        }


        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("WebRTCConnector: Instance already set, there should only be one in the scene");
            }
            Instance = this;
        }

        public void Initialize(string _peerExecutablePath, int _clientId)
        {
            if (!string.IsNullOrEmpty(_peerExecutablePath)) {
                peerExecutablePath = _peerExecutablePath;
            }
            clientId = _clientId;
       
        }

        public void AllConnectionsDone()
        {

            if (peerConnected)
            {
                Debug.LogWarning("WebRTCConnector: second call to AllConnectionsDone");
                return;
            }
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
        public void OnEnable()
        {
            Debug.Log($"WebRTCConnector: Installing message callback");
            WebRTCConnectorPinvoke.RegisterDebugCallback(OnDebugCallback);
            if (logFileDirectory == null)
            {
                logFileDirectory = "";
            }
            WebRTCConnectorPinvoke.set_logging(logFileDirectory, debug);
        }

        public void OnDestroy()
        {
            WebRTCConnectorPinvoke.clean_up();
            if (peerProcess != null)
            {
                Debug.Log("WebRTCConnector: Terminating peer");
                peerProcess.Kill();
            }
        }

        public void Update()
        {
            // xxxjack Check that peerProcess is still running
            if (peerProcess != null && peerProcess.HasExited)
            {
                Debug.LogError($"WebRTCConnector: Ieer process has exited with exit status {peerProcess.ExitCode}");
                peerProcess = null;
            }
        }

        public void StartWebRTCPeer(Uri url)
        {
            string mySFUAddress = $"{url.Host}:{url.Port}";
            if (peerProcess != null)
            {
                // A peer has already been started. Double-check it's for the correct SFU.
                if (mySFUAddress != peerSFUAddress)
                {
                    Debug.LogError($"WebRTCConnector: Want peer for SFU {mySFUAddress} but already have one for {peerSFUAddress}");
                }
                return;
            }
            peerSFUAddress = mySFUAddress;
           
        }

        public void PrepareForTransmission(int _nTracks)
        {
            nTransmissionTracks += _nTracks;
        }

    }
}
