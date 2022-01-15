using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class VoicePreparer : BasePreparer
    {
        
        public ulong currentTimestamp;
        public int currentQueueSize;
        BaseMemoryChunk currentAudioFrame;
        float[] audioBuffer;
        bool readNextFrameWhenNeeded = true;
        // We should _not_ drop audio frames if they are in the past, but could still be
        // considered part of the current visual frame. Otherwise, we may end up dropping one
        // audio frame for every visual frame (because the visual clock jumps forward over the
        // audio clock).
        const int VISUAL_FRAME_DURATION_MS = 66;

        public VoicePreparer(QueueThreadSafe _inQueue) : base(_inQueue)
        {
            stats = new Stats(Name());
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

        int _fillFromAudioBuffer(float[] dst, int position, int len)
        {
            // If the buffer is empty we do nothing.
            if (audioBuffer == null) return 0;
            // If the buffer has no more samples than what we want we copy everything
            // and delete the buffer
            if (audioBuffer.Length <= len)
            {
                int copyCount = audioBuffer.Length;
                System.Array.Copy(audioBuffer, 0, dst, position, copyCount);
                audioBuffer = null;
                return copyCount;
            }
            // If the buffer has more samples we copy what we need and keep the rest.
            System.Array.Copy(audioBuffer, 0, dst, 0, len);
            int remaining = audioBuffer.Length - len;
            float[] leftOver = new float[remaining];
            System.Array.Copy(audioBuffer, len, leftOver, 0, remaining);
            audioBuffer = leftOver;
            return len;
        }

        bool _fillIntoAudioBuffer()
        {
            if (audioBuffer != null)
            {
                Debug.LogError($"{Name()}: Programmer error: __fillIntoAudioBuffer() called while audioBuffer is not empty");
                return true;
            }
            if (currentAudioFrame == null && readNextFrameWhenNeeded) _FillAudioFrame(0);
            if (currentAudioFrame == null) return false;
            currentTimestamp = (ulong)currentAudioFrame.info.timestamp;
            if (currentAudioFrame is FloatMemoryChunk)
            {
                var _frame = ((FloatMemoryChunk)currentAudioFrame);
                int availableLen = _frame.elements;
                audioBuffer = new float[availableLen];
                System.Array.Copy(_frame.buffer, 0, audioBuffer, 0, availableLen);
            }
            else
            {
                int availableLen = currentAudioFrame.length / sizeof(float);
                audioBuffer = new float[availableLen];
                System.Runtime.InteropServices.Marshal.Copy(currentAudioFrame.pointer, audioBuffer, 0, availableLen);
            }
            currentAudioFrame.free();
            currentAudioFrame = null;
            return true;
        }

        public bool GetAudioBuffer(float[] dst, int len)
        {
            lock(this)
            {
                if (InQueue.IsClosed()) return false;
                int position = 0;
                _fillIntoAudioBuffer();
                while (len > 0)
                {
                    int curLen = _fillFromAudioBuffer(dst, position, len);
                    // If we didn't copy anything this time we're done. And we return true if we have copied anything at all.
                    if (curLen == 0)
                    {
                        Debug.Log($"{Name()}: inserted {len} zero samples");
                        return (position != 0);
                    }
                    position += curLen;
                    len -= curLen;
                }
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
