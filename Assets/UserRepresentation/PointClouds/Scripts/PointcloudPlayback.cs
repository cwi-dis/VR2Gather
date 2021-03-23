using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.UserRepresentation.Voice;
using VRT.Core;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;

namespace VRT.UserRepresentation.PointCloud
{
    public class PointcloudPlayback : PointCloudPipeline
    {
        [Tooltip("Overrides PrerecordedReaderConfig setting: directory to read")]
        public string folder;
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
            if (folder == null || folder == "")
            {
                folder = realUser.PCSelfConfig.PrerecordedReaderConfig.folder;
           }
            Debug.Log($"{Name()}: folder={folder}");
            cfg.PCSelfConfig.PrerecordedReaderConfig.folder = folder;
            cfg.PCSelfConfig.frameRate = realUser.PCSelfConfig.frameRate;
            cfg.Render = realUser.Render;
            try
            {
                Init(dummyUser, cfg, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{Name()}: initialize: Exception: {e.Message} Stack: {e.StackTrace}");
                throw e;
            }
            _InitTileSelector();
        }
        protected override void _InitTileSelector()
        {
            PrerecordedPlaybackReader playbackReader = (PrerecordedPlaybackReader)reader;
            if (playbackReader == null)
            {
                throw new System.Exception($"{Name()}: reader is not PrerecordedPlaybackReader");
            }
            PrerecordedTileSelector ts = (PrerecordedTileSelector)tileSelector;
            if (ts == null)
            {
                throw new System.Exception($"{Name()}: tileSelector is not PrerecordedTileSelector");
            }
            ts?.Init(this, playbackReader.GetStaticPredictionInformation());
        }
    }
}
