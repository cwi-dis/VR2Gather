using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;
using Cwipc;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
namespace VRT.Core
{
    [Serializable]
    public class VRTConfig : MonoBehaviour
    {
        const int CurrentConfigVersion = 20260311;
        [Tooltip("Version of this config file/struct. Do not change.")]
        public int configVersion = CurrentConfigVersion;
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
        [Tooltip("local filename where orchestrator config is stored")]
        public string userConfigFilename;
        [Tooltip("Introspection: Config override JSON file used")]
        public string configOverrideFilename;
        [Tooltip("If nonzero: target frame rate. -1 is system default. (ignored when using HMD)")]
        public int targetFrameRate = -1; // system default framerate
        [Tooltip("Maximum NTP desync allowed before a warning is shown")]
        public float ntpSyncThreshold = 1.0f;
        [Tooltip("If not empty: directory path where ffmpeg native DLLs are stored")]
        public string ffmpegDLLDir = "";
        [Tooltip("Automatically invent a username if not set")]
        public bool autoInventUsername = true;

        [Serializable]
        public class StatisticsConfigType
        {
            [Tooltip("If nonzero: number of seconds between stats: lines. If zero: every event")]
            public double interval = 10.0;
            [Tooltip("Path name of stats: output file. Empty: to console log")]
            public string outputFile = "";
            [Tooltip("Append to stats: file in stead of overwriting")]
            public bool outputFileAppend = true;

        };
        [Tooltip("Settings for VRTStatistics-style runtime statistics")]
        public StatisticsConfigType StatisticsConfig;
        
        [Serializable]
        public class ScreenshotConfigType
        {
            [Tooltip("Set this to true to enable the screenshot tool")]
            public bool takeScreenshot = false;
            [Tooltip("FPS for recording. Default is game FPS")]
            public int fps = 0;
            [Tooltip("Directory where screenshots will be deposited")]
            public string screenshotTargetDirectory = "";
            [Tooltip("Set to true to delete directory before starting recording")]
            public bool preDeleteTargetDirectory = true;
            [Tooltip("filename format. Can include {ts}, {num} and {framenum} constructs")]
            public string filenameTemplate = "Frame{framenum}.png";
        }
        [Tooltip("Settings for screenshot tool")]
        public ScreenshotConfigType ScreenshotConfig;

        [Serializable]
        public class AutoStartConfigType
        {
            // This class allows to setup a machine (through config.json) to
            // - automatically login,
            // - automatically create a session with a given name and parameters
            // - automatically join a session of a given name
            // - automatically start a session when enough people have joined
            [Tooltip("Ignore autoStart settings when in developer mode")]
            public bool ignoreAutoStartForDeveloper = false;
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
        public AutoStartConfigType AutoStartConfig;

        [Serializable]
        public class TransportDashConfigType
        {
            [Tooltip("How many milliseconds one transmitted DASH segment should contain")]
            public int segmentSize = 4000;
            [Tooltip("After how many milliseconds DASH segments can be deleted by the SFU")]
            public int segmentLife = 15000;
            [Tooltip("lldash log level: 0-Error, 1-Warn, 2-Info, 3-Debug, 4-APIDebug")]
            public int logLevel = 0;
            [Tooltip("Override the lldash native library location.")]
            public string nativeLibraryPath = "";
        }
        [Tooltip("Settable parameters for DASH transport protocol")]
        public TransportDashConfigType TransportDashConfig;
        [Serializable]
        public class TransportWebRTCConfigType
        {
            public string peerExecutablePath;
            public bool peerInWindow = false;
            public bool peerWindowDontClose = false;
            public int peerUDPPort = 8000;
            public string peerIPAddress = "127.0.0.1";
            public string logFileDirectory = null;
            public int debugLevel = 0;
        }
        [Tooltip("Settable parameters for WebRTC transport protocol")]
        public TransportWebRTCConfigType TransportWebRTCConfig;

