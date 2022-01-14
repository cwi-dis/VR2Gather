using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class VoicePreparer : BasePreparer
    {
        int bufferSize;

        public ulong currentTimestamp;
        public int currentQueueSize;
        BaseMemoryChunk currentAudioFrame;
        bool readNextFrameWhenNeeded = true;
        // We should _not_ drop audio frames if they are in the past, but could still be
        // considered part of the current visual frame. Otherwise, we may end up dropping one
        // audio frame for every visual frame (because the visual clock jumps forward over the
        // audio clock).
        const int VISUAL_FRAME_DURATION_MS = 66;

        public VoicePreparer(QueueThreadSafe _inQueue) : base(_inQueue)
        {
            stats = new Stats(Name());
            bufferSize = 320 * 6 * 100;
            Debug.Log("VoicePreparer: Started.");
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            Debug.Log("VoicePreparer: Stopped");
        }

        public override void Synchronize()
        {
            // Synchronize playout for the current frame with other preparers (if needed)
            if (synchronizer)
            {
                ulong earliestTimestamp = currentTimestamp;
                if (earliestTimestamp == 0) earliestTimestamp = InQueue._PeekTimestamp();
                ulong latestTimestamp = InQueue.LatestTimestamp();
                synchronizer.SetTimestampRangeForCurrentFrame(Name(), earliestTimestamp, latestTimestamp);
            }
        }
        public override bool LatchFrame()
        {
            lock (this)
            {
                readNextFrameWhenNeeded = true;
                ulong bestTimestamp = 0;
                if (currentAudioFrame != null)
                {
                    // Debug.Log($"{Name()}: previous audio frame not consumed yet");
                    return true;
                }
                if (synchronizer != null)
                {
                    bestTimestamp = synchronizer.GetBestTimestampForCurrentFrame();
                    if (bestTimestamp != 0 && bestTimestamp <= currentTimestamp)
                    {
                        if (synchronizer.debugSynchronizer) Debug.Log($"{Name()}: show nothing for frame {UnityEngine.Time.frameCount}: {currentTimestamp - bestTimestamp} ms in the future: bestTimestamp={bestTimestamp}, currentTimestamp={currentTimestamp}");
                        readNextFrameWhenNeeded = false;
                        return false;
                    }
                    if (bestTimestamp != 0 && synchronizer.debugSynchronizer) Debug.Log($"{Name()}: frame {UnityEngine.Time.frameCount} bestTimestamp={bestTimestamp}, currentTimestamp={currentTimestamp}, {bestTimestamp - currentTimestamp} ms too late");
                }
                // xxxjack Note: we are holding the lock during TryDequeue. Is this a good idea?
                // xxxjack Also: the 0 timeout to TryDecode may need thought.
                if (InQueue.IsClosed()) return false; // We are shutting down
                  return _FillAudioFrame(bestTimestamp);
            }
         }

        bool _FillAudioFrame(ulong minTimestamp)
        {
            int dropCount = 0;
            while(true)
            {
                currentAudioFrame = InQueue.TryDequeue(0);
                if (currentAudioFrame == null) {
                    if (synchronizer != null && synchronizer.debugSynchronizer && currentTimestamp != 0)
                    {
                        Debug.Log($"{Name()}: no audio frame available");
                    }
                    stats.statsUpdate(dropCount, true);
                    return false;
                }
                if (minTimestamp > 0)
                {
                    currentTimestamp = (ulong)currentAudioFrame.info.timestamp;
                    bool trySkipForward = currentTimestamp < minTimestamp - VISUAL_FRAME_DURATION_MS;
                    if (trySkipForward)
                    {
                        bool canDrop = InQueue._PeekTimestamp(minTimestamp + 1) < minTimestamp;
                        // Debug.Log($"{Name()}: xxxjack trySkipForward _FillAudioFrame({minTimestamp}) currentTimestamp={currentTimestamp}, delta={minTimestamp - currentTimestamp}, candrop={canDrop}");
                        if (canDrop)
                        {
                            // There is another frame in the queue that is also earlier than minTimestamp.
                            // Drop this one.
                            dropCount++;
                            currentAudioFrame.free();
                            currentAudioFrame = null;
                            continue;
                        }
                    }

                }
                stats.statsUpdate(dropCount, false);
                break;
            }
            return true;
        }

        public bool GetAudioBuffer(float[] dst, int len)
        {
            lock(this)
            {
                if (InQueue.IsClosed()) return false;
                if (currentAudioFrame == null && readNextFrameWhenNeeded) _FillAudioFrame(0);
                if (currentAudioFrame == null) return false;
                currentTimestamp = (ulong)currentAudioFrame.info.timestamp;
                currentQueueSize = InQueue._Count;
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

            public void statsUpdate(int dropCount, bool noData)
            {

                statsTotalUpdates += 1;
                statsDrops += dropCount;
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
