using System;
using UnityEngine;

namespace VRT.Core
{
    public class TiledWorker : BaseWorker
    {
        [Serializable]
        public struct TileInfo
        {
            // xxxjack wrong: should be min and max angle in XZ plane
            public Vector3 normal;
            public string cameraName;
            public int cameraMask;
        }

        public TiledWorker(WorkerType _type = WorkerType.Run) : base(_type)
        {
        }

        virtual public TileInfo[] getTiles()
        {
            return null;
        }
    }
}
