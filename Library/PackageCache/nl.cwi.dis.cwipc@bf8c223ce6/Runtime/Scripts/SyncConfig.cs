using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    /// <summary>
    /// Structure specifying mapping between two clocks: the wall clock time of the current system and the clock
    /// time of a specific stream. This structure is used to adjust the timestamps on incomging streams to the local
    /// system clock, to make timestamps of different streams comparable (and comparable to local system time).
    /// </summary>
    [Serializable]
    public struct SyncConfig
    {
        [Serializable]
        public struct ClockCorrespondence
        {
            public Timestamp wallClockTime;
            public Timestamp streamClockTime;
        };
        public ClockCorrespondence visuals;
        public ClockCorrespondence audio;
    };
}