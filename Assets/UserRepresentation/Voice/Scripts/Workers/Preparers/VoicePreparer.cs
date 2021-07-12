using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class VoicePreparer : BasePreparer
    {
        int bufferSize;

        QueueThreadSafe inQueue;
        public ulong currentTimestamp;
        public int currentQueueSize;
        BaseMemoryChunk currentAudioFrame;
        bool readNextFrameWhenNeeded = true;
        public VoicePreparer(QueueThreadSafe _inQueue) : base(WorkerType.End)
        {
            stats = new Stats(Name());
            inQueue = _inQueue;
            if (inQueue == null) Debug.LogError($"VoicePreparer: Programmer error: ERROR inQueue=NULL");
            bufferSize = 320 * 6 * 100;
            Debug.Log("VoicePreparer: Started.");
            // xxxjack stats not used? stats = new Stats(Name());
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            Debug.Log("VoicePreparer: Stopped");
        }

        protected override void Update()
        {
            base.Update();
        }
        public override void Synchronize()
        {
            // Synchronize playout for the current frame with other preparers (if needed)
            if (synchronizer)
            {
                ulong nextTimestamp = inQueue._PeekTimestamp(currentTimestamp + 1);
                synchronizer.SetTimestampRangeForCurrentFrame(Name(), currentTimestamp, nextTimestamp);
            }
        }
        public override bool LatchFrame()
        {
            lock (this)
            {
                readNextFrameWhenNeeded = true;
                ulong bestTimestamp = 0;
                if (synchronizer != null)
                {
                    bestTimestamp = synchronizer.GetBestTimestampForCurrentFrame();
                    if (bestTimestamp != 0 && bestTimestamp <= currentTimestamp)
                    {
                        if (true || synchronizer.debugSynchronizer) Debug.Log($"{Name()}: show nothing for frame {UnityEngine.Time.frameCount}: {currentTimestamp - bestTimestamp} ms in the future: bestTimestamp={bestTimestamp}, currentTimestamp={currentTimestamp}");
                        readNextFrameWhenNeeded = false;
                        return false;
                    }
                    if (true || bestTimestamp != 0 && synchronizer.debugSynchronizer) Debug.Log($"{Name()}: frame {UnityEngine.Time.frameCount} bestTimestamp={bestTimestamp}, currentTimestamp={currentTimestamp}, {bestTimestamp - currentTimestamp} ms too late");
                }
                // xxxjack Note: we are holding the lock during TryDequeue. Is this a good idea?
                // xxxjack Also: the 0 timeout to TryDecode may need thought.
                if (inQueue.IsClosed()) return false; // We are shutting down
                if (currentAudioFrame != null)
                {
                    Debug.LogWarning($"{Name()}: previous audio frame not consumed");
                    currentAudioFrame.free();
                }
                return _FillAudioFrame(bestTimestamp);
            }
         }

        bool _FillAudioFrame(ulong minTimestamp)
        {
            while(true)
            {
                currentAudioFrame = inQueue.TryDequeue(0);
                if (currentAudioFrame == null) {
                    stats.statsUpdate(false, true);
                    return false;
                }
                currentTimestamp = (ulong)currentAudioFrame.info.timestamp;
                bool trySkipForward = currentTimestamp < minTimestamp;
                if (trySkipForward)
                {
                    if (inQueue._PeekTimestamp(minTimestamp + 1) < minTimestamp)
                    {
                        // There is another frame in the queue that is also earlier than minTimestamp.
                        // Drop this one.
                        stats.statsUpdate(true, false);
                        currentAudioFrame.free();
                        currentAudioFrame = null;
                        continue;
                    }
                }
                stats.statsUpdate(false, false);
                break;
            }
            if (currentAudioFrame == null)
            {
                if (synchronizer != null && true || synchronizer.debugSynchronizer && currentTimestamp != 0)
                {
                    Debug.Log($"{Name()}: no audio frame available");
                }
                return false;
            }
            return true;
        }

        public bool GetAudioBuffer(float[] dst, int len)
        {
            lock(this)
            {
                if (inQueue.IsClosed()) return false;
                if (currentAudioFrame == null && readNextFrameWhenNeeded) _FillAudioFrame(0);
                if (currentAudioFrame == null) return false;
                currentTimestamp = (ulong)currentAudioFrame.info.timestamp;
                currentQueueSize = inQueue._Count;
                if (currentAudioFrame is FloatMemoryChunk)
                {
                    System.Array.Copy(((FloatMemoryChunk)currentAudioFrame).buffer, 0, dst, 0, len);
                }
                else
                {
                    System.Runtime.InteropServices.Marshal.Copy(currentAudioFrame.pointer, dst, 0, len);
                }
                currentAudioFrame.free();
                currentAudioFrame = null;
                return true;
            }
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalUpdates;
            double statsDrops;
            double statsNoData;

            public void statsUpdate(bool dropped, bool noData)
            {

                statsTotalUpdates += 1;
                if (dropped) statsDrops++;
                if (noData) statsNoData++;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalUpdates / Interval():F2}, fps_dropped={statsDrops / Interval():F2}, fps_nodata={statsNoData / Interval():F2}");
                 }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalUpdates = 0;
                    statsDrops = 0;
                    statsNoData = 0;
                }
            }
        }

        protected Stats stats;

    }
}
