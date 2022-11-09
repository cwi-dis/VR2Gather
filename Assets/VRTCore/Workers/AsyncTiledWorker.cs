using System;
using UnityEngine;

namespace VRT.Core
{
    public abstract class AsyncTiledWorker : AsyncWorker
    {
        [Serializable]
        public struct TileInfo
        {
            // xxxjack wrong: should be min and max angle in XZ plane
            public Vector3 normal;
            public string cameraName;
            public int cameraMask;
        }

        public AsyncTiledWorker() : base()
        {
        }

        virtual public TileInfo[] getTiles()
        {
            return null;
        }
    }
}
