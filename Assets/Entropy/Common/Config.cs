using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Config {
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
    public class _PCs
    {
        public bool     forceMesh;
        public string   sourceType;
        public string   cwicpcFilename;
        public string   cwicpcDirectory;
        public string   plyFilename;
        public string   plyDirectory;
        public string   networkHost;
        public int      networkPort;
        public string   subURL;
        public string   realsense2ConfigFile;
        public string   realsense2EncodedName;
        public string   realsense2EncodedURL;
        public int      subStreamNumber;

        public float    pointSize = 0.008f;
        public Vector3  position;
        public Vector3  rotation;
        public Vector3  scale = Vector3.one;
        public Vector3  offsetPosition;
        public Vector3  offsetRotation;
    };
    public _PCs[] PCs;

    static Config _Instance;
    public static Config Instance {
        get {
            //var path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/config.json";
            var path = Application.streamingAssetsPath + "/config.json";
            if (_Instance == null) {
                var file = System.IO.File.ReadAllText(path);
                _Instance = JsonUtility.FromJson<Config>(file);
            }
            return _Instance;
        }
    }

    public void WriteConfig(object toJson) {
        //var path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/config.json";
        var path = Application.streamingAssetsPath + "/config.json";
        System.IO.File.WriteAllText(path, JsonUtility.ToJson(toJson, true));

        //System.IO.File.WriteAllText(Application.streamingAssetsPath + "/ipScalable.json", JsonHelper.ToJson(playerConfig, true));
    }
}
