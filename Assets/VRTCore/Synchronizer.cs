using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTCore
{
    public class Synchronizer : MonoBehaviour
    {
        [Tooltip("If nonzero enable jitterbuffer. The number is maximum ms catchup per frame. Default: as fast as possible.")]
        public int catchUpMs = 0;
        long workingEpoch;  // now(ms) + this value: optimal timestamp in buffer.
        int currentFrameCount = 0;  // Unity frame number we are currently working for
        ulong currentEarliestTimestamp = 0; // Earliest timestamp available for this frame, for all clients
        ulong currentLatestTimestamp = 0;   // Latest timestamp available for this frame, for all clients
        ulong bestTimestampForCurrentFrame = 0; // Computed best timestamp for this frame

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        void _Reset()
        {
            if (UnityEngine.Time.frameCount != currentFrameCount)
            {
                currentFrameCount = UnityEngine.Time.frameCount;
                currentEarliestTimestamp = 0;
                currentLatestTimestamp = 0;
                bestTimestampForCurrentFrame = 0;
            }
        }
        public void SetTimestampRangeForCurrentFrame(ulong earliestTimestamp, ulong latestTimestamp)
        {
            _Reset();
            //Debug.Log($"{Name()}: xxxjack SetTimestampRangeForCurrentFrame: frame={currentFrameCount}, earliest={earliestTimestamp}, latest={latestTimestamp}");
            // Record (for current frame) earliest and latest timestamp available on all prepareres.
            // In other words: the maximum of all earliest timestamps and minimum of all latest reported.
            if (latestTimestamp == 0) latestTimestamp = earliestTimestamp;
            if (earliestTimestamp == 0) earliestTimestamp = latestTimestamp;
            if (earliestTimestamp == 0) return;
            if (currentEarliestTimestamp == 0 || earliestTimestamp > currentEarliestTimestamp)
            {
                currentEarliestTimestamp = earliestTimestamp;
            }
            if (currentLatestTimestamp == 0 || latestTimestamp < currentLatestTimestamp)
            {
                currentLatestTimestamp = latestTimestamp;
            }
        }
 
        void _ComputeTimestampForCurrentFrame()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            long utcMillisForCurrentFrame = (long)sinceEpoch.TotalMilliseconds;
            // If there is no latest timestamp, or it is old anyway, we use the earliest timestamp for this frame.
            if (currentLatestTimestamp <= currentEarliestTimestamp)
            {
                bestTimestampForCurrentFrame = currentEarliestTimestamp;
                stats.statsUpdate(false, true, false, workingEpoch, bestTimestampForCurrentFrame);
                return;
            }
            // If we do catch-up we see whether the latest timestamp isn't ahead of catch-up.
            if (catchUpMs != 0 && workingEpoch != 0)
            {
                // currentLatestTimestamp may be too far in the future. 
                long expectedNextTimestamp = utcMillisForCurrentFrame + workingEpoch + catchUpMs;
                if (currentLatestTimestamp > (ulong)expectedNextTimestamp)
                {
                    bestTimestampForCurrentFrame = currentEarliestTimestamp;
                    stats.statsUpdate(false, false, true, workingEpoch, bestTimestampForCurrentFrame);
                    return;
                }
            }
            // We are going to show new data in the current frame. Update our epoch.
            workingEpoch = utcMillisForCurrentFrame - (long)currentLatestTimestamp;
            bestTimestampForCurrentFrame = currentLatestTimestamp;
            stats.statsUpdate(true, false, false, workingEpoch, bestTimestampForCurrentFrame);
        }
        public ulong GetBestTimestampForCurrentFrame()
        {
            _Reset();
            if (bestTimestampForCurrentFrame == 0) _ComputeTimestampForCurrentFrame();
            return bestTimestampForCurrentFrame;
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log($"{Name()}: xxxjack synchronizer started");
            stats = new Stats(Name());
        }

        // Update is called once per frame
        void Update()
        {
        }
        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalEpochOffset;
            double statsTotalCalls = 0;
            double statsTotalFreshReturn = 0;
            double statsTotalStaleReturn = 0;
            double statsTotalHoldoffReturn = 0;

            public void statsUpdate(bool freshReturn, bool staleReturn, bool holdReturn, long epochOffset, ulong timestamp)
            {
                statsTotalEpochOffset += epochOffset;
                statsTotalCalls++;
                if (freshReturn) statsTotalFreshReturn++;
                if (staleReturn) statsTotalStaleReturn++;
                if (holdReturn) statsTotalHoldoffReturn++;
                
                if (ShouldOutput())
                {
                    Output($"fps={statsTotalCalls / Interval():F2}, fresh_fps={statsTotalFreshReturn / Interval():F2}, stale_fps={statsTotalStaleReturn / Interval():F2}, holdoff_fps={statsTotalHoldoffReturn / Interval():F2}, latency_ms={(int)(statsTotalEpochOffset / Interval())}, timestamp={timestamp}" );
                    
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalEpochOffset = 0;
                    statsTotalCalls = 0;
                    statsTotalFreshReturn = 0;
                    statsTotalHoldoffReturn = 0;
                    statsTotalStaleReturn = 0;
                }
            }
        }

        protected Stats stats;
    }
}
