using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
namespace VRT.Core
{
    [Serializable]
    public class VRTConfig : MonoBehaviour
    {
        
        public static bool ISXRActive() {
                if (XRGeneralSettings.Instance == null) {
                    return false;
                }
                if (XRGeneralSettings.Instance.Manager == null) {
                    return false;
                }
                return XRGeneralSettings.Instance.Manager.activeLoader != null;
            }      

        [Tooltip("Orchestrator SocketIO endpoint URL")]
        public string orchestratorURL = "";
        [Tooltip("If nonzero: target frame rate. -1 is system default. (ignored when using HMD)")]
        public int targetFrameRate = -1; // system default framerate
        [Tooltip("Maximum NTP desync allowed before a warning is shown")]
        public float ntpSyncThreshold = 1.0f;
        [Tooltip("Audio sample rate. NOTE: must match between all instances")]
        public readonly int audioSampleRate = 48000;
        [Tooltip("If nonzero: number of seconds between stats: lines. If zero: every event")]
        public double statsInterval = 10.0;
        [Tooltip("Path name of stats: output file. Empty: to console log")]
        public string statsOutputFile = "";
        [Tooltip("Append to stats: file in stead of overwriting")]
        public bool statsOutputFileAppend = true;
        [Tooltip("If not empty: directory path where ffmpeg native DLLs are stored")]
        public string ffmpegDLLDir = "";

        [Serializable]
        public class _ScreenshotTool
        {
            public bool takeScreenshot = false;
            public string screenshotTargetDirectory = "";
        }
        public _ScreenshotTool ScreenshotTool;

        [Serializable]
        public class _AutoStart
        {
            // This class allows to setup a machine (through config.json) to
            // - automatically login,
            // - automatically create a session with a given name and parameters
            // - automatically join a session of a given name
            // - automatically start a session when enough people have joined
            [Tooltip("Automatically login with predefined credentials")]
            public bool autoLogin = false;
            [Tooltip("Automatically create a session")]
            public bool autoCreate = false;
            [Tooltip("Automatically join a session")]
            public bool autoJoin = true;
            [Tooltip("If not empty: autoCreate for this user, autoJoin for all others")]
            public string autoCreateForUser = "";
            [Tooltip("AutoCreate and AutoJoin:Session name")]
            public string sessionName = "";
            [Tooltip("AutoCreate: Scenario name")]
            public string sessionScenario = "";
            [Tooltip("AutoCreate: Transport protocol")]
            public string sessionTransportProtocol = "";
            [Tooltip("AutoCreate: Uncompressed pointclouds")]
            public bool sessionUncompressed = false;
            [Tooltip("AutoCreate: Uncompressed audio")]
            public bool sessionUncompressedAudio = false;
            [Tooltip("AutoCreate: Start session when this many users have joined")]
            public int autoStartWith = -1;
            [Tooltip("Automatically leave session after this many seconds (if > 0)")]
            public float autoLeaveAfter = 0f;
            [Tooltip("Automatically quit application after leaving")]
            public bool autoStopAfterLeave = false;
            [Tooltip("Delay in seconds between automatic login/create/join step")]
            public float autoDelay = 0.2f;
        };
        [Tooltip("Automation of LoginManager dialogs")]
        public _AutoStart AutoStart;

        [Serializable]
        public class _Macintosh
        {
            public string SIGNALS_SMD_PATH;
        };
        public _Macintosh Macintosh;

        [Serializable]
        public class _TransportDash
        {
            [Tooltip("How many milliseconds one transmitted DASH segment should contain")]
            public int segmentSize;
            [Tooltip("After how many milliseconds DASH segments can be deleted by the SFU")]
            public int segmentLife;
        }
        [Tooltip("Settable parameters for DASH protocol")]
        public _TransportDash TransportDash;
        [Serializable]
        public class _TransportWebRTC
        {
            public string peerExecutablePath;
            public bool peerInWindow = false;
            public bool peerWindowDontClose = false;
            public int peerUDPPort = 8000;
            public string peerIPAddress = "127.0.0.1";
            public string logFileDirectory = null;
            public int debugLevel = 0;
        }
        public _TransportWebRTC TransportWebRTC;

