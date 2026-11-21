using System.Threading;
using UnityEngine;
#if !VRT_WITHOUT_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Core
{
    /// <summary>
    /// Support for helper threads: threads a worker creates itself, besides its own worker thread
    /// (cwi-dis/cwipc_unity#82, rule 4).
    ///
    /// To be replaced by cwipc_unity's own helper once cwi-dis/cwipc_unity#88 is released.
    /// </summary>
    public static class HelperThreads
    {
        /// <summary>
        /// Wait at most timeoutMs for a helper thread to end. A thread that doesn't end in time is
        /// left running (it should be a background thread), not aborted (rule 5): an error is logged
        /// and a stats record written, so threads that don't stop can be found in the stats logs.
        /// </summary>
        /// <param name="thread">The helper thread. Null, or never started, counts as stopped.</param>
        /// <param name="timeoutMs">How long to wait.</param>
        /// <param name="ownerName">Name of the worker that owns the thread, for the log and the stats record.</param>
        /// <returns>True if the thread has ended.</returns>
        public static bool Join(Thread thread, int timeoutMs, string ownerName)
        {
            if (thread == null || (thread.ThreadState & ThreadState.Unstarted) != 0) return true;
            if (thread.Join(timeoutMs)) return true;
            Debug.LogError($"{ownerName}: helper thread {thread.Name} did not stop in {timeoutMs} ms; leaving it running");
#if !VRT_WITHOUT_STATS
            Statistics.Output(ownerName, $"join_timeout=1, thread={thread.Name}, waited_ms={timeoutMs}");
#endif
            return false;
        }
    }
}
