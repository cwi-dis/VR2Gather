using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTCore
{
    public class TiledWorker : BaseWorker
    {
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
