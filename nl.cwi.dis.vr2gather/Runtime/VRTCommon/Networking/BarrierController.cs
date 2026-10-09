using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VRT.Orchestrator;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Synchronisation point: waits until every participant has signalled "ready", then
    /// fires OnAllReady (on the session master only).
    ///
    /// Normally used through the PFB_Barrier prefab, which wires three objects in the Inspector:
    ///   Ready.OnTrigger      -> Barrier.Trigger      (Ready is a NetworkTrigger)
    ///   Barrier.OnAllReady   -> Proceed.Trigger      (Proceed is a NetworkTrigger)
    ///   Proceed.OnTrigger    -> whatever should happen on all participants
    ///
    /// Only the session master counts; on other participants Trigger() is ignored, because
    /// NetworkTrigger routes every signal through the master. Without a session (solo) a
    /// single Trigger() releases the barrier.
    ///
    /// The barrier counts Trigger() calls, not participants: each participant must signal
    /// ready exactly once. Use a Once behaviour (#350) in front of the Ready trigger if a
    /// button or code path could fire it more than once.
    ///
    /// Known limitation: the number of participants is looked up on each Trigger(). If a
    /// participant leaves the session while the others are waiting, the barrier only
    /// releases when another Trigger() arrives.
    /// </summary>
    public class BarrierController : MonoBehaviour
    {
        [Tooltip("Number of Trigger() calls required before OnAllReady fires. " +
                 "0 = number of users currently in the session.")]
        public int requiredCount = 0;

        [Tooltip("Fired on the session master when all required triggers have arrived.")]
        public UnityEvent OnAllReady;

        [Tooltip("If true, the barrier resets itself after firing OnAllReady so it can be reused (e.g. for repeated rounds).")]
        public bool resetWhenDone = false;

        int _count;
        bool _released;

        public void Trigger()
        {
            var comm = VRTOrchestratorSingleton.Comm;
            bool hasSession = comm?.SelfUser != null;
            if (hasSession && !comm.UserIsMaster) return;

            if (_released) return;
            _count++;
            int needed = GetNeededCount();
            Debug.Log($"BarrierController({name}): {_count}/{needed}");
            if (_count >= needed)
            {
                _released = true;
                OnAllReady.Invoke();
                if (resetWhenDone) ResetBarrier();
            }
        }

        public void ResetBarrier()
        {
            _count = 0;
            _released = false;
        }

        int GetNeededCount()
        {
            if (requiredCount > 0) return requiredCount;
            int n = VRTOrchestratorSingleton.Comm?.CurrentSession?.sessionUsers?.Length ?? 1;
            return Mathf.Max(n, 1);
        }

        /// <summary>
        /// Coroutine helper for code-driven barriers: signals the ready trigger, then yields
        /// until the proceed trigger fires on this participant. If either reference is null
        /// the coroutine returns immediately.
        /// </summary>
        public static IEnumerator WaitFor(NetworkTrigger ready, NetworkTrigger proceed)
        {
            if (ready == null || proceed == null) yield break;
            bool released = false;
            UnityAction onProceed = () => released = true;
            proceed.OnTrigger.AddListener(onProceed);
            ready.Trigger();
            yield return new WaitUntil(() => released);
            proceed.OnTrigger.RemoveListener(onProceed);
        }
    }
}