        [Serializable]
        public class _PC : Cwipc.CwipcConfig {
            [Tooltip("If non-zero, sets the limit on the number of point clouds buffered for output (otherwise a sensible default is used)")]
            public int preparerQueueSize = 0;

        }
        public _PC PCs;

        [Serializable]
        public class _Voice
        {
            [Tooltip("Approximate voice input frame rate (will be rounded down to intgral number of DSP buffers)")]
            public int audioFps = 50;
            [Tooltip("If > 0, voice output is further behind its natural playout time than this, and data is available, we will drop packets to catch up")]
            public float maxPlayoutLatency = 0.3f;
            [Tooltip("If voice output is further ahead of its natural playout time than this we will insert silence to allow the other streams to catch up")]
            public float maxPlayoutAhead = 0.066f;
            [Tooltip("If non-zero, sets the limit on the number of audio packets buffered for voice output (otherwise a sensible default is used)")]
            public int preparerQueueSize = 0;
            [Tooltip("If true voice output will run at its own speed, unsynchronized with other streams, its natural playout clock determined by the local system clock")]
            public bool ignoreSynchronizer = false;
        }
        [Tooltip("Conversational audio settings")]
        public _Voice Voice;

        [Serializable]
        public class _Synchronizer
        {
            [Tooltip("Enable to get lots of log messages on Synchronizer use")]
            public bool debugSynchronizer = false;
            [Tooltip("Enable to get log messages on jitter buffer adaptations")]
            public bool debugJitterBuffer = false;

            [Tooltip("Minimum preferred playout latency in milliseconds")]
            public int minLatency = 0;
            [Tooltip("Maximum preferred playout latency, reset to minLatency if we reach this latency")]
            public int maxLatency = 0;

            [Tooltip("Limit by how much we decrease preferred latency")]
            public int latencyMaxDecrease = 1;
            [Tooltip("Limit by how much we increase preferred latency")]
            public int latencyMaxIncrease = 33;

            [Tooltip("If not all streams have data available play out unsynced (false: delay until data is available)")]
            public bool acceptDesyncOnDataUnavailable = false;
        }
        [Tooltip("Avatar media stream synchronizer parameters")]
        public _Synchronizer Synchronizer;

        [Serializable]
        public class _User
        {
            [Tooltip("local filename where orchestrator config is stored")]
            public string orchestratorConfigFilename;
            [Serializable]
            public class _PCSelfConfig
            {
                [Serializable]
                public enum PCCapturerType
                {
                    auto,
                    none,
                    realsense,
                    kinect,
                    synthetic,
                    remote,
                    proxy,
                    prerecorded,
                    developer
                };
                public PCCapturerType capturerType;
                [Tooltip("Override capturerType by name")]
                public string capturerTypeName;
                [Serializable]
                public class _CameraReaderConfig
                {
                    public string configFilename;
                }
                public _CameraReaderConfig CameraReaderConfig;
                [Serializable]
                public class _RemoteCameraReaderConfig
                {
                    public string url;
                    public bool isCompressed;
                }
                public _RemoteCameraReaderConfig RemoteCameraReaderConfig;
                [Serializable]
                public class _ProxyReaderConfig
                {
                    public string localIP;
                    public int port;
                }
                public _ProxyReaderConfig ProxyReaderConfig;
                [Serializable]
                public class _SynthReaderConfig
                {
                    public int nPoints;
                }
                public _SynthReaderConfig SynthReaderConfig;
                [Serializable]
				public class _PrerecordedReaderConfig
				{
					public string folder;
                };
				public _PrerecordedReaderConfig PrerecordedReaderConfig;
				
                public float voxelSize;
                public float frameRate;
                public bool tiled;
                [Serializable]
                public class _Encoder
                {
                    public int octreeBits;
                }
                public _Encoder[] Encoders;
            }
            public _PCSelfConfig PCSelfConfig;

        };
        [Tooltip("Point cloud avatar capturer, encoder and transmission parameters")]
        public _User LocalUser;