        [Serializable]
        public class CwipcConfigType : Cwipc.CwipcConfig {
            [Tooltip("If non-zero, sets the limit on the number of point clouds buffered for output (otherwise a sensible default is used)")]
            public int preparerQueueSize = 0;

        }
        [Tooltip("Settings for cwipc point cloud support")]
        public CwipcConfigType CwipcConfig;

        [Serializable]
        public class VoiceConfigType
        {
            [Tooltip("Audio sample rate. NOTE: must match between all instances")]
            public readonly int AudioSampleRate = 48000;
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
        public VoiceConfigType VoiceConfig;

        [Serializable]
        public class SynchronizerConfigType
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
        [Tooltip("Representation media stream synchronizer parameters")]
        public SynchronizerConfigType SynchronizerConfig;

        [Serializable]
        public class PointCloudTransmissionConfigType
        {
            [Tooltip("Set to true to transmit point clouds in multiple tiles")]
            public bool tiled;
            [Serializable]
            public class EncoderConfigType
            {
                [Tooltip("cwipc_codec octreeBits parameter (higher is better)")]
                public int octreeBits;
            }
            [Tooltip("How to encode point cloud streams. Multiple values result in multiple quality streams (per tile, possibly)")]
            public EncoderConfigType[] EncoderConfigs;
        }
        [Tooltip("Settable parameters for point cloud transmission streams")]
        public PointCloudTransmissionConfigType PointCloudTransmissionConfig;
        
        [Serializable]
        public class TileSelectorConfigType {
            [Tooltip("Algorithm for selection. Default: none")]
            public string algorithm;
            [Tooltip("Print log messages to allow debugging the decisions of the tile selector")]
            public bool debugDecisions = false;
            [Tooltip("Override bitrate budget with a static value if non-zero (in stead of measuring it)")]
            public int bitrateBudget;
        }

        [Tooltip("Tile selection algorithm parameters")]
        public TileSelectorConfigType TileSelectorConfig;

        [Serializable]
        public class PositionTrackerConfigType {
            [Tooltip("If non-empty, file where user location and gaze orientation are recorded")]
            public string outputFile;
            [Tooltip("Output recording interval override if non-zero, in milliseconds")]
            public int outputIntervalOverride;
            [Tooltip("If non-empty, file where user location and gaze orientation are played back from")]
            public string inputFile;
        }
        [Tooltip("Parameters for saving or playing back user position")]
        public PositionTrackerConfigType PositionTrackerConfig;

        [Serializable]
        public class RepresentationConfigType
        {
            [SerializeField]
            [Tooltip("Representation of this user")]
            public UserRepresentationType representation = UserRepresentationType.SimpleAvatar;
            [Tooltip("Representation of this user, as string. Overrides representation")]
            public string representation_str;
            [Tooltip("Name of webcam to use (for webcam representation")]
            public string webcamName = "";
            [Tooltip("Name of microphone to use (empty or None for no voice transmission)")]
            public string microphoneName = "";
            [Tooltip("For TCP: URL to use for representation transport protocol")]
            public string userRepresentationTCPUrl = "";
            [Serializable]
            public class RepresentationPointcloudConfigType
            {
                
