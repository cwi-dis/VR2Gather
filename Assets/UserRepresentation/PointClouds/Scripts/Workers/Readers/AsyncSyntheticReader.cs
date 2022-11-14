using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    public class AsyncSyntheticReader : AsyncPointcloudReader
    {

        public AsyncSyntheticReader(float _frameRate, int nPoints, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(_outQueue, _out2Queue)
        {
            voxelSize = 0;
            if (_frameRate > 0)
            {
                frameInterval = System.TimeSpan.FromSeconds(1 / _frameRate);
            }
            try
            {
                reader = cwipc.synthetic((int)_frameRate, nPoints);
                if (reader != null)
                {
                    Start();
                    Debug.Log("{Name()}: Started.");
                }
                else
                    throw new System.Exception($"{Name()}: cwipc_synthetic could not be created"); // Should not happen, should throw exception
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: Exception: {e.Message}");
                throw;
            }
        }
    }
}