        [Tooltip("Introspection: Config override JSON file used")]
        public string configOverrideFilename;

#if UNITY_EDITOR
        [ContextMenu("Save as config.json")]
        private void SaveAsConfigJson()
        {
            string file = ConfigFilename();
            System.IO.File.WriteAllText(file, JsonUtility.ToJson(this, true));
            Debug.Log($"VRTConfig: Saving configuration to {file}");
        }

        [ContextMenu("Load from config.json")]
        private void LoadFromConfigJson()
        {
            string file = ConfigFilename();
            JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(file), this);
            Debug.Log($"VRTConfig: Loaded configuration from {file}");
        }
#endif
        static VRTConfig _Instance;
        public static VRTConfig Instance
        {
            get
            {
                if (_Instance == null)
                {
                    Debug.LogError("VRTConfig: Instance accessed before allocation. Must be on a Component that is initialized very early.");
                }
                return _Instance;
            }
            
        }

        private void Awake()
        {
            if (_Instance != null)
            {
                Debug.LogWarning($"VRTConfig: Awake() called but there is an Instance already from {_Instance.gameObject}. Keeping the old one.");
                Destroy(gameObject);
                return;
            }
            Initialize();
        }

        private void Initialize()
        {
            string file = ConfigFilename();
            configOverrideFilename = file;
            if (System.IO.File.Exists(file))
            {
                Debug.Log($"VRTConfig: override settings from {file}");
                JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(file), this);
            }
            else
            {
                Debug.LogWarning($"VRTConfig: override file not found: {file}");
            }
            //
            // Update various settings after reading configfile overrides
            //
            if (targetFrameRate != 0)
            {
                Application.targetFrameRate = this.targetFrameRate;
                if (Application.targetFrameRate > 0)
                {
                    Debug.LogWarning($"VRTCore.Config: Application.targetFrameRate set to {Application.targetFrameRate}");
                }
            }
            if (LocalUser.PCSelfConfig.capturerTypeName != null && LocalUser.PCSelfConfig.capturerTypeName != "") {
                if (!Enum.TryParse(LocalUser.PCSelfConfig.capturerTypeName, out LocalUser.PCSelfConfig.capturerType))
                {
                    Debug.LogError($"VRTCore.Config: Unknown capturerTypeName \"{LocalUser.PCSelfConfig.capturerTypeName}\"");
                    LocalUser.PCSelfConfig.capturerType = _User._PCSelfConfig.PCCapturerType.none;
                }
            }
            // Initialize some other modules that have their own configuration.
#if VRT_WITH_STATS
            Statistics.Initialize(this.statsInterval, this.statsOutputFile, this.statsOutputFileAppend);
#endif
            Cwipc.CwipcConfig.SetInstance(this.PCs);
            _Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        static string _ConfigFilenameFromCommandLineArgs()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i=0; i<arguments.Length-1; i++)
            {
                if (arguments[i] == "-vrt-config") return arguments[i + 1];
            }
            return null;
        }

        public static string ConfigFilename(string filename="config.json")
        {
            if (filename == "config.json")
            {
                string clConfigFile = _ConfigFilenameFromCommandLineArgs();
                if (clConfigFile != null)
                {
                    clConfigFile = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), clConfigFile);
                    return clConfigFile;
                }
            }
            string dataPath;
            if (Application.isEditor)
            {
                // In the editor the config file is at the toplevel, above the Assets folder
                dataPath = System.IO.Path.GetDirectoryName(Application.dataPath);
            }
            else if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                // For the Mac player, the config file is in the Contents directory, which is dataPath
                dataPath = Application.dataPath;
            } else if (Application.platform == RuntimePlatform.Android)
            {
                dataPath = Application.persistentDataPath; // xxxjack for debugging
            } else
            {
                // For Windos/Linux player, the config file is in the same directory as the executable
                // For both cases, this is the parent of Application.dataPath.
                // For future reference: this scheme will not work for iOS and Windows Store (which will need to use
                // something based on persistentDataPath)
                dataPath = System.IO.Path.GetDirectoryName(Application.dataPath);
            }
            return System.IO.Path.Combine(dataPath, filename);
        }
    }
}