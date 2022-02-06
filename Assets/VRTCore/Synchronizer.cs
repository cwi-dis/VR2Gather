using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Core
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class Synchronizer : MonoBehaviour
    {
        
        [Tooltip("Enable to get lots of log messages on Synchronizer use")]
        public bool debugSynchronizer = false;
        [Tooltip("Enable to get log messages on jitter buffer adaptations")]
        public bool debugJitterBuffer = false;

        [Tooltip("Current preferred playout latency (ms)")]
        public Timedelta currentLatency = 0;

        [Tooltip("Minimum preferred playout latency (ms)")]
        public Timedelta minLatency = 0;
        [Tooltip("Maximum preferred playout latency (ms), reset to minLatency if we reach this")]
        public Timedelta maxLatency = 0;

        [Tooltip("Limit by how much we decrease preferred latency")]
        public int latencyMaxDecrease = 0;
        [Tooltip("Limit by how much we increase preferred latency")]
        public int latencyMaxIncrease = 0;

        [Tooltip("If not all streams have data available play out unsynced (false: delay until data is available)")]
        public bool acceptDesyncOnDataUnavailable = false;
        [Tooltip("Set to true once a correctly synchronized frame has been produced")]
        public bool stableStreamsDetected = false;

        int currentFrameCount = 0;  // Unity frame number we are currently working for
        Timestamp utcMillisForCurrentFrame = 0; // timestamp for the frame we are currently working for
        bool currentFrameDesync = false; // Set to true if we detect we can't do the right thing for the current frame
        Timestamp availableIntervalBegin = 0; // Earliest timestamp available for this frame, for all clients
        Timestamp availableIntervalEnd = 0;   // earliest (over all clients) Latest timestamp available in the client queue
        Timestamp bestTimestampForCurrentFrame = 0; // Computed best timestamp for this frame

        public class TimestampRange
        {
            public string caller;
            public Timestamp earliestFrameTimestamp;
            public Timestamp latestFrameTimestamp;
        };
        TimestampRange audioTimestampRange;

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        void _Reset()
        {
            if (Time.frameCount != currentFrameCount)
            {
                currentFrameCount = Time.frameCount;
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                utcMillisForCurrentFrame = (Timestamp)sinceEpoch.TotalMilliseconds;
                currentFrameDesync = false;
                availableIntervalBegin = 0;
                availableIntervalEnd = 0;
                bestTimestampForCurrentFrame = 0;
                audioTimestampRange = null;
            }
        }

        public void SetAudioTimestampRangeForCurrentFrame(string caller, Timestamp earliestFrameTimestamp, Timestamp latestFrameTimestamp)
        {
            if (audioTimestampRange != null)
            {
                Debug.LogError($"{Name()}: {caller}: Duplicate SetAudioTimestampRangeForCurrentFrame call");
            }
            audioTimestampRange = new TimestampRange()
            {
                caller = caller,
                earliestFrameTimestamp = earliestFrameTimestamp,
                latestFrameTimestamp = latestFrameTimestamp
            };
        }

        public void SetTimestampRangeForCurrentFrame(string caller, Timestamp earliestFrameTimestamp, Timestamp latestFrameTimestamp)
        {
            _Reset();
            if (debugSynchronizer) Debug.Log($"{Name()}: SetTimestampRangeForCurrentFrame {caller}: frame={currentFrameCount}, earliest={earliestFrameTimestamp}, latest={latestFrameTimestamp}");
            //
            // If there is nothing in the queue (for our caller) the latest is the earliest. Which may be 0, if there is nothing.
            // If there is no earliest then the lastest is used as the earliest.
            //
            if (latestFrameTimestamp == 0) latestFrameTimestamp = earliestFrameTimestamp;
            if (earliestFrameTimestamp == 0) earliestFrameTimestamp = latestFrameTimestamp;
            //
            // Now either both are zero or both are non-zero
            //
            if (latestFrameTimestamp == 0 || earliestFrameTimestamp == 0)
            {
                currentFrameDesync = true;
                if (latestFrameTimestamp != 0 || earliestFrameTimestamp != 0) Debug.LogError($"{Name()}: programmer error in timestamp range [{earliestFrameTimestamp}..{latestFrameTimestamp}]");
                return;
            }
            //
            // If we have no interval yet we use this one.
            //
            if (availableIntervalBegin == 0 || availableIntervalEnd == 0)
            {
                if (availableIntervalBegin != 0 || availableIntervalEnd != 0) Debug.LogError($"{Name()}: programmer Error in previous timestamp range");
                availableIntervalBegin = earliestFrameTimestamp;
                availableIntervalEnd = latestFrameTimestamp;
                return;
            }
            //
            // Check whether the ranges are disjunct.
            //
            bool rangeTooNew = earliestFrameTimestamp > availableIntervalEnd;
            bool rangeTooOld = latestFrameTimestamp < availableIntervalBegin;
            if (rangeTooNew || rangeTooOld)
            {
                currentFrameDesync = true;
                string problem = rangeTooNew ? "new" : "old";
                Debug.Log($"{Name()}: {caller}: too {problem}: timestamp range [{earliestFrameTimestamp-utcMillisForCurrentFrame}..{latestFrameTimestamp - utcMillisForCurrentFrame}], allowed range [{availableIntervalBegin - utcMillisForCurrentFrame}..{availableIntervalEnd - utcMillisForCurrentFrame}]");
                // xxxjack decide which one to keep: the earliest or the latest range....
                // keeping the latest range should cause "best progress".
                // keeping the earliest should cause "quickest sync".
                // For now we do nothing, just keeping the old one, which is random.
                if (rangeTooOld && !acceptDesyncOnDataUnavailable)
                {
                    // This pipeline hasn't received the correct packets yet.
                    // We make all other streams wait for this stream.
                    availableIntervalBegin = earliestFrameTimestamp;
                    availableIntervalEnd = latestFrameTimestamp;
                }
                return;
            }
            //
            // There is overlap. Update the range.
            //
            if (earliestFrameTimestamp > availableIntervalBegin) availableIntervalBegin = earliestFrameTimestamp;
            if (latestFrameTimestamp < availableIntervalEnd) availableIntervalEnd = latestFrameTimestamp;
        }

        void _ComputeTimestampForCurrentFrame()
        {
            // First we have to add the audio to the computations.
            if (audioTimestampRange != null)
            {
                SetTimestampRangeForCurrentFrame(audioTimestampRange.caller, audioTimestampRange.earliestFrameTimestamp, audioTimestampRange.latestFrameTimestamp);
                audioTimestampRange = null;
            }
            // If we don't have an interval we cannot do anything
            if (availableIntervalBegin == 0)
            {
                currentFrameDesync = true;
                bestTimestampForCurrentFrame = (Timestamp)(utcMillisForCurrentFrame - currentLatency);
                return;
            }
            // If we managed to find a correctly synced timestamp for this frame
            // we check whether it is the first time we managed to do so.
            if (!currentFrameDesync && !stableStreamsDetected)
            {
                Debug.Log($"{Name()}: First synchronized frame produced");
                Stats.Output(Name(), "synchronised=1");
                stableStreamsDetected = true;
            }
            _AdjustParameters();

            stats.statsUpdate(true, false, utcMillisForCurrentFrame, bestTimestampForCurrentFrame);
        }

        void _AdjustParameters()
        {
#if xxxjack_outdated
            //
            // We can lower the current preferred latency iff
            // - we are allowed to do catchup
            // - it is lower than the minimum preferred latency,
            // - there are ample entries in all the queues
            //
            if (latencyCatchup > 0 && currentPreferredLatency > minPreferredLatency && availableIntervalEnd > 0)
            {
                Timedelta maxCatchup = utcMillisForCurrentFrame - availableIntervalEnd;
                Timedelta curCatchup = maxCatchup;
                if (curCatchup > latencyCatchup)
                {
                    curCatchup = latencyCatchup;
                }
                if (curCatchup > 0 && currentPreferredLatency - curCatchup >= minPreferredLatency)
                {
                    currentPreferredLatency -= curCatchup;
                    if (debugSynchronizer) Debug.Log($"{Name()}: catching up {curCatchup} ms, currentPreferredLatency={currentPreferredLatency}, maxCatchup={maxCatchup}");
                }
            }
#else
#endif
            //
            // Check that the latency is within the acceptable range
            //
            if (currentLatency < minLatency)
            {
                currentLatency = minLatency;
            }
            if (maxLatency > 0 && currentLatency > maxLatency)
            {
                currentLatency = minLatency;
                stableStreamsDetected = false;
                Stats.Output(Name(), "synchronised=0");
                Debug.LogWarning($"{Name()}: lost synchronization");
            }
            //
            // We now know the preferred latency, so we can compute the
            // best timestamp.
            //
            Timedelta latencyAdjustment = 0;
            bestTimestampForCurrentFrame = (Timestamp)(utcMillisForCurrentFrame - currentLatency);
            //
            // Check whether it falls into the interval, adapt if not.
            //
            if (bestTimestampForCurrentFrame < availableIntervalBegin)
            {
                // Our best timestamp falls before the begin of the available range.
                // We need to forward the best timestamp, so we should also decrease the currentPreferredLatency.
                if (debugJitterBuffer)
                {
                    Debug.Log($"{Name()}: jitterBuffer: need to decrease latency, interval too new");
                }
                latencyAdjustment = bestTimestampForCurrentFrame - availableIntervalBegin;
                // We return the oldest available timestamp in the range.
                bestTimestampForCurrentFrame = availableIntervalBegin;
            }
            else
            if (bestTimestampForCurrentFrame > availableIntervalEnd)
            {
                // Our best timestamp falls after the end of the available range.
                // So we need to decrease the best timestamp, which means we should increase the currentPreferredLatency.
                if (debugJitterBuffer)
                {
                    Debug.Log($"{Name()}: jitterBuffer: need to increase latency, interval too old");
                }
                latencyAdjustment = bestTimestampForCurrentFrame - availableIntervalEnd;
                // We return the newest frame in the range.
                bestTimestampForCurrentFrame = availableIntervalEnd;
            }
            else
            {
                // Our best timestamp falls within the available range, so we return it as-is.
                // xxxjack we should do optional catchup here by setting latencyAdjustment to a negative value, if there
                // is enough data in the range.
            }
            if (latencyAdjustment != 0)
            {
                // We limit latencyAdjustment
                if (latencyAdjustment < -latencyMaxDecrease)
                {
                    latencyAdjustment = -latencyMaxDecrease;
                    if (debugJitterBuffer)
                    {
                        Debug.Log($"{Name()}: jitterBuffer: limited latency decrease: {latencyAdjustment}");
                    }
                }
                if (latencyAdjustment > latencyMaxIncrease)
                {
                    latencyAdjustment = latencyMaxIncrease;
                    if (debugJitterBuffer)
                    {
                        Debug.Log($"{Name()}: jitterBuffer: limited latency increase: {latencyAdjustment}");
                    }
                }
                currentLatency += latencyAdjustment;
                if (debugJitterBuffer)
                {
                    Debug.Log($"{Name()}: jitterBuffer: currentPreferredLatency={currentLatency}, latencyAdjustment={latencyAdjustment}");
                }
            }
        }

        public Timestamp GetBestTimestampForCurrentFrame()
        {
            _Reset();
            if (bestTimestampForCurrentFrame == 0) _ComputeTimestampForCurrentFrame();
            return bestTimestampForCurrentFrame;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (debugSynchronizer) Debug.Log($"{Name()}: Synchronizer started");
            stats = new Stats(Name());
        }

        // Update is called once per frame
        void Update()
        {
        }
        protected class Stats : BaseStats
        {
            public Stats(string name) : base(name) { }

            Timedelta statsTotalPreferredLatency;
            int statsTotalCalls = 0;
            int statsTotalFreshReturn = 0;
            int statsTotalStaleReturn = 0;
            
            public void statsUpdate(bool freshReturn, bool staleReturn, Timestamp utcMillisForCurrentFrame, Timestamp timestamp)
            {
                Timedelta currentLatency = utcMillisForCurrentFrame - timestamp;
                statsTotalPreferredLatency += currentLatency;
                statsTotalCalls++;
                if (freshReturn) statsTotalFreshReturn++;
                if (staleReturn) statsTotalStaleReturn++;
                
                if (ShouldOutput())
                {
                    Output($"fps={statsTotalCalls / Interval():F2}, latency_ms={(int)(statsTotalPreferredLatency / statsTotalCalls)}, fps_fresh={statsTotalFreshReturn / Interval():F2}, fps_stale={statsTotalStaleReturn / Interval():F2}, timestamp={timestamp}");

                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPreferredLatency = 0;
                    statsTotalCalls = 0;
                    statsTotalFreshReturn = 0;
                    statsTotalStaleReturn = 0;
                }
            }
        }

        protected Stats stats;
    }
}
