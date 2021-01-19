using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.UserRepresentation.Voice;
using VRTCore;
using VRT.Core;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;

namespace VRT.UserRepresentation.PointCloud
{
    public class PrerecordedPointcloud : PointCloudPipeline
    {
        [Tooltip("Overrides PrerecordedReaderConfig setting: directories to read")]
        public string[] folders;
        [Tooltip("Read .ply files in stead of .cwipcdump files")]
        public bool ply;
        User dummyUser;
        Config._User cfg;

        public void Awake()
        {

            dummyUser = new User();
            dummyUser.userData = new UserData();
            dummyUser.userData.userRepresentationType = UserRepresentationType.__PCC_PRERECORDED__;

            Config._User realUser = Config.Instance.LocalUser;

            cfg = new Config._User();
            cfg.sourceType = "prerecorded";
            cfg.PCSelfConfig = new Config._User._PCSelfConfig();
            cfg.PCSelfConfig.PrerecordedReaderConfig = new Config._User._PCSelfConfig._PrerecordedReaderConfig();
            if (folders == null || folders.Length == 0)
            {
                folders = realUser.PCSelfConfig.PrerecordedReaderConfig.folders;
                ply = realUser.PCSelfConfig.PrerecordedReaderConfig.ply;
                Debug.Log($"xxxjack folders={folders} ply={ply}");
            }
            Debug.Log($"{Name()}: folder={folders}");
            cfg.PCSelfConfig.PrerecordedReaderConfig.folders = folders;
            cfg.PCSelfConfig.PrerecordedReaderConfig.ply = ply;
            cfg.Render = realUser.Render;

            try
            {
                Init(dummyUser, cfg, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Cannot initialize prerecorded pointcloud: Exception: {e.Message} Stack: {e.StackTrace}");
                throw e;
            }
        }
    }
}
