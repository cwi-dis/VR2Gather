using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Core
{
    [Serializable]
    public class Config
    {
        public enum ProtocolType
        {
            None,
            Dash,
            SocketIO,
            TCP
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
        public string videoCodec = "h264";
        public UserRepresentation userRepresentation = UserRepresentation.PC;
        public Presenter presenter = Presenter.None;
        public float nonHMDHeight = 1.8f;
        public bool pilot3NavigationLogs = true;
        public double statsInterval = 10.0;
        public string statsOutputFile = "";
        public bool statsOutputFileAppend = true;

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
            public Vector3 scale;
            public bool forceMesh;
            public float defaultCellSize;
            public float cellSizeFactor;
        };
        public _PCs PCs;

        [Serializable]
        public class _User
        {
            public string sourceType;
            [Serializable]
            public class _PCSUBConfig
            {
                public int[] tileNumbers;
                public int initialDelay;
            }
            public _PCSUBConfig SUBConfig;
            [Serializable]
            public class _AudioSUBConfig
            {
                public int streamNumber;
                public int initialDelay;
            }
            public _AudioSUBConfig AudioSUBConfig;

            [Serializable]
            public class _PCSelfConfig
            {
                [Serializable]
                public class _RS2ReaderConfig
                {
                    public string configFilename;
                    public bool wantedSkeleton = true;
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

            [Serializable]
            public class _Render
            {
                public float pointSize = 0.008f;
                public Vector3 position;
                public Vector3 rotation;
                public Vector3 scale = Vector3.one;
            }
            public _Render Render;
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
                    var file = System.IO.File.ReadAllText(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/config.json");
                    _Instance = JsonUtility.FromJson<Config>(file);
                    Application.targetFrameRate = _Instance.targetFrameRate;
                }
                return _Instance;
            }
        }

        public void WriteConfig(object toJson)
        {
            var path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/config.json";
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(toJson, true));

            //System.IO.File.WriteAllText(Application.streamingAssetsPath + "/ipScalable.json", JsonHelper.ToJson(playerConfig, true));
        }
    }
}