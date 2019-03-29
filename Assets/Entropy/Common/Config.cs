using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Config {
    [Serializable]
    public class CTVMs {
        public string connectionURI;
        public string exchangeName;
    };
    public CTVMs TVMs;

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
