using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Core
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

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