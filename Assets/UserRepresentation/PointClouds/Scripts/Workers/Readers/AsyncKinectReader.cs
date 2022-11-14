using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    public class AsyncKinectReader : AsyncPointcloudReader
    {

        public AsyncKinectReader(string _configFilename, float _voxelSize, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(_outQueue, _out2Queue)
        {
            voxelSize = _voxelSize;
            if (_frameRate > 0)
            {
                frameInterval = System.TimeSpan.FromSeconds(1 / _frameRate);
            }
            try
            {
                reader = cwipc.kinect(_configFilename);
                if (reader != null)
                {
                    Start();
                    Debug.Log("{Name()}: Started.");
                }
                else
                    throw new System.Exception($"{Name()}: cwipc_kinect could not be created"); // Should not happen, should throw exception
            }
            catch (System.DllNotFoundException e)
            {
                throw new System.Exception($"{Name()}: support for Kinect grabber not installed on this computer. Missing DLL {e.Message}.");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: caught System.exception {e.Message}");
                throw;
            }
        }
    }
}
