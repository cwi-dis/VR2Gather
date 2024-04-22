using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    /// <summary>
    /// Base class for object implementing asynchronous reader, through AsyncWorker.
    /// </summary>
    public abstract class AsyncReader : AsyncWorker
    {
        /// <summary>
        /// Informs the reader about which stream time corresponds to which local system (wall clock) time.
        /// After this call, timestamps of frames produces by this reader will be in local system time.
        /// </summary>
        /// <param name="_clockCorrespondence">Mapping between timestamps</param>
        public virtual void SetSyncInfo(SyncConfig.ClockCorrespondence _clockCorrespondence)
        {
            if (_clockCorrespondence.streamClockTime != _clockCorrespondence.wallClockTime)
            {
                Debug.LogWarning($"{Name()}: SetSyncInfo({_clockCorrespondence.wallClockTime}={_clockCorrespondence.streamClockTime}) called but not implemented in this reader");
            }
        }
    }
}
