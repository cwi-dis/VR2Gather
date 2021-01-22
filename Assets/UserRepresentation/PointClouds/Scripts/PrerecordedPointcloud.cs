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
        [Tooltip("Overrides PrerecordedReaderConfig setting: directory to read")]
        public string folder;
        [Tooltip("Overrides PrerecordedReaderConfig setting: per-tile subfolders")]
        public string[] tiles;
        [Tooltip("Overrides PrerecordedReaderConfig setting: per-quality subfolders")]
        public string[] qualities;
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
            if (folder == null || folder == "")
            {
                folder = realUser.PCSelfConfig.PrerecordedReaderConfig.folder;
                tiles = realUser.PCSelfConfig.PrerecordedReaderConfig.tiles;
                qualities = realUser.PCSelfConfig.PrerecordedReaderConfig.qualities;
                ply = realUser.PCSelfConfig.PrerecordedReaderConfig.ply;
            }
            Debug.Log($"{Name()}: folder={folder} ply={ply} {tiles.Length} tiles, {qualities.Length} qualities");
            cfg.PCSelfConfig.PrerecordedReaderConfig.folder = folder;
            cfg.PCSelfConfig.PrerecordedReaderConfig.tiles = tiles;
            cfg.PCSelfConfig.PrerecordedReaderConfig.qualities = qualities;
            cfg.PCSelfConfig.PrerecordedReaderConfig.ply = ply;
            cfg.Render = realUser.Render;
            // xxxjack debug
            xxxjack_nQualities = cfg.PCSelfConfig.PrerecordedReaderConfig.qualities.Length;
            xxxjack_nTiles = cfg.PCSelfConfig.PrerecordedReaderConfig.tiles.Length;
            xxxjack_selectedQualities = new int[xxxjack_nTiles];
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

        // xxxjack debug code
        int xxxjack_nQualities;
        int xxxjack_nTiles;
        int[] xxxjack_selectedQualities;
        int xxxjack_tileToSwitch;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                for (int i = 0; i < xxxjack_nTiles; i++) xxxjack_selectedQualities[i] = 0;
                SelectTileQualities(xxxjack_selectedQualities);
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                for (int i = 0; i < xxxjack_nTiles; i++) xxxjack_selectedQualities[i] = xxxjack_nQualities - 1;
                SelectTileQualities(xxxjack_selectedQualities);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                for (int i = 0; i < xxxjack_nTiles; i++) xxxjack_selectedQualities[i] = 0;
                xxxjack_selectedQualities[0] = xxxjack_nQualities - 1;
                SelectTileQualities(xxxjack_selectedQualities);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                for (int i = 0; i < xxxjack_nTiles; i++) xxxjack_selectedQualities[i] = 0;
                xxxjack_selectedQualities[1] = xxxjack_nQualities - 1;
                SelectTileQualities(xxxjack_selectedQualities);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                for (int i = 0; i < xxxjack_nTiles; i++) xxxjack_selectedQualities[i] = 0;
                xxxjack_selectedQualities[2] = xxxjack_nQualities - 1;
                SelectTileQualities(xxxjack_selectedQualities);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                for (int i = 0; i < xxxjack_nTiles; i++) xxxjack_selectedQualities[i] = 0;
                xxxjack_selectedQualities[3] = xxxjack_nQualities - 1;
                SelectTileQualities(xxxjack_selectedQualities);
            }

        }
    }
}
