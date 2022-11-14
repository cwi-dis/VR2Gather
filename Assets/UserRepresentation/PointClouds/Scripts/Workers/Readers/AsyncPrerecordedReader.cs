using System;
using UnityEngine;

namespace Cwipc
{
    public class AsyncPrerecordedReader : AsyncPrerecordedBaseReader
    {
        TileInfo[] tileInfo;
        [Serializable]
        class _Config
        {
            public TileInfo[] tileInfo;
        }

        public AsyncPrerecordedReader(string _dirname, float _voxelSize, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null)
        : base(_dirname, _voxelSize, _frameRate)
        {
        	newTimestamps = true;
            Add(null, _outQueue, _out2Queue);
            _initTileInfo();
            Start();
        }

        void _initTileInfo()
        {
            var tileConfigFilename = System.IO.Path.Combine(baseDirectory, "tileconfig.json");
            if (!System.IO.File.Exists(tileConfigFilename))
            {
                Debug.LogWarning($"{Name()}: No tileconfig: {tileConfigFilename}");
                return;
            }
            var file = System.IO.File.ReadAllText(tileConfigFilename);
            _Config _config;
            _config = JsonUtility.FromJson<_Config>(file);
            if (_config == null)
            {
                Debug.LogError($"{Name()}: Error reading {tileConfigFilename}");
                return;
            }
            tileInfo = _config.tileInfo;
            Debug.Log($"{Name()}: _initTileInfo: {tileInfo?.Length} tiles");

        }
        public override TileInfo[] getTiles()
        {
            return tileInfo;
        }

        public override void ReportCurrentTimestamp(long curIndex)
        {
            return;
        }
    }
  
}
