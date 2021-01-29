using System.Collections.Generic;
using UnityEngine;
using VRTCore;

namespace VRT.UserRepresentation.PointCloud
{

    public class PrerecordedReader : TiledWorker
    {
        List<PrerecordedTileReader> tileReaders = new List<PrerecordedTileReader>();
        public SharedCounter sharedCounter = new SharedCounter();
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public int numberOfFilesPerReader = 0;
        string[] qualitySubdirs;
        
        public PrerecordedReader(string[] _qualities) : base(WorkerType.Init)
        {
            if (_qualities == null)
            {
                qualitySubdirs = new string[1] { "" };
            } else
            {
                qualitySubdirs = _qualities;
            }
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public void Add(string dirname, bool _ply, bool _loop, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null)
        {
            string subDir = qualitySubdirs[0];
            PrerecordedTileReader tileReader = new PrerecordedTileReader(this, tileReaders.Count, dirname, subDir, _ply, _loop, _frameRate, _outQueue, _out2Queue);
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

    public class PrerecordedTileReader : BaseWorker
    {
        string dirname;
        string subdir;
        string[] filenames; // All files in the sequence
        SharedCounter positionCounter;
        bool ply;
        bool loop;
        System.TimeSpan frameInterval;  // Interval between frame grabs, if maximum framerate specified
        System.DateTime earliestNextCapture;    // Earliest time we want to do the next capture, if non-null.
        QueueThreadSafe outQueue;
        QueueThreadSafe out2Queue;
        int thread_index;
        PrerecordedReader parent;

        public PrerecordedTileReader(PrerecordedReader _parent, int _index,  string _dirname, string _subdir, bool _ply, bool _loop, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(WorkerType.Init)
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
            ply = _ply;
            string pattern = ply ? "*.ply" : "*.cwipcdump";
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
            loop = _loop && filenames.Length > 0;
            if (_frameRate > 0)
            {
                frameInterval = System.TimeSpan.FromSeconds(1 / _frameRate);
            }
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
            if (!loop && curIndex >= filenames.Length) return;
            curIndex = curIndex % filenames.Length;
            var nextFilename = System.IO.Path.Combine(dirname, subdir, filenames[curIndex]);

            //xxxshishir set current position for tile selection
            PrerecordedTileSelector.curIndex = curIndex;

            cwipc.pointcloud pc;
            if (ply)
            {
                System.UInt64 timestamp = 0;
                pc = cwipc.read(nextFilename, timestamp);
            }
            else
            {
                pc = cwipc.readdump(nextFilename);
            }
            if (pc == null) return;
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
            statsUpdate(pc.count(), didDropSelfView, didDropEncoder);
            pc.free();
        }

        System.DateTime statsLastTime;
        double statsTotalPoints;
        double statsTotalPointclouds;
        double statsDropsSelf;
        double statsDropsEncoder;
        const int statsInterval = 10;

        public void statsUpdate(int pointCount, bool droppedSelf=false, bool droppedEncoder=false)
        {
            if (statsLastTime == null)
            {
                statsLastTime = System.DateTime.Now;
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
                statsDropsSelf = 0;
                statsDropsEncoder = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(statsInterval))
            {
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: {Name()}: {statsTotalPointclouds / statsInterval} fps, {(int)(statsTotalPoints / statsTotalPointclouds)} points per cloud, {statsDropsSelf / statsInterval} selfview drops per second, {statsDropsEncoder / statsInterval} encoder drops per second");
                if (statsDropsSelf > 3 * statsInterval || statsDropsEncoder > 3 * statsInterval)
                {
                    Debug.LogWarning($"{Name()}: excessive dropped frames. Lower LocalUser.PCSelfConfig.frameRate in config.json.");
                }
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
                statsDropsSelf = 0;
                statsDropsEncoder = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalPoints += pointCount;
            statsTotalPointclouds += 1;
            if (droppedSelf) statsDropsSelf++;
            if (droppedEncoder) statsDropsEncoder++;
        }
    }
}
