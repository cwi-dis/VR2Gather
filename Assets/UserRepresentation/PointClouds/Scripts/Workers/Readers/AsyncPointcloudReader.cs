using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public abstract class AsyncPointcloudReader : AsyncTiledWorker
    {
        protected cwipc.source reader;
        protected float voxelSize;
        protected System.TimeSpan frameInterval;  // Interval between frame grabs, if maximum framerate specified
        protected System.DateTime earliestNextCapture;    // Earliest time we want to do the next capture, if non-null.
        protected QueueThreadSafe outQueue;
        protected QueueThreadSafe out2Queue;
        protected bool dontWait = false;
        protected float[] bbox;

        protected AsyncPointcloudReader(QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base()
        {
            if (_outQueue == null)
            {
                throw new System.Exception("{Name()}: outQueue is null");
            }
            outQueue = _outQueue;
            out2Queue = _out2Queue;
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
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
                rv[i].cameraName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(origTileInfo[i].cameraName);
                rv[i].cameraMask = origTileInfo[i].cameraMask;
            }
            return rv;
        }

        public void SetCrop(float[] _bbox)
        {
            bbox = _bbox;
        }

        public void ClearCrop()
        {
            bbox = null;
        }

        public override void Stop()
        {
            base.Stop();
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            if (out2Queue != null && !out2Queue.IsClosed()) out2Queue.Close();
        }

        public override void AsyncOnStop()
        {
            base.AsyncOnStop();
            reader?.free();
            reader = null;
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            if (out2Queue != null && !out2Queue.IsClosed()) out2Queue.Close();
            Debug.Log($"{Name()}: Stopped.");
        }

        protected override void AsyncUpdate()
        {
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
            Timedelta downsampleDuration = 0;
            if (voxelSize != 0)
            {
                System.DateTime downsampleStartTime = System.DateTime.Now;
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
                System.DateTime downsampleStopTime = System.DateTime.Now;
                downsampleDuration = (Timedelta)(downsampleStopTime - downsampleStartTime).TotalMilliseconds;

            }

            if (bbox != null)
            {
                cwipc.pointcloud newPc = cwipc.crop(pc, bbox);
                pc.free();
                pc = newPc;
            }

#pragma warning disable CS0219 // Variable is assigned but its value is never used
            bool didDrop = false;
            bool didDropSelf = false;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
            Timedelta encoderQueuedDuration = 0;
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
                encoderQueuedDuration = out2Queue.QueuedDuration();
                bool ok = out2Queue.Enqueue(pc.AddRef());
                if (!ok)
                {
                    didDrop = true;
                }
            }
#if VRT_WITH_STATS
            stats.statsUpdate(pc.count(), pc.cellsize(), downsampleDuration, didDrop, didDropSelf, encoderQueuedDuration, pc.timestamp());
#endif
            pc.free();
        }

#if VRT_WITH_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalPoints = 0;
            double statsTotalPointclouds = 0;
            double statsTotalPointSize = 0;
            double statsDrops = 0;
            double statsSelfDrops = 0;
            double statsQueuedDuration = 0;
            double statsDownsampleDuration = 0;
            int statsAggregatePackets = 0;

            public void statsUpdate(int pointCount, float pointSize, Timedelta downsampleDuration, bool dropped, bool droppedSelf, Timedelta queuedDuration, Timestamp timestamp)
            {
                
                statsTotalPoints += pointCount;
                statsTotalPointSize += pointSize;
                statsTotalPointclouds++;
                statsAggregatePackets++;
                if (dropped) statsDrops++;
                if (droppedSelf) statsSelfDrops++;
                statsQueuedDuration += queuedDuration;
                statsDownsampleDuration += downsampleDuration;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalPointclouds / Interval():F2}, points_per_cloud={(int)(statsTotalPoints / statsTotalPointclouds)}, avg_pointsize={(statsTotalPointSize / statsTotalPointclouds):G4}, fps_dropped={statsDrops / Interval():F2}, selfdrop_fps={statsSelfDrops / Interval():F2},  encoder_queue_ms={(int)(statsQueuedDuration / statsTotalPointclouds)}, downsample_ms={statsDownsampleDuration/statsTotalPointclouds:F2}, pc_timestamp={timestamp}, aggregate_packets={statsAggregatePackets}");
                    if (statsDrops > 1 + 3 * Interval())
                    {
                        double ok_fps = (statsTotalPointclouds - statsDrops) / Interval();
                        Debug.LogWarning($"{name}: excessive dropped frames. Set LocalUser.PCSelfConfig.frameRate <= {ok_fps:F2}  in config.json.");
                    }
                    Clear();
                    statsTotalPoints = 0;
                    statsTotalPointclouds = 0;
                    statsTotalPointSize = 0;
                    statsDrops = 0;
                    statsSelfDrops = 0;
                    statsQueuedDuration = 0;
                    statsDownsampleDuration = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }
}
