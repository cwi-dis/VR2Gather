using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Config {
    [Serializable]
    public class _TVMs
    {
        public string connectionURI;
        public string exchangeName;
    };
    public _TVMs TVMs;

    [Serializable]
    public class _PCs
    {
        public string filename;
    };
    public _PCs PCs;

    static Config _Instance;
    public static Config Instance {
        get {
            var path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/config.json";
            if (_Instance == null) {
                var file = System.IO.File.ReadAllText(path);
                _Instance = JsonUtility.FromJson<Config>(file);
            }
            return _Instance;
        }
    }
}
