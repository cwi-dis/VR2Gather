using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConfigManager {
    
    public enum PlayerMode {
        Video360Mono,
        Video360Stereo,
        TVScreen,
        ParalaxTVScreen,
        Billboards,
        MeshModels
    };

    [System.Serializable]
    public class Config {
        public bool is_player_1 = false;
        public string player2_ip = "127.0.0.1";
        public int port = 55556;
        public int playerMode = 4;

        public bool single_player = false;
        public string microphone_device = "";
        public bool use_TVMs = true;

        public string local_tvm_position = string.Empty;
        public string local_tvm_rotation = string.Empty;
        public string local_tvm_scale = string.Empty;
        public string local_tvm_address = string.Empty; // amqp://tofis:tofis@195.521.117.145:5672
        public string local_tvm_exchange_name = string.Empty; // player1

        public string remote_tvm_position = string.Empty;
        public string remote_tvm_rotation = string.Empty;
        public string remote_tvm_scale = string.Empty;
        public string remote_tvm_address = string.Empty; // amqp://tofis:tofis@195.521.117.145:5672
        public string remote_tvm_exchange_name = string.Empty; // player2

        public string local_pointcloud_url = string.Empty;
        public string local_pointcloud_position = string.Empty;
        public string local_pointcloud_rotation = string.Empty;
        public string local_pointcloud_scale = string.Empty;
        public string local_pointcloud_size = string.Empty;

        public string remote_pointcloud_url = string.Empty;
        public string remote_pointcloud_position = string.Empty;
        public string remote_pointcloud_rotation = string.Empty;
        public string remote_pointcloud_scale = string.Empty;
        public string remote_pointcloud_size = string.Empty;
        public string camera_height = string.Empty;
        //        public string camera_rotation = string.Empty;
    }

    [System.Serializable]
    public class PlayerConfig {
        public int player_id = 1;
        public string player_ip = string.Empty;
        public string tvm_position = string.Empty;
        public string tvm_rotation = string.Empty;
        public string tvm_scale = string.Empty;
        public string tvm_address = string.Empty;
        public string tvm_exchange_name = string.Empty;
    }

    public static Config config;

    public static PlayerConfig[] playerConfig = new PlayerConfig[4];

    public static void ReadConfig() {
        config = JsonUtility.FromJson<Config>(System.IO.File.ReadAllText(Application.streamingAssetsPath + "/ip.json"));

        //playerConfig = JsonHelper.FromJson<PlayerConfig>(System.IO.File.ReadAllText(Application.streamingAssetsPath + "/ipScalable.json"));
    }

    public static void WriteConfig() {
        System.IO.File.WriteAllText(Application.streamingAssetsPath + "/ip.json", JsonUtility.ToJson(config, true));

        //System.IO.File.WriteAllText(Application.streamingAssetsPath + "/ipScalable.json", JsonHelper.ToJson(playerConfig, true));
    }

}