                [Tooltip("PC capturer type")]
                public RepresentationPointcloudVariant variant;
                [Tooltip("Override variant (string)")]
                public string variant_str;
                [Serializable]
                public class CameraConfigType
                {
                    [Tooltip("Override cameraconfig.json")]
                    public string configFilename;
                }
                [Tooltip("Settings for camera variant")]
                public CameraConfigType CameraConfig;
                [Serializable]
                public class RemoteConfigType
                {
                    public string url;
                    public bool isCompressed;
                }
                [Tooltip("Settings for remote variant (opens TCP connection to a remote camera)")]
                public RemoteConfigType RemoteConfig;
                [Serializable]
                public class ProxyConfigType
                {
                    public string localIP;
                    public int port;
                }
                [Tooltip("Settings for proxy variant (creates TCP server to which remote camera connects)")]
                public ProxyConfigType ProxyConfig;
                [Serializable]
                public class SyntheticConfigType
                {
                    public int nPoints;
                }
                [Tooltip("Settings for synthetic point cloud variant")]
                public SyntheticConfigType SyntheticConfig;
                [Serializable]
				public class PrerecordedConfigType
				{
					public string folder;
                };
                [Tooltip("Settings for prerecorded variant (which plays back a prerecorded sequence)")]
				public PrerecordedConfigType PrerecordedConfig;
				[Tooltip("If non-zero: override voxelsize of point cloud capturer")]
                public float voxelSize;
                [Tooltip("If non-zero: override framerate of point cloud capturer")]
                public float frameRate;
                
            }
            public RepresentationPointcloudConfigType RepresentationPointcloudConfig;
        };

        [Tooltip("User representation configuration, overridden by config-user.json")]
        public RepresentationConfigType RepresentationConfig;

        public void SaveUserConfig()
        {
            // And also save a local copy, if wanted
            if (String.IsNullOrEmpty(userConfigFilename))
            {
                userConfigFilename = "config-user.json";
            }
            _PreSave();
            var configData = JsonUtility.ToJson(RepresentationConfig, true);
            var fullName = ConfigFilename(userConfigFilename, label:"User config");
            Debug.Log($"VRTConfig: Full user config filename: {fullName}");
            System.IO.File.WriteAllText(fullName, configData);
            Debug.Log($"VRTConfig: saved UserData to {fullName}");
            
        }

        public void LoadUserConfig()
        {
            if (String.IsNullOrEmpty(userConfigFilename))
            {
                userConfigFilename = "config-user.json";
            }
            var fullName = ConfigFilename(userConfigFilename, label:"User config");
            Debug.Log($"VRTConfig: Full user config filename: {fullName}");
            JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(fullName), RepresentationConfig);
            _PostLoad();
            Debug.Log($"VRTConfig: loaded UserData from {fullName}");
        }
        
#if UNITY_EDITOR
        [ContextMenu("Save as config.json and config-user.json")]
        private void SaveAsConfigJson()
        {
            string file = ConfigFilename(force:true);
            _PreSave();
            System.IO.File.WriteAllText(file, JsonUtility.ToJson(this, true));
            Debug.Log($"VRTConfig: Saved configuration to {file}");
            SaveUserConfig();
        }

        [ContextMenu("Load from config.json")]
        private void LoadFromConfigJson()
        {
            string file = ConfigFilename(force:true);
            JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(file), this);
            LoadUserConfig();
            _PostLoad();
            configOverrideFilename = file;
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

        private void _PostLoad()
        {
            // Convert all enums to their string representation
            if (!String.IsNullOrEmpty(RepresentationConfig.RepresentationPointcloudConfig.variant_str))
            {
                if (!Enum.TryParse<RepresentationPointcloudVariant>(
                    RepresentationConfig.RepresentationPointcloudConfig.variant_str,
                    true, 
                    out RepresentationConfig.RepresentationPointcloudConfig.variant))
                {
                    Debug.LogError($"VRTConfig: Invalid value for variant_str");
                }
            }

            if (!string.IsNullOrEmpty(RepresentationConfig.representation_str))
            {
                if (!Enum.TryParse<UserRepresentationType>(
                    RepresentationConfig.representation_str,
                    true,
                    out RepresentationConfig.representation))
                {
                    Debug.LogError($"VRTConfig: Invalid value for representation_str");
                }
            }
            // And ensure everything is consistent again
            _PreSave();
        }

        private void _PreSave()
        {
            RepresentationConfig.RepresentationPointcloudConfig.variant_str = RepresentationConfig.RepresentationPointcloudConfig.variant.ToString();
            RepresentationConfig.representation_str = RepresentationConfig.representation.ToString();
        }
        
