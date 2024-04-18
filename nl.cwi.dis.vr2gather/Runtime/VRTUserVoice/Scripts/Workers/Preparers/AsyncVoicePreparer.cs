using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
using Cwipc;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.UserRepresentation.Voice
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;

    public class AsyncVoicePreparer : AsyncPreparer, IAudioPreparer
    {
        const bool debugBuffering = false;
        const bool debugBufferingMore = false;
        Timestamp currentTimestamp;
        public int currentQueueSize;
        BaseMemoryChunk currentAudioFrame;
        //
        // Keeping track of the "current" timestamp for audio is tricky. We remember the offset
        // from system clock to audio clock as at was valid the last time we returned a frame to the receiver.
        // We also compute when we expect the next call from the receiver, so we can attempt to
        // do the right thing when synchronizing.
        //
        Timedelta sysClockToAudioClock;
        Timestamp nextGetAudioBufferExpected;
        //
        // We need to double-buffer between currentAudioFrame and the receiver, because the buffer
        // sizes can be different.
        //
        float[] audioBuffer;
        Timestamp audioBufferHeadTimestamp;

        bool PauseAudioPlayout = true;
        // We should _not_ drop audio frames if they are in the past, but we also don't want to
        // drop frames if we later have to insert zeros because we dropped too aggressive.
        // Especially with bursty transport like Dash this is likely to happen.
        //
        // The value of this parameter should be dynamically adjusted: it should start small and increase
        // whenever we detect that after we have dropped frames we have to insert zeros a relatively short time
        // later.
        public Timedelta audioMaxAheadMs = 66;
        // If we do need to drop frames to catch up it may be better to do a single drop of multiple
        // frames than multiple drops of a single frame. We need to cater for that.
        
        public AsyncVoicePreparer(QueueThreadSafe _inQueue) : base(_inQueue)
        {
            NoUpdateCallsNeeded();
            if (VRTConfig.Instance.Voice.maxPlayoutAhead != 0)
            {
                audioMaxAheadMs = (Timedelta)(VRTConfig.Instance.Voice.maxPlayoutAhead * 1000);
            }
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
            Debug.Log($"{Name()}: Started.");
            Start();
        }

        public override void SetSynchronizer(ISynchronizer _synchronizer)
        {
            if (_synchronizer != null && VRTConfig.Instance.Voice.ignoreSynchronizer)
            {
#if VRT_WITH_STATS
                Statistics.Output(base.Name(), "unsynchronized=1");
#endif
                _synchronizer = null;
            }
            synchronizer = _synchronizer;
        }

        public override void AsyncOnStop()
        {
            base.AsyncOnStop();
            Debug.Log($"{Name()}: Stopped");
        }

        protected override void AsyncUpdate()
        {
        }

        public Timestamp getCurrentTimestamp()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            Timestamp now = (Timestamp)sinceEpoch.TotalMilliseconds;

            return now + sysClockToAudioClock;
        }

        public override void Synchronize()
        {
            // Synchronize playout for the current frame with other preparers (if needed)
            if (synchronizer != null)
            {
                Timestamp earliestTimestamp = currentTimestamp;
                if (earliestTimestamp == 0) earliestTimestamp = InQueue._PeekTimestamp();
                Timestamp latestTimestamp = InQueue.LatestTimestamp();
                synchronizer.SetAudioTimestampRangeForCurrentFrame(Name(), earliestTimestamp, latestTimestamp);
            }
        }

        public override bool LatchFrame()
        {
            lock (this)
            {
                PauseAudioPlayout = false;
                if (currentAudioFrame != null)
                {
                    #pragma warning disable CS0162
                    if (debugBuffering) Debug.Log($"{Name()}: previous audio frame not consumed yet");
                    return true;
                }
                Timestamp bestTimestamp = 0;
                if (synchronizer != null)
                {
                    bestTimestamp = synchronizer.GetBestTimestampForCurrentFrame();
                    if (bestTimestamp != 0 && bestTimestamp < currentTimestamp)
                    {
                        if (debugBuffering || synchronizer.debugSynchronizer) Debug.Log($"{Name()}: LatchFrame: show nothing for frame {UnityEngine.Time.frameCount}: {currentTimestamp - bestTimestamp} ms in the future: bestTimestamp={bestTimestamp}, currentTimestamp={currentTimestamp}");
                        PauseAudioPlayout = true;
                        return false;
                    }
                    if (bestTimestamp != 0 && synchronizer.debugSynchronizer) Debug.Log($"{Name()}: frame {UnityEngine.Time.frameCount} bestTimestamp={bestTimestamp}, currentTimestamp={currentTimestamp}, {bestTimestamp - currentTimestamp} ms too late");
                }
                else
                {
                    // For voice, we do an extra step: we optionally set an upper limit to the latency.
                    if (VRTConfig.Instance.Voice.maxPlayoutLatency > 0)
                    {
                        if (bestTimestamp == 0)
                        {
                            bestTimestamp = currentTimestamp;
                        }
                        Timestamp latestTimestampInqueue = InQueue.LatestTimestamp();
                        if (latestTimestampInqueue > currentTimestamp + (Timedelta)(VRTConfig.Instance.Voice.maxPlayoutLatency * 1000))
                        {
#pragma warning disable CS0162
                            if (debugBuffering) Debug.Log($"{Name()}: LatchFrame: skip forward {latestTimestampInqueue - (Timedelta)(VRTConfig.Instance.Voice.maxPlayoutLatency * 1000) - bestTimestamp} ms: more than maxPlayoutLatency in input queue");
                            bestTimestamp = latestTimestampInqueue - (Timedelta)(VRTConfig.Instance.Voice.maxPlayoutLatency * 1000);
                        }
                    }
                }
                if (InQueue.IsClosed()) return false; // We are shutting down
                return _FillAudioFrame(bestTimestamp);
            }
         }

        bool _FillAudioFrame(Timestamp minTimestamp)
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
#if VRT_WITH_STATS
                    stats.statsUpdate(0, dropCount, true);
