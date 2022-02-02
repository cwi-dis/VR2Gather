using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Core
{
    [Serializable]
    public class Config
    {
        [Flags] public enum ProtocolType
        {
            None = 0,
            SocketIO = 1,
            Dash = 2,
            TCP = 3,
        };

        public enum UserRepresentation
        {
            TVM,
            PC
        };

        public enum Presenter
        {
            None,
            Local,
            Live
        }

        public string orchestratorURL = "";
        public string orchestratorLogURL = "";
        public bool openLogOnExit = true;
        public int targetFrameRate = 90;
        public float memoryDamping = 1.3f;
        public float ntpSyncThreshold = 1.0f;
        public ProtocolType protocolType = ProtocolType.SocketIO;
        public readonly int audioSampleRate = 48000;
        public UserRepresentation userRepresentation = UserRepresentation.PC;
        public Presenter presenter = Presenter.None;
        public bool pilot3NavigationLogs = true;
        public double statsInterval = 10.0;
        public string statsOutputFile = "";
        public bool allowControllerMovement = true;
        public bool statsOutputFileAppend = true;
        public string ffmpegDLLDir = "";

        [Serializable]
        public class _VR
        {
            public string[] preferredDevices = { "Oculus", "OpenVR", "" };
            public string preferredController = "";
            public bool useLookingGlass = false;
        }
        public _VR VR;

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
            public bool autoLogin = false;
            public string sessionName = "";
            public int sessionScenario = -1;
            public int sessionTransportProtocol = -1;
            public bool sessionUncompressed = false;
            public bool sessionUncompressedAudio = false;
            public bool autoCreate = false;
            public bool autoJoin = true;
            public string autoCreateForUser = "";
            public int autoStartWith = -1;
            public float autoDelay = 0.2f;
        };
        public _AutoStart AutoStart;

        [Serializable]
        public class _TVMs
        {
            public string connectionURI;
            public string exchangeName;
            public bool printMetrics;
            public bool saveMetrics;
        };
        public _TVMs TVMs;

        [Serializable]
        public class _Macintosh
        {
            public string SIGNALS_SMD_PATH;
        };
        public _Macintosh Macintosh;

        [Serializable]
        public class _PCs
        {
            public string Codec = "cwi1";
            public float defaultCellSize;
            public float cellSizeFactor;
            public bool debugColorize;
        };
        public _PCs PCs;

        [Serializable]
        public class _Voice
        {
            public string Codec = "VR2A";
            public int audioFps = 50;
            public float maxPlayoutLatency = 0.3f;
            public float maxPlayoutAhead = 0.066f;
            public bool ignoreSynchronizer = false;
        }
        public _Voice Voice;

        [Serializable]
        public class _Video
        {
            public string Codec = "h264";
        }
        public _Video Video;

        [Serializable]
        public class _User
        {
            public string sourceType;
            [Serializable]
            public class _PCSUBConfig
            {
            }
            public _PCSUBConfig PCSUBConfig;
            [Serializable]
            public class _AudioSUBConfig
            {
                public int streamNumber;
            }
            public _AudioSUBConfig AudioSUBConfig;

            [Serializable]
            public class _PCSelfConfig
            {
                [Serializable]
                public class _RS2ReaderConfig
                {
                    public string configFilename;
                }
                public _RS2ReaderConfig RS2ReaderConfig;
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
				[Serializable]
				public class _CerthReaderConfig
                {
                    public string ConnectionURI;
                    public string PCLExchangeName;
                    public string MetaExchangeName;
                    public Vector3 OriginCorrection;
                    public Vector3 BoundingBotLeft;
                    public Vector3 BoundingTopRight;
                }
                public _CerthReaderConfig CerthReaderConfig;
                public float voxelSize;
                public float frameRate;
                public bool tiled;
                [Serializable]
                public class _Encoder
                {
                    public int octreeBits;
                }
                public _Encoder[] Encoders;
                [Serializable]
                public class _Bin2Dash
                {
                    public int segmentSize;
                    public int segmentLife;
                }
                public _Bin2Dash Bin2Dash;
                public _Bin2Dash AudioBin2Dash;
             }
            public _PCSelfConfig PCSelfConfig;

        };
        public _User LocalUser;
        public _User RemoteUser;

        static Config _Instance;
        public static Config Instance
        {
            get
            {
                if (_Instance == null)
                {
                    string file = ConfigFilename();
                    _Instance = JsonUtility.FromJson<Config>(System.IO.File.ReadAllText(file));
                    Application.targetFrameRate = _Instance.targetFrameRate;
                }
                return _Instance;
            }
        }

        public void WriteConfig(object toJson)
        {
            string file = ConfigFilename();
            System.IO.File.WriteAllText(file, JsonUtility.ToJson(toJson, true));

            //System.IO.File.WriteAllText(Application.streamingAssetsPath + "/ipScalable.json", JsonHelper.ToJson(playerConfig, true));
        }

        public static string ConfigFilename(string filename="config.json")
        {
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