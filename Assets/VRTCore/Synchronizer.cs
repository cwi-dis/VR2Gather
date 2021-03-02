using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTCore
{
    public class Synchronizer : MonoBehaviour
    {
        int currentFrameCount = 0;
        ulong currentEarliestTimestamp = 0;
        ulong currentLatestTimestamp = 0;

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
 
        public ulong GetBestTimestampForCurrentFrame()
        {
            _Reset();
            if (currentLatestTimestamp != 0) return currentLatestTimestamp;
            return currentEarliestTimestamp;
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log($"{Name()}: xxxjack synchronizer started");
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}