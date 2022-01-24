﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class VoicePreparer : BasePreparer
    {
        const bool debugBuffering = false;
        ulong currentTimestamp;
        public int currentQueueSize;
        BaseMemoryChunk currentAudioFrame;
        //
        // Keeping track of the "current" timestamp for audio is tricky. We remember the offset
        // from system clock to audio clock as at was valid the last time we returned a frame to the receiver.
        // We also compute when we expect the next call from the receiver, so we can attempt to
        // do the right thing when synchronizing.
        //
        long sysClockToAudioClock;
        long nextGetAudioBufferExpected;
        //
        // We need to double-buffer between currentAudioFrame and the receiver, because the buffer
        // sizes can be different.
        //
        float[] audioBuffer;
        long audioBufferHeadTimestamp;

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

        public ulong getCurrentTimestamp()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            long now = (long)sinceEpoch.TotalMilliseconds;

            return (ulong)(now + sysClockToAudioClock);
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
                if (currentAudioFrame != null)
                {
                    Debug.LogError($"{Name()}: _fillAudioFrame called, but currentAudioFrame is not null");
                    currentAudioFrame.free();
                    currentAudioFrame = null;
                }
                currentAudioFrame = InQueue.TryDequeue(0);
                if (currentAudioFrame == null) {
                    if (true || (synchronizer != null && synchronizer.debugSynchronizer && currentTimestamp != 0))
                    {
                        Debug.Log($"{Name()}: no audio frame available");
                    }
                    stats.statsUpdate(0, dropCount, true);
                    return false;
                }
#if VRT_AUDIO_DEBUG
                ToneGenerator.checkToneBuffer("VoicePreparer.InQueue.currentAudioFrame", currentAudioFrame.pointer, currentAudioFrame.length);
#endif
                currentTimestamp = (ulong)currentAudioFrame.info.timestamp;
                if (debugBuffering) Debug.Log($"{Name()}: xxxjack got audioFrame ts={currentAudioFrame.info.timestamp}, bytecount={currentAudioFrame.length}, queue={InQueue.Name()}");
                if (minTimestamp > 0)
                {
                    bool trySkipForward = currentTimestamp < minTimestamp - VISUAL_FRAME_DURATION_MS;
                    if (trySkipForward)
                    {
                        bool canDrop = InQueue._PeekTimestamp(minTimestamp + 1) < minTimestamp;
                        if (debugBuffering) Debug.Log($"{Name()}: xxxjack trySkipForward _FillAudioFrame({minTimestamp}) currentTimestamp={currentTimestamp}, delta={minTimestamp - currentTimestamp}, candrop={canDrop}");
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
                int nSamples = currentAudioFrame.length / sizeof(float);
                stats.statsUpdate(nSamples, dropCount, false);
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
                audioBufferHeadTimestamp = 0; // xxxjack or should we compute what it should have been??!?
                if (debugBuffering) Debug.Log($"{Name()}: xxxjack copied {copyCount} samples, buffer empty, want {len - copyCount} more");
                return copyCount;
            }
            // If the buffer has more samples we copy what we need and keep the rest.
            System.Array.Copy(audioBuffer, 0, dst, position, len);
            int remaining = audioBuffer.Length - len;
            float[] leftOver = new float[remaining];
            System.Array.Copy(audioBuffer, len, leftOver, 0, remaining);
            audioBuffer = leftOver;
            audioBufferHeadTimestamp += (long)(1000 * len / Config.Instance.audioSampleRate);
            if (debugBuffering) Debug.Log($"{Name()}: xxxjack copied all {len} samples, {remaining} left in buffer");
            return len;
        }

        bool _fillIntoAudioBuffer(bool optional)
        {
            if (audioBuffer != null)
            {
                if (!optional)
                {
                    Debug.LogError($"{Name()}: Programmer error: _fillIntoAudioBuffer() called while audioBuffer is not empty");
                }
                return true;
            }
            if (currentAudioFrame == null && readNextFrameWhenNeeded) _FillAudioFrame(0);
            if (currentAudioFrame == null) return false;
            int availableLen = currentAudioFrame.length / sizeof(float);
            if (availableLen*sizeof(float) != currentAudioFrame.length)
            {
                Debug.LogError($"{Name()}: frame contains {currentAudioFrame.length} bytes which is not {availableLen} floats");
            }
            audioBuffer = new float[availableLen];
            System.Runtime.InteropServices.Marshal.Copy(currentAudioFrame.pointer, audioBuffer, 0, availableLen);
            audioBufferHeadTimestamp = currentAudioFrame.info.timestamp;
            currentAudioFrame.free();
            currentAudioFrame = null;
            return true;
        }

        public int GetAudioBuffer(float[] dst, int len)
        {
            lock(this)
            {
                if (InQueue.IsClosed()) return len;
                if (debugBuffering) Debug.Log($"{Name()}: xxxjack getAudioBuffer({len})");
                int position = 0;
                int oldPosition = 0;
                int curLen = 0;
                _fillIntoAudioBuffer(true);
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                long now = (long)sinceEpoch.TotalMilliseconds;
                sysClockToAudioClock = audioBufferHeadTimestamp - now;
                nextGetAudioBufferExpected = now + (len * 1000 / Config.Instance.audioSampleRate);
                while (len > 0)
                {
                    curLen = _fillFromAudioBuffer(dst, position, len);
                    // If we didn't copy anything this time we're done. And we return true if we have copied anything at all.
                    if (curLen == 0)
                    {
                        if (true || debugBuffering) Debug.Log($"{Name()}: xxxjack getAudioBuffer: inserted {len} zero samples from {position}, done={position != 0}");
#if VRT_AUDIO_DEBUG
                        ToneGenerator.checkToneBuffer("VoicePreparer.GetAudioBuffer.partial", dst);
#endif
                        return len;
                    }
                    oldPosition = position;
                    position += curLen;
                    len -= curLen;
                    if (len > 0)
                    {
                        _fillIntoAudioBuffer(false);
                    }
                }
                if (debugBuffering) Debug.Log($"{Name()}: xxxjack getAudioBuffer: done=true");
#if VRT_AUDIO_DEBUG
                ToneGenerator.checkToneBuffer("VoicePreparer.GetAudioBuffer.full", dst);
#endif
                return len;
            }
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalUpdates;
            double statsTotalSamples;
            double statsDrops;
            double statsNoData;

            public void statsUpdate(int nSamples, int dropCount, bool noData)
            {

                statsTotalUpdates += 1;
                statsTotalSamples += nSamples;
                statsDrops += dropCount;
                if (noData) statsNoData++;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalUpdates / Interval():F2}, fps_dropped={statsDrops / Interval():F2}, fps_nodata={statsNoData / Interval():F2}, samples_per_frame={(int)(statsTotalSamples / (statsTotalUpdates-statsNoData))}");
                 }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalUpdates = 0;
                    statsTotalSamples = 0;
                    statsDrops = 0;
                    statsNoData = 0;
                }
            }
        }

        protected Stats stats;

    }
}
