using System;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class PrerecordedLiveReader : PrerecordedBaseReader
    {
        TileInfo[] tileInfo;
        [Serializable]
        class _Config
        {
            public TileInfo[] tileInfo;
        }

        public PrerecordedLiveReader(string _dirname, float _voxelSize, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null)
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
                Debug.Log($"{Name()}: xxxjack no tileconfig: {tileConfigFilename}");
                return;
            }
            var file = System.IO.File.ReadAllText(tileConfigFilename);
            _Config _config;
            _config = JsonUtility.FromJson<_Config>(file);
            tileInfo = _config.tileInfo;
            Debug.Log($"{Name()}: _initTileInfo: {tileInfo?.Length} tiles");

        }
        public override TileInfo[] getTiles()
        {
            Debug.Log($"{Name()}: xxxjack getTiles returning {tileInfo?.Length} tiles");
            return tileInfo;
#if notdefined
        cwipc.tileinfo[] origTileInfo = reader.get_tileinfo();
        if (origTileInfo == null || origTileInfo.Length <= 1) return null;
        int nTile = origTileInfo.Length;
        TileInfo[] rv = new TileInfo[nTile];
        for (int i = 0; i < nTile; i++)
        {
            rv[i].normal = new Vector3((float)origTileInfo[i].normal.x, (float)origTileInfo[i].normal.y, (float)origTileInfo[i].normal.z);
            rv[i].cameraName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(origTileInfo[i].camera);
            rv[i].cameraMask = origTileInfo[i].ncamera;
        }
        return rv;
#endif
        }
    }
  
}
