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
        public void SetEarliestTimestampForCurrentFrame(ulong timestamp)
        {
            _Reset();
            // Record (for current frame) earliest timestamp available on all prepareres.
            // In other words: the maximum of all earliest timestamps reported.
            if (timestamp == 0) return;
            if (currentEarliestTimestamp == 0 || timestamp > currentEarliestTimestamp)
            {
                currentEarliestTimestamp = timestamp;
            }
        }
        public void SetLatestTimestampForCurrentFrame(ulong timestamp)
        {
            _Reset();
            // Record (for current frame) latest timestamp available on all prepareres.
            // In other words: the minimum of all latest timestamps reported.
            if (timestamp == 0) return;
            if (currentLatestTimestamp == 0 || timestamp < currentLatestTimestamp)
            {
                currentLatestTimestamp = timestamp;
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

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}