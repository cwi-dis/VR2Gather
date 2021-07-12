using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Core
{
    public class Synchronizer : MonoBehaviour
    {
        [Tooltip("Enable to get lots of log messages on Synchronizer use")]
        public bool debugSynchronizer = false;
        [Tooltip("Current preferred playout latency")]
        public long currentPreferredLatency = 0;
        [Tooltip("If nonzero enable jitterbuffer. The number is maximum ms catchup per frame (if currentPreferredLatency > minPreferredLatency). Default: as fast as possible.")]
        public int latencyCatchup = 0;
        [Tooltip("Minimum preferred playout latency")]
        public long minPreferredLatency = 0;

        int currentFrameCount = 0;  // Unity frame number we are currently working for
        ulong latestCurrentFrameTimestamp = 0; // Earliest timestamp available for this frame, for all clients
        ulong earliestNextFrameTimestamp = 0;   // Latest timestamp available for this frame, for all clients
        ulong earliestLatestFrameTimestamp = 0;   // earliest (over all clients) Latest timestamp available in the client queue
        ulong bestTimestampForCurrentFrame = 0; // Computed best timestamp for this frame

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
                latestCurrentFrameTimestamp = 0;
                earliestNextFrameTimestamp = 0;
                earliestLatestFrameTimestamp = 0;
                bestTimestampForCurrentFrame = 0;
            }
        }
        public void SetTimestampRangeForCurrentFrame(string caller, ulong currentFrameTimestamp, ulong nextFrameTimestamp, ulong latestFrameTimestamp)
        {
            _Reset();
            if (debugSynchronizer) Debug.Log($"{Name()}: SetTimestampRangeForCurrentFrame {caller}: frame={currentFrameCount}, earliest={currentFrameTimestamp}, next={nextFrameTimestamp}, latest={latestFrameTimestamp}");
            // First we record the minimum of the latest timestamp in the queue.
            // This is only used for catch-up.
            if (this.earliestLatestFrameTimestamp == 0 || earliestLatestFrameTimestamp >= latestFrameTimestamp)
            {
                this.earliestLatestFrameTimestamp = latestFrameTimestamp;
            }
            // Record (for current frame) earliest and next timestamp available on all prepareres.
            // In other words: the maximum of all earliest timestamps and minimum of all next reported.
            if (nextFrameTimestamp == 0) nextFrameTimestamp = currentFrameTimestamp;
            if (currentFrameTimestamp == 0) currentFrameTimestamp = nextFrameTimestamp;
            if (currentFrameTimestamp == 0) return;
            if (this.latestCurrentFrameTimestamp == 0 || currentFrameTimestamp > this.latestCurrentFrameTimestamp)
            {
                this.latestCurrentFrameTimestamp = currentFrameTimestamp;
            }
            if (this.earliestNextFrameTimestamp == 0 || nextFrameTimestamp < this.earliestNextFrameTimestamp)
            {
                this.earliestNextFrameTimestamp = nextFrameTimestamp;
            }
        }

        void _ComputeTimestampForCurrentFrame()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            long utcMillisForCurrentFrame = (long)sinceEpoch.TotalMilliseconds;
            //
            // We can lower the current preferred latency iff
            // - we are allowed to do catchup
            // - it is lower than the minimum preferred latency,
            // - there are ample entries in all the queues
            //
            if (latencyCatchup > 0 && currentPreferredLatency > minPreferredLatency && earliestLatestFrameTimestamp > 0)
            {
                long maxCatchup = utcMillisForCurrentFrame - (long)earliestLatestFrameTimestamp;

            }
            //
            // We now know the preferred latency, so we can compute the
            // best timestamp.
            //
            bestTimestampForCurrentFrame = (ulong)(utcMillisForCurrentFrame - currentPreferredLatency);

            //
            // If there is no next timestamp that all source have,
            // or if it is later than the best timestamp, 
            // we use the best timestamp for the current frame.
            // This may still result in some sources catching up.
            //
            if (earliestNextFrameTimestamp == 0 || bestTimestampForCurrentFrame < earliestNextFrameTimestamp)
            {
                bestTimestampForCurrentFrame = latestCurrentFrameTimestamp;
                stats.statsUpdate(false, true, false, utcMillisForCurrentFrame, bestTimestampForCurrentFrame);
                return;
            }
            // If we do catch-up we see whether the latest timestamp isn't ahead of catch-up.
            if (latencyCatchup != 0 && currentPreferredLatency != 0)
            {
                // earliestNextTimestamp may be too far in the future.
                // In that case we stick with the latest current frame timestamp
                long expectedNextTimestamp = utcMillisForCurrentFrame + currentPreferredLatency + latencyCatchup;
                if (earliestNextFrameTimestamp > (ulong)expectedNextTimestamp)
                {
                    bestTimestampForCurrentFrame = latestCurrentFrameTimestamp;
                    stats.statsUpdate(false, false, true, utcMillisForCurrentFrame, bestTimestampForCurrentFrame);
                    return;
                }
            }

            stats.statsUpdate(true, false, false, utcMillisForCurrentFrame, bestTimestampForCurrentFrame);
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

            long statsTotalPreferredLatency;
            int statsTotalCalls = 0;
            int statsTotalFreshReturn = 0;
            int statsTotalStaleReturn = 0;
            int statsTotalHoldoffReturn = 0;

            public void statsUpdate(bool freshReturn, bool staleReturn, bool holdReturn, long utcMillisForCurrentFrame, ulong timestamp)
            {
                long currentLatency = utcMillisForCurrentFrame - (long)timestamp;
                statsTotalPreferredLatency += currentLatency;
                statsTotalCalls++;
                if (freshReturn) statsTotalFreshReturn++;
                if (staleReturn) statsTotalStaleReturn++;
                if (holdReturn) statsTotalHoldoffReturn++;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalCalls / Interval():F2}, latency_ms={(int)(statsTotalPreferredLatency / statsTotalCalls)}, fresh_fps={statsTotalFreshReturn / Interval():F2}, stale_fps={statsTotalStaleReturn / Interval():F2}, holdoff_fps={statsTotalHoldoffReturn / Interval():F2}, timestamp={timestamp}");

                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPreferredLatency = 0;
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
