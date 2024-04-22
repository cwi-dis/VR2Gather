using System;
using System.Collections.Generic;
using UnityEngine;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace Cwipc
{

    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    /// <summary>
    /// AsyncPrerecordedBaseReader reads pointclouds from .ply or .cwipcdump files.
    ///
    /// It contains the common code for two distinct use cases (subclasses):
    /// - AsyncPrerecordedReader reads a single directory full of files, where each file may contain
    /// a pointcloud with multiple tiles. It is used to simulate a users' self-representation
    /// from a prerecorded set of pointclouds.
    /// - AsyncPrerecordedPlaybackReader reads a multilevel directory structure, with each tile and quality
    /// level is a distinct directory. It is meant for playback, not self-representation, for the
    /// quality-assessment experiments.
    ///
    /// </summary>
    public abstract class AsyncPrerecordedBaseReader : AsyncPointCloudReader
    {
        /// <summary>
        /// Structure describing directory tree with point clouds stored
        /// in individual per-tile per-quality files. This structure is used
        /// to do instantaneous switching between qualities, for UX experiments.
        ///
        /// Commonly read from tileconfig.json in the top level directory.
        ///
        /// NOTE: this structure is slightly different from the one in AsyncPrerecordedReader,
        /// for no good reason.
        /// </summary>
        [Serializable]
        public class _PrerecordedReaderConfig
        {
            public string[] tiles;  // Only for PrerecordedPlaybackReader: subdirectory names per tile
            public string[] qualities; // Only for prerecordedPlaybackReader: subsubdirectory names per quality
            public bool ply;    // True when using .ply files, false when using .cwipcdump files
            public bool preferBest; // Start reading best (last) quality (default is first/worst) until instructed otherwise
            public PointCloudTileDescription[] tileInfo; // Only for PrerecordedLiveReader: list of tile definitions
        };
        protected string baseDirectory;
        List<_SingleDirectoryReader> tileReaders = new List<_SingleDirectoryReader>();
        public SharedCounter sharedCounter = new SharedCounter();
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public int numberOfFilesPerReader = 0;
        protected string[] qualitySubdirs;
        protected string[] tileSubdirs;
        protected PointCloudTileDescription[] tileInfo;
        bool preferBest;
        // Next variables are shared (readonly) among _SingleDirectoryReader children.
        // I don't think C# has a way to say this without using public.
        public bool newTimestamps = false;
        public bool readPlyFiles;
        public float frameRate;
        public int remainingLoopCount = -1;
        public bool multireader = false;
        
        public AsyncPrerecordedBaseReader(string directory, float _voxelSize, float _frameRate, int loopCount=0) : base(null)
        {
            voxelSize = _voxelSize;
            frameRate = _frameRate;
            if (loopCount <= 0)
            {
                remainingLoopCount = -1;
            }
            else
            {
                remainingLoopCount = loopCount;
            }

            baseDirectory = directory;
            _InitFromConfigFile(directory);
        }

        protected override void AsyncUpdate()
        {
        }

        protected void _InitFromConfigFile(string directory)
        {
            var configFilename = System.IO.Path.Combine(directory, "tileconfig.json");
            if (!System.IO.File.Exists(configFilename))
            {
                Debug.LogWarning($"{Name()}: {configFilename} does not exist. Guessing defaults.");
                bool hasPly = System.IO.Directory.GetFiles(directory, "*.ply").Length != 0;
                bool hasCwipcdump = System.IO.Directory.GetFiles(directory, "*.cwipcdump").Length != 0;
                if (!hasPly && !hasCwipcdump)
                {
                    Debug.LogWarning($"{Name()}: {directory} contains neither .ply nor .cwipcdump files");
                }
                if (hasPly && hasCwipcdump)
                {
                    Debug.LogWarning($"{Name()}: {directory} contains both .ply and .cwipcdump files, showing .ply");
                }
                readPlyFiles = hasPly;
                return;
            }
            var file = System.IO.File.ReadAllText(configFilename);
            _PrerecordedReaderConfig config = JsonUtility.FromJson<_PrerecordedReaderConfig>(file);
            preferBest = config.preferBest;
            qualitySubdirs = config.qualities ?? new string[1] { "" };
            tileSubdirs = config.tiles; // can be null
            tileInfo = config.tileInfo ?? new PointCloudTileDescription[1]
            {
                new PointCloudTileDescription {
                    normal = new Vector3 {x=0, y=0, z=0},
                    cameraName = "all",
                    cameraMask = 0
                }
            };
            readPlyFiles = config.ply;
        }

        /// <summary>
        /// Return array of available tiles.
        /// </summary>
        /// <returns></returns>
        public override PointCloudTileDescription[] getTiles()
        {
            return tileInfo;
        }

        public void Add(string tilename, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null)
        {
            string tileDirectory = tilename == null ? baseDirectory : System.IO.Path.Combine(baseDirectory, tilename);
            string subDir = qualitySubdirs == null ? "" : qualitySubdirs[0];
            if (preferBest)
            {
                subDir = qualitySubdirs[qualitySubdirs.Length - 1];
            }
            _SingleDirectoryReader tileReader = new _SingleDirectoryReader(this, tileReaders.Count, tileDirectory, subDir, _outQueue, _out2Queue);
            tileReaders.Add(tileReader);
        }

        public void SelectTileQualities(int[] qualities)
        {
            for(int i=0; i<qualities.Length; i++)
            {
                string newSubDir = qualitySubdirs[qualities[i]];
                tileReaders[i].setSubDir(newSubDir);
            }
        }

        public void StopWithoutClose()
        {
            foreach(var tr in tileReaders)
            {
                tr.StopWithoutClose();
            }
            Stop();
        }

        public override void Stop()
        {
            base.Stop();
            foreach(var tr in tileReaders)
            {
                tr.Stop();
            }
        }

        /// <summary>
        /// Report current timestamp to subclass.
        ///
        /// This is a gross hack. The prerecorded reader needs this information to report to the
        /// selection code for the quality assessment experiment. Does not need to be implemented except for this special case.
        /// </summary>
        /// <param name="curIndex"></param>
        abstract public void ReportCurrentTimestamp(Timestamp curIndex);
    }

    public class SharedCounter
    {
        System.Threading.Barrier barrier = null;

        public SharedCounter()
        {
            barrier = new System.Threading.Barrier(0);
        }
        public void Subscribe()
        {
            barrier.AddParticipant();
        }

        public long WaitAndGet()
        {
            barrier.SignalAndWait();
            return barrier.CurrentPhaseNumber;
        }
    }

    //
    // Helper class for PrerecordedBaseReader.
    // Reads files from a single directory and feeds the pointclouds into 1 or 2
    // QueueThreadSafe queues.
    //
    // The parent PrerecordedBaseReader may change the directory to read from on the fly,
    // as long as the old and new directories contain exactlhy the same number of files
    // with the same filenames.
    //
    // The reason for this convoluted organization is that it allows very fast (near instantaneous)
    // switching of representations (and therefore quality levels) for the PrerecordedPlaybackReader,
    // which is needed for the quality assessment trial.
    //
    public class _SingleDirectoryReader : AsyncWorker
    {
        string dirname;
        string subdir;
        string[] filenames; // All files in the sequence
        SharedCounter positionCounter;
        System.TimeSpan frameInterval;  // Interval between frame grabs, if maximum framerate specified
        System.DateTime earliestNextCapture;    // Earliest time we want to do the next capture, if non-null.
        QueueThreadSafe outQueue;
        QueueThreadSafe out2Queue;
        int thread_index;
        AsyncPrerecordedBaseReader parent;

        public _SingleDirectoryReader(AsyncPrerecordedBaseReader _parent, int _index,  string _dirname, string _subdir, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base()
        {
            dirname = _dirname;
            subdir = _subdir;
            parent = _parent;
            if (_outQueue == null)
            {
                throw new System.Exception($"{Name()}: outQueue is null");
            }
            if (outQueue != null)
            {
                throw new System.Exception($"{Name()}: only single Add() allowed");
            }
            positionCounter = parent.sharedCounter;
            positionCounter.Subscribe();
            thread_index = _index;
            outQueue = _outQueue;
            out2Queue = _out2Queue;
            string pattern = parent.readPlyFiles ? "*.ply" : "*.cwipcdump";
            filenames = System.IO.Directory.GetFileSystemEntries(System.IO.Path.Combine(dirname, subdir), pattern);
            // Remove path, keep only filename 
            for(int i=0; i<filenames.Length; i++)
            {
                filenames[i] = System.IO.Path.GetFileName(filenames[i]);
            }
            // Sort alphabetically
            System.Array.Sort(filenames);
            Debug.Log($"{Name()}: Recording consists of {filenames.Length} files");
            if (filenames.Length == 0) throw new System.Exception($"{Name()}: no files matching {pattern} found in {dirname}");
            if (parent.numberOfFilesPerReader == 0) parent.numberOfFilesPerReader = filenames.Length;
            if (parent.numberOfFilesPerReader != filenames.Length)
            {
                Debug.LogError($"{Name()}: inconsistent tiling. Some tiles have {parent.numberOfFilesPerReader} file but there are {filenames.Length} in {dirname}");
            }
            if (parent.frameRate > 0)
            {
                frameInterval = System.TimeSpan.FromSeconds(1 / parent.frameRate);
            }
            else
            {
                Debug.LogError($"{Name()}: Invalid framerate, the target framerate is set to 0, sync is disabled");
            }
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
            Start();
        }
        public override string Name()
        {
            if (parent.multireader)
            {
                return $"{parent.Name()}.{thread_index}";
            }
            else
            {
                return $"{parent.Name()}";
            }
        }

        public void setSubDir(string newSubDir)
        {
            if (subdir == newSubDir) return;
            // Debug.Log($"{Name()}: xxxjack setSubDir {subdir} -> {newSubDir}");
            subdir = newSubDir;
        }

        public void StopWithoutClose()
        {
            outQueue = null;
            out2Queue = null;
            Stop();
        }

        public override void Stop()
        {
            base.Stop();
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            outQueue = null;
            if (out2Queue != null && !out2Queue.IsClosed()) out2Queue.Close();
            out2Queue = null;
        }

        public override void AsyncOnStop() {
            base.AsyncOnStop();
            filenames = null;
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            outQueue = null;
            if (out2Queue != null && !out2Queue.IsClosed()) out2Queue.Close();
        }

        protected override void AsyncUpdate() {

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
            // Check whether we have to start from the top, or are done.
            long curIndex = positionCounter.WaitAndGet();
            if (filenames == null)
            {
                Debug.Log($"{Name()}: xxxjack Update() called while already stopping");
                return;
            }
            if (curIndex > 0 && curIndex % filenames.Length == 0 && parent.remainingLoopCount > 0)
            {
                parent.remainingLoopCount--;
                Debug.Log($"{Name()}: end of loop, {parent.remainingLoopCount} more");
                if (parent.remainingLoopCount == 0)
                {
                    outQueue?.Close();
                    out2Queue?.Close();
                    filenames = null;
                    return;
                }
            }
            curIndex = curIndex % filenames.Length;
            var nextFilename = System.IO.Path.Combine(dirname, subdir, filenames[curIndex]);

            parent.ReportCurrentTimestamp(curIndex);

            cwipc.pointcloud pc;
            if (parent.readPlyFiles)
            {
                Timestamp timestamp = 0;
                pc = cwipc.read(nextFilename, timestamp);
            }
            else
            {
                pc = cwipc.readdump(nextFilename);
            }
            if (pc == null) return;
            pc.metadata.filename = nextFilename;
            if (parent.newTimestamps) {
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                Timestamp timestamp = (Timestamp)sinceEpoch.TotalMilliseconds;
                pc._set_timestamp(timestamp);
            }
            Timedelta downsampleDuration = 0;
            if (parent.voxelSize != 0)
            {
                System.DateTime downsampleStartTime = System.DateTime.Now;
                cwipc.pointcloud voxelizedPC = cwipc.downsample(pc, parent.voxelSize);
                if (voxelizedPC == null)
                {
                    Debug.LogError($"{Name()}: downsample({parent.voxelSize}) failed to produce new pc");
                } else
                {
                    pc.free();
                    pc = voxelizedPC;
                }
                System.DateTime downsampleStopTime = System.DateTime.Now;
                downsampleDuration = (Timedelta)(downsampleStopTime - downsampleStartTime).TotalMilliseconds;
            }
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            bool didDropSelfView = false;
            bool didDropEncoder = false;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
            Timedelta encoderQueuedDuration = 0;
            if (outQueue == null || outQueue.IsClosed())
            {
                Debug.LogError($"{Name()}: no outQueue, dropping pointcloud");
                didDropSelfView = true;
            }
            else
            {
                bool ok = outQueue.Enqueue(pc.AddRef());
                if (!ok)
                {
                    didDropSelfView = true;
                }
            }
            if (out2Queue == null || out2Queue.IsClosed())
            {
                // This is not an error. Debug.LogError($"{Name()}: no outQueue2, dropping pointcloud");
            }
            else
            {
                encoderQueuedDuration = out2Queue.QueuedDuration();
                bool ok = out2Queue.Enqueue(pc.AddRef());
                if (!ok)
                {
                    didDropEncoder = true;
                }
            }

#if VRT_WITH_STATS
            stats.statsUpdate(pc.count(), pc.cellsize(), downsampleDuration, didDropEncoder, didDropSelfView, encoderQueuedDuration, pc.timestamp(), subdir);
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

            public void statsUpdate(int pointCount, float pointSize, Timedelta downsampleDuration, bool dropped, bool droppedSelf, Timedelta queuedDuration, Timestamp timestamp, string subdir)
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
                    string msg = $"fps={statsTotalPointclouds / Interval():F2}, points_per_cloud={(int)(statsTotalPoints /  statsTotalPointclouds)}, avg_pointsize={(statsTotalPointSize / statsTotalPointclouds):G4}, fps_dropped={statsDrops / Interval():F2}, fps_dropped_self={statsSelfDrops / Interval():F2}, encoder_queue_ms={(int)(statsQueuedDuration / statsTotalPointclouds)}, downsample_ms={statsDownsampleDuration / statsTotalPointclouds:F2}, pc_timestamp={timestamp}, aggregate_packets={statsAggregatePackets}";
                    if (subdir != null && subdir != "")
                    {
                        msg += $", quality={subdir}";
                    }
                    Output(msg);
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
