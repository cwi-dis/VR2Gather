using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRTCore
{
    [Serializable]
    public struct SyncConfig
    {
        [Serializable]
        public struct ClockCorrespondence
        {
            public long wallClockTime;
            public long streamClockTime;
        };
        public ClockCorrespondence visuals;
        public ClockCorrespondence audio;
    };
}