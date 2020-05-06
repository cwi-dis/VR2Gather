using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Config {
    public float memoryDamping = 1.3f;
    public bool useAudio;
    [Serializable]
    public class _TVMs
    {
        public string   connectionURI;
        public string   exchangeName;
        public Vector3  offsetPosition;
        public Vector3  offsetRotation;
    };
    public _TVMs TVMs;

    [Serializable]
    public class _Macintosh
    {
        public string SIGNALS_SMD_PATH;
    };
    public _Macintosh Macintosh;

    [Serializable]
    public class _PCs {
        public Vector3 offsetPosition;
        public Vector3 offsetRotation;
        public Vector3 scale;
    };
    public _PCs PCs;

    [Serializable]
    public class _User {
        public string sourceType;
        public string cwicpcFilename;
        public string cwicpcDirectory;
        public string plyFilename;
        public string plyDirectory;
        [Serializable]
        public class _SUBConfig {
            public string url;
            public string streamName;
            public int streamNumber;
            public int initialDelay;
        }
        public _SUBConfig SUBConfig;
        public _SUBConfig AudioSUBConfig;

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
            public class _CerthReaderConfig
            {
                public string ConnectionURI;
                public string PCLExchangeName;
                public string MetaExchangeName;
            }
            public _CerthReaderConfig CerthReaderConfig;
            public float voxelSize;
            [Serializable]
            public class _Encoder {
                public int octreeBits;
            }
            public _Encoder[] Encoders;
            [Serializable]
            public class _Bin2Dash {
                public string url;
                public string streamName;
                public int segmentSize;
                public int segmentLife;
                public bool fileMirroring;
            }
            public _Bin2Dash Bin2Dash;
            public _Bin2Dash AudioBin2Dash;
        }
        public _PCSelfConfig PCSelfConfig;

        [Serializable]
        public class _NetConfig
        {
            public string hostName;
            public int port;
        }
        public _NetConfig NetConfig;


        [Serializable]
        public class _Render
        {
            public bool forceMesh;
            public float pointSize = 0.008f;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale = Vector3.one;
        }
        public _Render Render;
    };
    public _User[] Users;

    static Config _Instance;
    public static Config Instance {
        get {            
            if (_Instance == null) {
                var file = System.IO.File.ReadAllText(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/config.json");
                _Instance = JsonUtility.FromJson<Config>(file);
            }
            return _Instance;
        }
    }

    public void WriteConfig(object toJson) {
        var path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/config.json"; 
        System.IO.File.WriteAllText(path, JsonUtility.ToJson(toJson, true));

        //System.IO.File.WriteAllText(Application.streamingAssetsPath + "/ipScalable.json", JsonHelper.ToJson(playerConfig, true));
    }
}
