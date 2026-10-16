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
        //
        // Jitter buffer. The network delivers packets irregularly, often in bursts. So playout only starts
        // once jitterBufferSamples of audio is buffered, and whenever the buffer runs empty we buffer up to
        // that level again (one short gap instead of many tiny ones). If more than maxBufferSamples piles up
        // (after a network stall) the oldest audio is dropped to get back to the jitter buffer level.
        //
        // Everything is measured in buffered audio, never by comparing the sender timestamps to our own clock:
        // the clocks of the sending and the receiving machine can easily differ by a second, and then every
        // packet would look late and be dropped. For the same reason the synchronizer does not pause or drop
        // voice: voice plays at its own pace, and only reports its timestamps so other streams can follow it.
        //
        readonly int jitterBufferSamples;
        readonly int maxBufferSamples;
        bool buffering = true;
        int samplesPerPacket;

        public AsyncVoicePreparer(QueueThreadSafe _inQueue) : base(_inQueue)
        {
            NoUpdateCallsNeeded();
            var voiceConfig = VRTConfig.Instance.VoiceConfig;
            jitterBufferSamples = (int)(voiceConfig.jitterBuffer * voiceConfig.AudioSampleRate);
            maxBufferSamples = (int)(voiceConfig.maxPlayoutLatency * voiceConfig.AudioSampleRate);
            // Leave room above the jitter buffer level, otherwise every burst would cause drops
            if (maxBufferSamples > 0 && maxBufferSamples < 2 * jitterBufferSamples)
            {
                maxBufferSamples = 2 * jitterBufferSamples;
            }
#if VRT_WITH_STATS
            stats = new Stats(Name());
            Statistics.Output(Name(), $"jitter_buffer_ms={1000 * jitterBufferSamples / voiceConfig.AudioSampleRate}, max_buffer_ms={1000 * maxBufferSamples / voiceConfig.AudioSampleRate}");
#endif
            Debug.Log($"{Name()}: Started.");
            Start();
        }

        public override void SetSynchronizer(ISynchronizer _synchronizer)
        {
            if (_synchronizer != null && VRTConfig.Instance.VoiceConfig.ignoreSynchronizer)
            {
#if VRT_WITH_STATS
                Statistics.Output(base.Name(), "unsynchronized=1");
#endif
                _synchronizer = null;
            }
            synchronizer = _synchronizer;
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
                if (InQueue.IsClosed()) return false; // We are shutting down
                _DropExcessAudio();
                if (currentAudioFrame != null)
                {
                    #pragma warning disable CS0162
                    if (debugBuffering) Debug.Log($"{Name()}: previous audio frame not consumed yet");
                    return true;
                }
                return _FillAudioFrame();
            }
        }

        // Audio available for playout, in samples. Caller must hold the lock.
        int _BufferedSamples()
        {
            int count = InQueue.Count() * samplesPerPacket;
            if (currentAudioFrame != null) count += currentAudioFrame.length / sizeof(float);
            if (audioBuffer != null) count += audioBuffer.Length;
            return count;
        }

        // After a network stall the delayed packets arrive all at once. Playing them all would keep us
        // that much behind for the rest of the session, so we skip the oldest audio down to the jitter buffer level.
        void _DropExcessAudio()
        {
            if (maxBufferSamples <= 0 || samplesPerPacket == 0) return;
            int buffered = _BufferedSamples();
            if (buffered <= maxBufferSamples) return;
            int dropCount = 0;
            while (buffered - samplesPerPacket >= jitterBufferSamples)
            {
                BaseMemoryChunk frame = currentAudioFrame ?? InQueue.TryDequeue(0);
                currentAudioFrame = null;
                if (frame == null) break;
                buffered -= frame.length / sizeof(float);
                frame.free();
                dropCount++;
            }
#pragma warning disable CS0162
            if (debugBuffering) Debug.Log($"{Name()}: dropped {dropCount} frames, {1000 * buffered / VRTConfig.Instance.VoiceConfig.AudioSampleRate} ms buffered");
#if VRT_WITH_STATS
            stats.statsDrop(dropCount);
#endif
        }

        bool _FillAudioFrame()
        {
            if (currentAudioFrame != null)
            {
                Debug.LogError($"{Name()}: _fillAudioFrame called, but currentAudioFrame is not null");
                currentAudioFrame.free();
                currentAudioFrame = null;
            }
            currentAudioFrame = InQueue.TryDequeue(0);
            if (currentAudioFrame == null)
            {
#if VRT_WITH_STATS
                stats.statsUpdate(0, true);
#endif
                return false;
            }
#if VRT_AUDIO_DEBUG
            ToneGenerator.checkToneBuffer("VoicePreparer.InQueue.currentAudioFrame", currentAudioFrame.pointer, currentAudioFrame.length);
#endif
            currentTimestamp = currentAudioFrame.metadata.timestamp;
            samplesPerPacket = currentAudioFrame.length / sizeof(float);
#pragma warning disable CS0162
            if (debugBuffering) Debug.Log($"{Name()}: xxxjack got audioFrame ts={currentAudioFrame.metadata.timestamp}, bytecount={currentAudioFrame.length}, queue={InQueue.Name()}");
#if VRT_WITH_STATS
            stats.statsUpdate(samplesPerPacket, false);
#endif
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
            audioBufferHeadTimestamp += (Timedelta)(1000 * len / VRTConfig.Instance.VoiceConfig.AudioSampleRate);
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
            if (currentAudioFrame == null) _FillAudioFrame();
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
                if (InQueue.IsClosed())
                {
                    System.Array.Clear(dst, 0, len);
                    return len;
                }
                if (buffering)
                {
                    if (currentAudioFrame == null) _FillAudioFrame();
                    int buffered = _BufferedSamples();
                    if (buffered == 0 || buffered < jitterBufferSamples)
                    {
                        if (debugBufferingMore) Debug.Log($"{Name()}: xxxjack getAudioBuffer: buffering, {buffered} samples available");
                        System.Array.Clear(dst, 0, len);
                        return len;
                    }
                    if (debugBuffering) Debug.Log($"{Name()}: buffered {buffered} samples, starting playout");
                    buffering = false;
                }
#pragma warning disable CS0162
                if (debugBufferingMore) Debug.Log($"{Name()}: xxxjack getAudioBuffer({len})");
                int position = 0;
                int oldPosition = 0;
                int curLen = 0;
                _fillIntoAudioBuffer(true);
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                Timestamp now = (Timestamp)sinceEpoch.TotalMilliseconds;
                nextGetAudioBufferExpected = now + (len * 1000 / VRTConfig.Instance.VoiceConfig.AudioSampleRate);
                if (audioBufferHeadTimestamp > 0)
                {
                    sysClockToAudioClock = audioBufferHeadTimestamp - now;
                }
                while (len > 0)
                {
                    curLen = _fillFromAudioBuffer(dst, position, len);
                    // If we didn't copy anything we ran out of audio: fill the rest with silence, and
                    // buffer up to the jitter buffer level again before continuing.
                    if (curLen == 0)
                    {
                        System.Array.Clear(dst, position, len);
                        buffering = true;
#if VRT_WITH_STATS
                        stats.statsRebuffer();
#endif
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
            double statsRebuffers;

            public void statsDrop(int dropCount)
            {
                statsDrops += dropCount;
            }

            public void statsRebuffer()
            {
                statsRebuffers++;
            }

            public void statsUpdate(int nSamples, bool noData)
            {

                statsTotalUpdates += 1;
                statsTotalSamples += nSamples;
                if (noData) statsNoData++;

                if (ShouldOutput())
                {
                    int samplesPerFrame = 0;
                    if(statsTotalUpdates > statsNoData)
                    {
                        samplesPerFrame = (int)(statsTotalSamples / (statsTotalUpdates - statsNoData));
                    }
                    Output($"fps={statsTotalUpdates / Interval():F2}, fps_dropped={statsDrops / Interval():F2}, fps_nodata={statsNoData / Interval():F2}, fps_rebuffered={statsRebuffers / Interval():F2}, samples_per_frame={samplesPerFrame}");
                    Clear();
                    statsTotalUpdates = 0;
                    statsTotalSamples = 0;
                    statsDrops = 0;
                    statsNoData = 0;
                    statsRebuffers = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }
}
