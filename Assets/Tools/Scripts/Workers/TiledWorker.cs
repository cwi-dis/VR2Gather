using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class TiledWorker : BaseWorker {
        public class TileInfo
        {
            int dummy;
        }

        public TiledWorker(WorkerType _type= WorkerType.Run) : base(_type) {
        }

        public TileInfo[] getTiles()
        {
            return null;
        }
    }
}
