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
        [Tooltip("Current preferred playout latency")]
        public Timedelta currentPreferredLatency = 0;
        [Tooltip("If nonzero enable jitterbuffer. The number is maximum ms catchup per frame (if currentPreferredLatency > minPreferredLatency). Default: as fast as possible.")]
        public int latencyCatchup = 0;
        [Tooltip("Minimum preferred playout latency")]
        public Timedelta minPreferredLatency = 0;
        [Tooltip("If not all streams have data available play out unsynced (false: delay until data is available)")]
        public bool acceptDesyncOnDataUnavailable = false;

        int currentFrameCount = 0;  // Unity frame number we are currently working for
        Timestamp availableIntervalBegin = 0; // Earliest timestamp available for this frame, for all clients
        Timestamp availableIntervalEnd = 0;   // earliest (over all clients) Latest timestamp available in the client queue
        Timestamp bestTimestampForCurrentFrame = 0; // Computed best timestamp for this frame

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
                availableIntervalBegin = 0;
                availableIntervalEnd = 0;
                bestTimestampForCurrentFrame = 0;
            }
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
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                Timestamp utcMillisForCurrentFrame = (Timestamp)sinceEpoch.TotalMilliseconds;
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
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            Timestamp utcMillisForCurrentFrame = (Timestamp)sinceEpoch.TotalMilliseconds;
            // If we don't have an interval we cannot do anything
            if (availableIntervalBegin == 0)
            {
                bestTimestampForCurrentFrame = (Timestamp)(utcMillisForCurrentFrame - currentPreferredLatency);
                return;
            }
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
            // Sanity check
            if (currentPreferredLatency < minPreferredLatency)
            {
                currentPreferredLatency = minPreferredLatency;
            }
            //
            // We now know the preferred latency, so we can compute the
            // best timestamp.
            //
            bestTimestampForCurrentFrame = (Timestamp)(utcMillisForCurrentFrame - currentPreferredLatency);
            //
            // Check whether it falls into the interval, adapt if not.
            //
            if (bestTimestampForCurrentFrame < availableIntervalBegin) bestTimestampForCurrentFrame = availableIntervalBegin;
            if (bestTimestampForCurrentFrame > availableIntervalEnd) bestTimestampForCurrentFrame = availableIntervalEnd;
            //
            // And recompute preferred latency
            //
            currentPreferredLatency = utcMillisForCurrentFrame - (long)bestTimestampForCurrentFrame;

            stats.statsUpdate(true, false, utcMillisForCurrentFrame, bestTimestampForCurrentFrame);
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
