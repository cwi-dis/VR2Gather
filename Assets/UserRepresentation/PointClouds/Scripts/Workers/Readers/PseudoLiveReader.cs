using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class PseudoLiveReader : PrerecordedReader
    {

        public PseudoLiveReader(string _dirname, bool _ply, float _voxelSize, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null)
        : base(null)
        {
        	newTimestamps = true;
			Add(_dirname, _ply, true, _voxelSize, _frameRate, _outQueue, _out2Queue);
            Start();
        }
	}
}