        private void Initialize()
        {
            string file = ConfigFilename(force:true);
            if (System.IO.File.Exists(file))
            {
                Debug.Log($"VRTConfig: override settings from {file}");
                // Hack to ensure we read the config version
                configVersion = 0;
                JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(file), this);
                LoadUserConfig();
                _PostLoad();
                configOverrideFilename = file;
            }
            else
            {
                Debug.LogWarning($"VRTConfig: override file not found: {file}");
            }

            if (configVersion != CurrentConfigVersion)
            {
                Debug.LogError($"VRTConfig: config file is version {configVersion} in stead of expected version {CurrentConfigVersion}");
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
            // Initialize some other modules that have their own configuration.
#if VRT_WITH_STATS
            Statistics.Initialize(this.StatisticsConfig.interval, this.StatisticsConfig.outputFile, this.StatisticsConfig.outputFileAppend);
#endif
            // Communicate cwipc settings to cwipc package. This sets all sorts
            // of things, from native log level to queue sizes, etc.
            Cwipc.CwipcConfig.SetInstance(this.CwipcConfig);
            
            _Instance = this;
            DontDestroyOnLoad(this.gameObject);
            if (autoInventUsername) {
                if (!PlayerPrefs.HasKey("userNameLoginIF")) {
                    string userName = System.Environment.MachineName;
                    userName = userName.ToLower();
                    if (userName.Length > 20) {
                        userName = userName.Substring(0, 20);
                    }
                    Debug.Log($"VRTConfig: Invented username {userName}");
                    PlayerPrefs.SetString("userNameLoginIF", userName);
                    PlayerPrefs.Save();
                }
            }
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

        static private string configFileFolder;

        public static string ConfigFilename(string filename="config.json", bool force=false, bool allowSearch=false, string label="Config file")
        {
            if (allowSearch)
            {
                // Special case: filename starting with .../ will be searched in parent folders
                if (filename.StartsWith(".../"))
                {
                    filename = filename.Substring(4);
                }
                else
                {
                    allowSearch = false;
                }
            }
            if (filename == "config.json")
            {
                string clConfigFile = _ConfigFilenameFromCommandLineArgs();
                if (clConfigFile != null)
                {
                    clConfigFile = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), clConfigFile);
                    Debug.Log($"VRTConfig: {label} {filename}: {clConfigFile}");
                    configFileFolder = System.IO.Path.GetDirectoryName(clConfigFile);
                    return clConfigFile;
                }
            }
            if (configFileFolder != null)
            {
                // If we got a config file from the command line, we try that directory first
                // If the force flag is set we use that without considering whether the file exists. Use this for output
                // files or directories.
                string candidate = System.IO.Path.Combine(configFileFolder, filename);
                if (force || System.IO.File.Exists(candidate))
                {
                    Debug.Log($"VRTConfig: {label} {filename}: {candidate}");
                    return candidate;
                }
            }
            string dataPath;
            if (Application.isEditor && Application.platform == RuntimePlatform.WindowsEditor)
            {
                // In the editor the config file is in the same directory as the executable
                dataPath = System.IO.Path.GetDirectoryName(Application.dataPath);
            }
            else if (Application.isEditor)
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

            while (true)
            {
                string rv = System.IO.Path.Combine(dataPath, filename);
                if (!allowSearch || System.IO.File.Exists(rv) || System.IO.Directory.Exists(rv))
                {
                    Debug.Log($"VRTConfig: {label} {filename}: {rv}");
                    return rv;
                }
                string parentDataPath = System.IO.Path.GetDirectoryName(dataPath);
                if (parentDataPath == null || parentDataPath == dataPath)
                {
                    break;
                }

                dataPath = parentDataPath;
            }
            Debug.LogWarning($"VRTConfig: {label} not found: {filename}");
            return filename;
        }
    }
}