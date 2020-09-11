using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Workers {
    public class PrerecordedReader : TiledWorker {
        List<string> filenames;
        bool ply;
        bool loop;
        System.TimeSpan frameInterval;  // Interval between frame grabs, if maximum framerate specified
        System.DateTime earliestNextCapture;    // Earliest time we want to do the next capture, if non-null.
        QueueThreadSafe outQueue;
        QueueThreadSafe out2Queue;

        PrerecordedReader(QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(WorkerType.Init)
        {
            if (_outQueue == null)
            {
                throw new System.Exception("{Name()}: outQueue is null");
            }
            outQueue = _outQueue;
            out2Queue = _out2Queue;
        }

        public PrerecordedReader(string dirname, bool _ply, bool _loop, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : this(_outQueue, _out2Queue)
        {
            ply = _ply;
            var _filenames = System.IO.Directory.GetFiles(dirname, ply ? "*.ply" : "*.cwipcdump");
            Debug.Log($"{Name()}: Recording consists of {_filenames.Length} files");
            filenames = new List<string>(_filenames);
            filenames.Sort();
            loop = _loop;
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
            if (out2Queue != null && !out2Queue.IsClosed()) out2Queue.Close();
        }

        public override void OnStop() {
            base.OnStop();
            filenames = null;
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
            if (filenames.Count == 0) return;
            var nextFilename = filenames[0];
            filenames.RemoveAt(0);
            if (loop) filenames.Add(nextFilename);
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
            bool didDrop = false;
            if (outQueue == null)
            {
                Debug.LogError($"{Name()}: no outQueue, dropping pointcloud");
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
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: {Name()}: {statsTotalPointclouds / statsInterval} fps, {(int)(statsTotalPoints / statsTotalPointclouds)} points per cloud, {statsDrops / statsInterval} drops per second");
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