#endif
                    return false;
                }
#if VRT_AUDIO_DEBUG
                ToneGenerator.checkToneBuffer("VoicePreparer.InQueue.currentAudioFrame", currentAudioFrame.pointer, currentAudioFrame.length);
#endif
                currentTimestamp = currentAudioFrame.metadata.timestamp;
#pragma warning disable CS0162
                if (debugBuffering) Debug.Log($"{Name()}: xxxjack got audioFrame ts={currentAudioFrame.metadata.timestamp}, bytecount={currentAudioFrame.length}, queue={InQueue.Name()}");
                if (minTimestamp > 0)
                {
                    bool canSkipForward = currentTimestamp < minTimestamp - audioMaxAheadMs;
                    if (canSkipForward)
                    {
                        Timestamp queueHeadTimestamp = InQueue._PeekTimestamp();
                        canSkipForward = queueHeadTimestamp != 0 && queueHeadTimestamp < minTimestamp - audioMaxAheadMs;
#pragma warning disable CS0162
                        if (debugBuffering) Debug.Log($"{Name()}: xxxjack _FillAudioFrame: minTimestamp={minTimestamp} currentTimestamp={currentTimestamp}, delta={minTimestamp - currentTimestamp}, queueHeadTimestamp={queueHeadTimestamp} canSkipForward={canSkipForward}");
                        if (canSkipForward)
                        {
                            // There is another frame in the queue that is also earlier than minTimestamp.
                            // Drop this one.
                            if (debugBuffering) Debug.Log($"{Name()}: drop frame ts={currentTimestamp} because next ts={queueHeadTimestamp} is better");
                            dropCount++;
                            currentAudioFrame.free();
                            currentAudioFrame = null;
                            continue;
                        }
                    }

                }
                int nSamples = currentAudioFrame.length / sizeof(float);
#if VRT_WITH_STATS
                stats.statsUpdate(nSamples, dropCount, false);
