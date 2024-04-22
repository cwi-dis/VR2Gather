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

    public abstract class AsyncPointCloudReader : AsyncReader, ITileDescriptionProvider, IPointCloudPositionProvider
    {
        protected cwipc.source reader;
        public float voxelSize;
        protected System.TimeSpan frameInterval;  // Interval between frame grabs, if maximum framerate specified
        protected System.DateTime earliestNextCapture;    // Earliest time we want to do the next capture, if non-null.
        protected QueueThreadSafe outQueue;
        protected QueueThreadSafe out2Queue;
        protected bool dontWait = false;
        protected float[] bbox;
        private cwipc.pointcloud mostRecentPC = null;
        
        protected AsyncPointCloudReader(QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base()
        {
            outQueue = _outQueue;
            out2Queue = _out2Queue;
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
        }


        public virtual PointCloudTileDescription[] getTiles()
        {
            cwipc.tileinfo[] origTileInfo = reader.get_tileinfo();
            if (origTileInfo == null || origTileInfo.Length == 0) return null;
            int nTile = origTileInfo.Length;
            PointCloudTileDescription[] rv = new PointCloudTileDescription[nTile];
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
        /// <summary>
        /// This method is called on every pointcloud just as it has been gotten from the source, before further processing.
        /// It can be implemented by subclasses, for example to get access to pointcloud metadata.
        /// </summary>
        /// <param name="pc"></param>
        protected virtual void OptionalProcessing(cwipc.pointcloud pc)
        {

        }

        public override void Stop()
        {
            base.Stop();
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            if (out2Queue != null && !out2Queue.IsClosed()) out2Queue.Close();
            if (mostRecentPC != null)
            {
                mostRecentPC.free();
                mostRecentPC = null;
            }
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
           
            cwipc.pointcloud pc = GetOnePointcloud();
           
            if (pc == null) return;
           
            OptionalProcessing(pc);
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
                //Debug.LogError($"Programmer error: {Name()}: no outQueue, dropping pointcloud");
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
            lock(this)
            {
                mostRecentPC?.free();
                mostRecentPC = pc;
            }
        }

        protected virtual cwipc.pointcloud GetOnePointcloud()
        {
            if (dontWait)
            {
                if (!reader.available(false)) return null;
            }
            return reader.get();
        }

        public Vector3? GetPosition()
        {
            cwipc.pointcloud pc = null;
            lock (this)
            {
                pc = mostRecentPC;
                mostRecentPC = null;
            }
            if (pc == null)
            {
                return null;
            }
        
            Vector3? rv = ComputePosition(pc);
            pc.free();
            return rv;
        }

        public int GetCameraCount()
        {
            cwipc.tileinfo[] origTileInfo = reader.get_tileinfo();
            if (origTileInfo == null) return 0;
            return origTileInfo.Length;
        }

        protected Vector3? ComputePosition(cwipc.pointcloud pc)
        {
            cwipc.pointcloud pcTmp;
            bool pcTmpAllocated = false;
            // Find bounding box and centroid of the point cloud
            Vector3 corner1, corner2, centroid;
            if (!AnalysePointcloud(pc, out corner1, out corner2, out centroid))
            {
                return null;
            }
            // If the bounding box is far too big we should limit it and recompute,
            // to get rid of outliers
            Vector3 bbSize = corner2 - corner1;
            if (bbSize.x > 1 || bbSize.y > 1)
            {
                corner1.x = centroid.x - 0.5f;
                corner1.z = centroid.z - 0.5f;
                corner2.x = centroid.x + 0.5f;
                corner2.z = centroid.z + 0.5f;
                pcTmp = cwipc.crop(pc, corner1, corner2);
                pcTmpAllocated = true;
                AnalysePointcloud(pcTmp, out corner1, out corner2, out centroid);
            } 
            else
            {
                pcTmp = pc;
            }
            // limit pointcloud to torso
            // recompute centroid
            Vector3 rv = centroid;
            rv.y = 0;
            bool mirrorX = true; // Should this be an attribute so we can also opt for mirroring Z?
            if (mirrorX)
            {
                rv.x = -rv.x;
            }
            if (pcTmpAllocated)
            {
                pcTmp.free();
            }
            return rv;
        }

        protected bool AnalysePointcloud(cwipc.pointcloud pc, out Vector3 corner1, out Vector3 corner2, out Vector3 centroid)
        {
            // xxxjack we only compute the centroid, based on a random sample of 300 points.
            // Otherwise things become far too expensive.
            const int minPointsForAnalysis = 3000; // xxxjack This should probably be a configurable parameter
            
            corner1 = Vector3.zero;
            corner2 = Vector3.zero;
            centroid = pc.get_centroid(minPointsForAnalysis);
            return pc.count() >= minPointsForAnalysis;
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
