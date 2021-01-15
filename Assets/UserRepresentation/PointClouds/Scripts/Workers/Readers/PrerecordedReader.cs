using System.Collections.Generic;
using UnityEngine;
using VRTCore;

namespace VRT.UserRepresentation.PointCloud
{

    public class PrerecordedReader : TiledWorker
    {
        List<PrerecordedTileReader> tileReaders = new List<PrerecordedTileReader>();

        public PrerecordedReader() : base(WorkerType.Init)
        {
        }

        public void Add(string dirname, bool _ply, bool _loop, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null)
        {
            PrerecordedTileReader tileReader = new PrerecordedTileReader(dirname, _ply, _loop, _frameRate, _outQueue, _out2Queue);
            tileReaders.Add(tileReader);
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

    public class PrerecordedTileReader : BaseWorker
    {
        string[] filenames; // All files in the sequence
        int filenamesCurIndex;  // Where we are now
        bool ply;
        bool loop;
        System.TimeSpan frameInterval;  // Interval between frame grabs, if maximum framerate specified
        System.DateTime earliestNextCapture;    // Earliest time we want to do the next capture, if non-null.
        QueueThreadSafe outQueue;
        QueueThreadSafe out2Queue;

        public PrerecordedTileReader(string dirname, bool _ply, bool _loop, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(WorkerType.Init)
        {
            if (_outQueue == null)
            {
                throw new System.Exception("{Name()}: outQueue is null");
            }
            if (outQueue != null)
            {
                throw new System.Exception($"{Name()}: only single Add() allowed");
            }
            outQueue = _outQueue;
            out2Queue = _out2Queue;
            ply = _ply;
            filenames = System.IO.Directory.GetFiles(dirname, ply ? "*.ply" : "*.cwipcdump");
            System.Array.Sort(filenames);
            Debug.Log($"{Name()}: Recording consists of {filenames.Length} files");
            filenamesCurIndex = 0;
            loop = _loop && filenames.Length > 0;
            if (_frameRate > 0)
            {
                frameInterval = System.TimeSpan.FromSeconds(1 / _frameRate);
            }
            Start();
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
            if (loop)
            {
                if (filenamesCurIndex >= filenames.Length) filenamesCurIndex = 0;
            }
            if (filenames == null || filenamesCurIndex >= filenames.Length)
            {
                Debug.Log($"{Name()}: xxxjack Update() called while already stopping");
                return;
            }

            var nextFilename = filenames[filenamesCurIndex];
            filenamesCurIndex++;

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
