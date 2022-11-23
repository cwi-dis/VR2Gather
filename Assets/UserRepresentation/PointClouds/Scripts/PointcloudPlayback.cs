using UnityEngine;
using VRT.Core;
using VRT.Orchestrator.Wrapping;

namespace VRT.UserRepresentation.PointCloud
{
    public class PointCloudPlayback : PointCloudPipeline
    {
        [Tooltip("Overrides PrerecordedReaderConfig setting: directory to read")]
        public string folder;
        [Tooltip("Overrides PrerecordedReaderConfig setting: per-tile subfolders")]
        public string[] tiles;
        [Tooltip("Overrides PrerecordedReaderConfig setting: per-quality subfolders")]
        public string[] qualities;
        [Tooltip("Read .ply files in stead of .cwipcdump files")]
        public bool ply;
        [Tooltip("Prefer best quality in stead of worst quality")]
        public bool preferBest;
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
#if WITH_QUALITY_ASSESMENT
                tiles = realUser.PCSelfConfig.PrerecordedReaderConfig.tiles;
                qualities = realUser.PCSelfConfig.PrerecordedReaderConfig.qualities;
                ply = realUser.PCSelfConfig.PrerecordedReaderConfig.ply;
                preferBest = realUser.PCSelfConfig.PrerecordedReaderConfig.preferBest;
#endif
            }
            //Debug.Log($"{Name()}: folder={folder} ply={ply} {tiles.Length} tiles, {qualities.Length} qualities, preferBest={preferBest}");
            cfg.PCSelfConfig.PrerecordedReaderConfig.folder = folder;
#if WITH_QUALITY_ASSESMENT
            cfg.PCSelfConfig.PrerecordedReaderConfig.tiles = tiles;
            cfg.PCSelfConfig.PrerecordedReaderConfig.qualities = qualities;
            cfg.PCSelfConfig.PrerecordedReaderConfig.ply = ply;
            cfg.PCSelfConfig.PrerecordedReaderConfig.preferBest = preferBest;
#endif
            cfg.PCSelfConfig.frameRate = realUser.PCSelfConfig.frameRate;
            try
            {
                Init(dummyUser, cfg, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{Name()}: initialize: Exception: {e.Message} Stack: {e.StackTrace}");
                throw;
            }
            _InitTileSelector();
            
        }
        protected override void _InitTileSelector()
        {
            if (tileSelector == null)
            {
                Debug.LogWarning($"{Name()}: no tileSelector");
                return;
            }
            if (tiles == null)
            {
                // Untiled: disable tile selector
                Debug.Log($"{Name()}: untiled, disabling {tileSelector.Name()}");
                tileSelector.gameObject.SetActive(false);
                tileSelector = null;
                return;
            }
            int nQualities = qualities.Length;
            int nTiles = tiles.Length;
            Debug.Log($"{Name()}: nTiles={nTiles} nQualities={nQualities}");
            if (nQualities <= 1) return;
            if (tileSelector == null)
            {
                Debug.LogWarning($"{Name()}: no tileSelector");
            }
            var ts = tileSelector as PrerecordedTileSelector;
            ts?.Init(this, nQualities, nTiles, null);
        }
    }
}
