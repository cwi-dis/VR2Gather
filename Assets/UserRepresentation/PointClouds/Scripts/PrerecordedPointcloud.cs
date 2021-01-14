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
        public string dirPath;
        User dummyUser;
        Config._User cfg;

        public void Awake()
        {
            Debug.Log($"{Name()}: xxxjack Awake called, dirPath={dirPath}");

            dummyUser = new User();
            dummyUser.userData = new UserData();
            dummyUser.userData.userRepresentationType = UserRepresentationType.__PCC_PRERECORDED__;

            cfg = new Config._User();
            cfg.sourceType = "self";
            cfg.PCSelfConfig = new Config._User._PCSelfConfig();
            cfg.PCSelfConfig.PrerecordedReaderConfig = new Config._User._PCSelfConfig._PrerecordedReaderConfig();
            cfg.PCSelfConfig.PrerecordedReaderConfig.folder = dirPath;

            Init(dummyUser, cfg, true);
            Debug.Log($"{Name()}: xxxjack Init(...) returned");
        }
    }
}
