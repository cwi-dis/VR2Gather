using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class PCReader : TiledWorker
    {
        protected cwipc.source reader;
        protected float voxelSize;
        protected System.TimeSpan frameInterval;  // Interval between frame grabs, if maximum framerate specified
        protected System.DateTime earliestNextCapture;    // Earliest time we want to do the next capture, if non-null.
        protected QueueThreadSafe outQueue;
        protected QueueThreadSafe out2Queue;
        protected bool dontWait = false;

        protected PCReader(QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(WorkerType.Init)
        {
            if (_outQueue == null)
            {
                throw new System.Exception("{Name()}: outQueue is null");
            }
            outQueue = _outQueue;
            out2Queue = _out2Queue;
            stats = new Stats(Name());
        }

        public PCReader(float _frameRate, int nPoints, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : this(_outQueue, _out2Queue)
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
                throw e;
            }
        }

        public override TileInfo[] getTiles()
        {
            cwipc.tileinfo[] origTileInfo = reader.get_tileinfo();
            if (origTileInfo == null || origTileInfo.Length <= 1) return null;
            int nTile = origTileInfo.Length;
            TileInfo[] rv = new TileInfo[nTile];
            for (int i = 0; i < nTile; i++)
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

        public override void OnStop()
        {
            base.OnStop();
            reader?.free();
            reader = null;
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            if (out2Queue != null && !out2Queue.IsClosed()) out2Queue.Close();
            Debug.Log($"{Name()}: Stopped.");
        }

        protected override void Update()
        {
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
            if (dontWait) {
            	if (!reader.available(false)) return;
            }
            cwipc.pointcloud pc = reader.get();
            if (pc == null) return;
            if (voxelSize != 0)
            {
                var newPc = cwipc.downsample(pc, voxelSize);
                if (newPc == null)
                {
                    Debug.LogWarning($"{Name()}: Voxelating pointcloud with {voxelSize} got rid of all points?");
                }
                else
                {
                    pc.free();
                    pc = newPc;
                }
            }

            bool didDrop = false;
            bool didDropSelf = false;
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
                    didDropSelf = true;
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
            stats.statsUpdate(pc.count(), didDrop, didDropSelf);
            pc.free();
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalPoints = 0;
            double statsTotalPointclouds = 0;
            double statsDrops = 0;
            double statsSelfDrops = 0;

            public void statsUpdate(int pointCount, bool dropped, bool droppedSelf)
            {
                
                statsTotalPoints += pointCount;
                statsTotalPointclouds += 1;
                if (dropped) statsDrops++;
                if (droppedSelf) statsSelfDrops++;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalPointclouds / Interval():F2}, points_per_cloud={(int)(statsTotalPoints / (statsTotalPointclouds == 0 ? 1 : statsTotalPointclouds))}, drop_fps={statsDrops / Interval():F2}, selfdrop_fps={statsSelfDrops / Interval():F2}");
                    if (statsDrops > 3 * Interval())
                    {
                        Debug.LogWarning($"{name}: excessive dropped frames. Lower LocalUser.PCSelfConfig.frameRate in config.json.");
                    }
                 }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPoints = 0;
                    statsTotalPointclouds = 0;
                    statsDrops = 0;
                    statsSelfDrops = 0;
                }
            }
        }

        protected Stats stats;
    }
}
