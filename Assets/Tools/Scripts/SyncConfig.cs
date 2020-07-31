using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SyncConfig
{
    [Serializable]
    public struct ClockCorrespondence
    {
        public System.Int64 wallClockTime;
        public System.Int64 streamClockTime;
    };
    public ClockCorrespondence visuals;
    public ClockCorrespondence audio;
};
