using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Core
{
    public abstract class AsyncReader : AsyncWorker
    {
        public virtual void SetSyncInfo(SyncConfig.ClockCorrespondence _clockCorrespondence)
        {
            if (_clockCorrespondence.streamClockTime != _clockCorrespondence.wallClockTime)
            {
                Debug.LogWarning($"{Name()}: SetSyncInfo({_clockCorrespondence.wallClockTime}={_clockCorrespondence.streamClockTime}) called but not implemented in this reader");
            }
        }
    }
}
