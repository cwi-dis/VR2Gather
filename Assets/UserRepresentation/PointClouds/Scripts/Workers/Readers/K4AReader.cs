using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTCore;

namespace Workers
{
    public class K4AReader : TiledWorker {
        cwipc.source reader;
        float voxelSize;
        System.TimeSpan frameInterval;  // Interval between frame grabs, if maximum framerate specified
        System.DateTime earliestNextCapture;    // Earliest time we want to do the next capture, if non-null.
        QueueThreadSafe outQueue;
        QueueThreadSafe out2Queue;

        K4AReader(QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(WorkerType.Init)
        {
            if (_outQueue == null)
            {
                throw new System.Exception("{Name()}: outQueue is null");
            }
            outQueue = _outQueue;
            out2Queue = _out2Queue;
        }

        public K4AReader(string _configFilename, float _voxelSize, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : this(_outQueue, _out2Queue)
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
                throw e;
            }
        }

        public override TileInfo[] getTiles()
        {
            cwipc.tileinfo[] origTileInfo = reader.get_tileinfo();
            if (origTileInfo == null || origTileInfo.Length <= 1) return null;
            int nTile = origTileInfo.Length;
            TileInfo[] rv = new TileInfo[nTile];
            for (int i=0; i<nTile; i++)
            {
                rv[i].normal = new Vector3((float)origTileInfo[i].normal.x, (float)origTileInfo[i].normal.y, (float)origTileInfo[i].normal.z);
                rv[i].cameraName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(origTileInfo[i].camera);
                rv[i].cameraMask = origTileInfo[i].ncamera;
            }
            return rv;
        }

        public override void Stop()
        {
            base.Stop();
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            if (out2Queue != null && !out2Queue.IsClosed()) out2Queue.Close();
        }

        public override void OnStop() {
            base.OnStop();
            reader?.free();
            reader = null;
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            if (out2Queue != null && !out2Queue.IsClosed()) out2Queue.Close();
            Debug.Log($"{Name()}: Stopped.");
        }

        protected override void Update() {
            base.Update();
            //
            // Limit framerate, if required
            //
            if (earliestNextCapture != null)
            {
                System.TimeSpan sleepDuration = earliestNextCapture - System.DateTime.Now;
                if (sleepDuration > System.TimeSpan.FromSeconds(0))
                {
                    System.Threading.Thread.Sleep(sleepDuration);
                }
            }
            if (frameInterval != null)
            {
                earliestNextCapture = System.DateTime.Now + frameInterval;
            }
            cwipc.pointcloud pc = reader.get();
            if (pc == null) return;
            if (voxelSize != 0) {
                var newPc = cwipc.downsample(pc, voxelSize);
                if (newPc == null)
                {
                    Debug.LogWarning($"{Name()}: Voxelating pointcloud with {voxelSize} got rid of all points?");
                } else
                {
                    pc.free();
                    pc = newPc;
                }
            }

            bool didDrop = false;
            if (outQueue == null)
            {
                Debug.LogError($"Programmer error: {Name()}: no outQueue, dropping pointcloud");
                didDrop = true;
            }
            else
            {
                bool ok = outQueue.Enqueue(pc.AddRef());
                if (!ok)
                {
                    didDrop = true;
                }
            }
            if (out2Queue == null)
            {
                // This is not an error. Debug.LogError($"{Name()}: no outQueue2, dropping pointcloud");
            }
            else
            {
                bool ok = out2Queue.Enqueue(pc.AddRef());
                if (!ok)
                {
                    didDrop = true;
                }
            }
            statsUpdate(pc.count(), didDrop);
            pc.free();
        }

        System.DateTime statsLastTime;
        double statsTotalPoints;
        double statsTotalPointclouds;
        double statsDrops;
        const int statsInterval = 10;

        public void statsUpdate(int pointCount, bool dropped=false)
        {
            if (statsLastTime == null)
            {
                statsLastTime = System.DateTime.Now;
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
                statsDrops = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(statsInterval))
            {
                Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={Name()}, fps={statsTotalPointclouds / statsInterval}, points_per_cloud={(int)(statsTotalPoints / (statsTotalPointclouds==0?1: statsTotalPointclouds))}, drops_per_second={statsDrops / statsInterval}");
                if (statsDrops > 3*statsInterval)
                {
                    Debug.LogWarning($"{Name()}: excessive dropped frames. Lower LocalUser.PCSelfConfig.frameRate in config.json.");
                }
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
                statsDrops = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalPoints += pointCount;
            statsTotalPointclouds += 1;
            if (dropped) statsDrops++;
        }
    }
}