#endif
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
#pragma warning disable CS0162
                if (debugBufferingMore) Debug.Log($"{Name()}: xxxjack copied {copyCount} samples, buffer empty, want {len - copyCount} more");
                return copyCount;
            }
            // If the buffer has more samples we copy what we need and keep the rest.
            System.Array.Copy(audioBuffer, 0, dst, position, len);
            int remaining = audioBuffer.Length - len;
            float[] leftOver = new float[remaining];
            System.Array.Copy(audioBuffer, len, leftOver, 0, remaining);
            audioBuffer = leftOver;
            audioBufferHeadTimestamp += (Timedelta)(1000 * len / VRTConfig.Instance.audioSampleRate);
#pragma warning disable CS0162
            if (debugBufferingMore) Debug.Log($"{Name()}: xxxjack copied all {len} samples, {remaining} left in buffer");
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
            if (currentAudioFrame == null) _FillAudioFrame(0);
            if (currentAudioFrame == null) return false;
            int availableLen = currentAudioFrame.length / sizeof(float);
            if (availableLen*sizeof(float) != currentAudioFrame.length)
            {
                Debug.LogError($"{Name()}: frame contains {currentAudioFrame.length} bytes which is not {availableLen} floats");
            }
            audioBuffer = new float[availableLen];
            System.Runtime.InteropServices.Marshal.Copy(currentAudioFrame.pointer, audioBuffer, 0, availableLen);
            audioBufferHeadTimestamp = currentAudioFrame.metadata.timestamp;
            currentAudioFrame.free();
            currentAudioFrame = null;
            if (debugBuffering) Debug.Log($"{Name()}: audioFrame consumed");
            return true;
        }

        public int GetAudioBuffer(float[] dst, int len)
        {
            lock(this)
            {
                if (debugBuffering) Debug.Log($"{Name()}: GetAudioBuffer(..., {len})");
                if (InQueue.IsClosed()) return len;
                if (PauseAudioPlayout)
                {
                    if (debugBuffering) Debug.Log($"{Name()}: xxxjack getAudioBuffer: paused, inQueue={InQueue.QueuedDuration()} ms");
                    return len;
                }
#pragma warning disable CS0162
                if (debugBufferingMore) Debug.Log($"{Name()}: xxxjack getAudioBuffer({len})");
                int position = 0;
                int oldPosition = 0;
                int curLen = 0;
                _fillIntoAudioBuffer(true);
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                Timestamp now = (Timestamp)sinceEpoch.TotalMilliseconds;
                nextGetAudioBufferExpected = now + (len * 1000 / VRTConfig.Instance.audioSampleRate);
                if (audioBufferHeadTimestamp > 0)
                {
                    sysClockToAudioClock = audioBufferHeadTimestamp - now;
                }
                while (len > 0)
                {
                    curLen = _fillFromAudioBuffer(dst, position, len);
                    // If we didn't copy anything this time we're done. And we return true if we have copied anything at all.
                    if (curLen == 0)
                    {
#pragma warning disable CS0162
                        if (debugBuffering)
                        {
                            Debug.Log($"{Name()}: xxxjack getAudioBuffer: inserted {len} zero samples from {position}, done={position != 0}, inQueue={InQueue.QueuedDuration()} ms");
                        }
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
#pragma warning disable CS0162
                if (debugBufferingMore) Debug.Log($"{Name()}: xxxjack getAudioBuffer: done=true");
#if VRT_AUDIO_DEBUG
                ToneGenerator.checkToneBuffer("VoicePreparer.GetAudioBuffer.full", dst);
#endif
                return len;
            }
        }

#if VRT_WITH_STATS
        protected class Stats : Statistics
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
                    int samplesPerFrame = 0;
                    if(statsTotalUpdates > statsNoData)
                    {
                        samplesPerFrame = (int)(statsTotalSamples / (statsTotalUpdates - statsNoData));
                    }
                    Output($"fps={statsTotalUpdates / Interval():F2}, fps_dropped={statsDrops / Interval():F2}, fps_nodata={statsNoData / Interval():F2}, samples_per_frame={samplesPerFrame}");
                    Clear();
                    statsTotalUpdates = 0;
                    statsTotalSamples = 0;
                    statsDrops = 0;
                    statsNoData = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }
}
