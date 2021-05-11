﻿using System;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{

    // PrerecordedBaseReader reads pointclouds from .ply or .cwipcdump files.
    //
    // It contains the common code for two distinct use cases (subclasses):
    // - PrerecordedLiveReader reads a single directory full of files, where each file may contain
    // a pointcloud with multiple tiles. It is used to simulate a users' self-representation
    // from a prerecorded set of pointclouds.
    // - PrerecordedPlaybackReader reads a multilevel directory structure, with each tile and quality
    // level is a distinct directory. It is meant for playback, not self-representation, for the
    // quality-assessment experiments.
    public class PrerecordedBaseReader : TiledWorker
    {
        [Serializable]
        public class _PrerecordedReaderConfig
        {
            public string[] tiles;  // Only for PrerecordedPlaybackReader: subdirectory names per tile
            public string[] qualities; // Only for prerecordedPlaybackReader: subsubdirectory names per quality
            public bool ply;    // True when using .ply files, false when using .cwipcdump files
            public bool preferBest; // Start reading best (last) quality (default is first/worst) until instructed otherwise
            public TiledWorker.TileInfo[] tileInfos; // Only for PrerecordedLiveReader: list of tile definitions
        };
        protected string baseDirectory;
        List<_SingleDirectoryReader> tileReaders = new List<_SingleDirectoryReader>();
        public SharedCounter sharedCounter = new SharedCounter();
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public int numberOfFilesPerReader = 0;
        protected string[] qualitySubdirs;
        protected string[] tileSubdirs;
        TiledWorker.TileInfo[] tileInfos;
        bool preferBest;
        // Next variables are shared (readonly) among _SingleDirectoryReader children.
        // I don't think C# has a way to say this without using public.
        public bool newTimestamps = false;
        public bool readPlyFiles;
        public float voxelSize;
        public float frameRate;
        public bool loop = true;
        
        public PrerecordedBaseReader(string directory, float _voxelSize, float _frameRate) : base(WorkerType.Init)
        {
            voxelSize = _voxelSize;
            frameRate = _frameRate;

            baseDirectory = directory;
            _InitFromConfigFile(directory);
        }

        protected bool _InitFromConfigFile(string directory)
        {
            var configFilename = System.IO.Path.Combine(directory, ".cwipc_prerecorded_config.json");
            if (!System.IO.File.Exists(configFilename))
            {
                Debug.LogWarning($"{Name()}: {configFilename} does not exist. Assuming defaults.");
                return false;
            }
            var file = System.IO.File.ReadAllText(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/config.json");
            _PrerecordedReaderConfig config = JsonUtility.FromJson<_PrerecordedReaderConfig>(file);
            preferBest = config.preferBest;
            qualitySubdirs = config.qualities ?? new string[1] { "" };
            tileSubdirs = config.tiles; // can be null
            tileInfos = config.tileInfos ?? new TiledWorker.TileInfo[1]
            {
                new TiledWorker.TileInfo {
                    normal = new Vector3 {x=0, y=0, z=0},
                    cameraName = "all",
                    cameraMask = 0
                }
            };
            readPlyFiles = config.ply;
            return true;
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }
        public override TileInfo[] getTiles()
        {
            return tileInfos;
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
        public override void Stop()
        {
            base.Stop();
            foreach(var tr in tileReaders)
            {
                tr.Stop();
            }
        }
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
    public class _SingleDirectoryReader : TiledWorker
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
        PrerecordedBaseReader parent;

        public _SingleDirectoryReader(PrerecordedBaseReader _parent, int _index,  string _dirname, string _subdir, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(WorkerType.Init)
        {
            dirname = _dirname;
            subdir = _subdir;
            if (_outQueue == null)
            {
                throw new System.Exception("{Name()}: outQueue is null");
            }
            if (outQueue != null)
            {
                throw new System.Exception($"{Name()}: only single Add() allowed");
            }
            parent = _parent;
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
            stats = new Stats(Name());
            Start();
        }
        public override string Name()
        {
            return $"{parent.Name()}.{thread_index}";
        }

        public void setSubDir(string newSubDir)
        {
            if (subdir == newSubDir) return;
            // Debug.Log($"{Name()}: xxxjack setSubDir {subdir} -> {newSubDir}");
            subdir = newSubDir;
        }

        public override void Stop()
        {
            base.Stop();
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            outQueue = null;
            if (out2Queue != null && !out2Queue.IsClosed()) out2Queue.Close();
            out2Queue = null;
        }

        public override void OnStop() {
            base.OnStop();
            filenames = null;
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            outQueue = null;
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
            // Check whether we have to start from the top, or are done.
            long curIndex = positionCounter.WaitAndGet();
            if (filenames == null)
            {
                Debug.Log($"{Name()}: xxxjack Update() called while already stopping");
                return;
            }
            if (!parent.loop && curIndex >= filenames.Length) return;
            curIndex = curIndex % filenames.Length;
            var nextFilename = System.IO.Path.Combine(dirname, subdir, filenames[curIndex]);

            //xxxshishir set current position for tile selection
            PrerecordedTileSelector.curIndex = curIndex;

            cwipc.pointcloud pc;
            if (parent.readPlyFiles)
            {
                System.UInt64 timestamp = 0;
                pc = cwipc.read(nextFilename, timestamp);
            }
            else
            {
                pc = cwipc.readdump(nextFilename);
            }
            if (pc == null) return;
            if (parent.newTimestamps) {
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                ulong timestamp = (ulong)sinceEpoch.TotalMilliseconds;
                pc._set_timestamp(timestamp);
            }
            if (parent.voxelSize != 0)
            {
                cwipc.pointcloud voxelizedPC = cwipc.downsample(pc, parent.voxelSize);
                if (voxelizedPC == null)
                {
                    Debug.LogError($"{Name()}: downsample({parent.voxelSize}) failed to produce new pc");
                } else
                {
                    pc.free();
                    pc = voxelizedPC;
                }
            }
            bool didDropSelfView = false;
            bool didDropEncoder = false;
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
                bool ok = out2Queue.Enqueue(pc.AddRef());
                if (!ok)
                {
                    didDropEncoder = true;
                }
            }
          
            stats.statsUpdate(pc.count(), didDropEncoder, didDropSelfView, pc.timestamp(), subdir);
            pc.free();
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalPoints = 0;
            double statsTotalPointclouds = 0;
            double statsDrops = 0;
            double statsSelfDrops = 0;

            public void statsUpdate(int pointCount, bool dropped, bool droppedSelf, ulong timestamp, string subdir)
            {
                
                statsTotalPoints += pointCount;
                statsTotalPointclouds += 1;
                if (dropped) statsDrops++;
                if (droppedSelf) statsSelfDrops++;

                if (ShouldOutput())
                {
                    string msg = $"fps={statsTotalPointclouds / Interval():F2}, points_per_cloud={(int)(statsTotalPoints / (statsTotalPointclouds == 0 ? 1 : statsTotalPointclouds))}, drop_fps={statsDrops / Interval():F2}, selfdrop_fps={statsSelfDrops / Interval():F2}, pc_timestamp={timestamp}";
                    if (subdir != null && subdir != "")
                    {
                        msg += $", quality={subdir}";
                    }
                    Output(msg);
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
