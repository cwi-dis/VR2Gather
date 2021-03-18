﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class RS2Reader : PCReader
    {

        public RS2Reader(string _configFilename, float _voxelSize, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(_outQueue, _out2Queue)
        {
            voxelSize = _voxelSize;
            if (_frameRate > 0)
            {
                frameInterval = System.TimeSpan.FromSeconds(1 / _frameRate);
            }
            try
            {
                reader = cwipc.realsense2(_configFilename);
                if (reader != null)
                {
                    Start();
                    Debug.Log("{Name()}: Started.");
                }
                else
                    throw new System.Exception($"{Name()}: cwipc_realsense2 could not be created"); // Should not happen, should throw exception
            }
            catch (System.DllNotFoundException e)
            {
                throw new System.Exception($"{Name()}: support for Realsense2 grabber not installed on this computer. Missing DLL {e.Message}.");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: caught System.exception {e.Message}");
                throw e;
            }
        }
	}
}
