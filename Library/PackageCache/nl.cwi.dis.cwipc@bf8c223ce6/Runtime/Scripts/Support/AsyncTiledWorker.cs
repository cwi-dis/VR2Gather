using System;
using UnityEngine;

namespace Cwipc
{
    /// <summary>
    /// Abstract class for an AsyncWorker that is expected to optionally produce tiled frames.
    /// Only really implemented for tiled pointcloud capturers, readers and receivers.
    /// </summary>
    public abstract class xxxjack_AsyncTiledWorker : AsyncWorker, ITileDescriptionProvider
    {


        public xxxjack_AsyncTiledWorker() : base()
        {
        }
        /// <summary>
        /// Return array of tiles produced  by this reader.
        /// </summary>
        /// <returns></returns>
        virtual public PointCloudTileDescription[] getTiles()
        {
            return null;
        }
    }
}
