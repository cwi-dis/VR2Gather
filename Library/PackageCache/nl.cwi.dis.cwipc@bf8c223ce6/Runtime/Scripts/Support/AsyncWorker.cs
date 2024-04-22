using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    /// <summary>
    /// Abstract base class for an asynchronous worker.
    /// The intention is an asynchronous worker is somewhat like a MonoBehaviour, in that its AsyncUpdate() method
    /// is regularly called to see if there is work to do, but it is implemented asynchronously using a separate
    /// thread.
    /// Usually an AsyncWorker will communicate with its source and/or sink through QueueThreadSafe objects, so the
    /// various AsyncWorkers are decoupled.
    /// </summary>
    public abstract class AsyncWorker
    {
        /// <summary>
        /// True if the worker is running.
        /// </summary>
        public bool isRunning { get; private set; }
        System.Threading.Thread thread;
        protected int loopInterval = 1; // How many milliseconds to sleep in the runloop
        protected int joinTimeout = 5000; // How many milliseconds to wait for thread completion before we abort it.
        protected const bool debugThreading = true;

        public AsyncWorker()
        {
        }

        /// <summary>
        /// Called by subclasses to indicate the subclass is not interested in calls to AsyncUpdate(), presumably
        /// because it is fully implemented using callbacks or something similar.
        /// </summary>
        protected void NoUpdateCallsNeeded()
        {
            loopInterval = 100;
            Debug.Log($"xxxjack {Name()}: NoUpdateCallsNeeded()");
        }

        public virtual string Name()
        {
            return $"{GetType().Name}";
        }

        /// <summary>
        /// Start the runner thread. Usually called from the subclass constructor, but for some implementations may
        /// be called later.
        /// </summary>
        protected virtual void Start()
        {
            if (debugThreading) Debug.Log($"{Name()}: starting thread");
            isRunning = true;
            thread = new System.Threading.Thread(new System.Threading.ThreadStart(AsyncRunner));
            thread.Name = Name();
            thread.Start();
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public virtual void Stop()
        {
            if (debugThreading) Debug.Log($"{Name()}: stopping thread");
            isRunning = false;
        }

        /// <summary>
        /// Stop the thread and wait for it to clean up and exit.
        /// </summary>
        public virtual void StopAndWait()
        {
            if (thread == null)
            {
                Debug.LogWarning($"{Name()}: No thread");
                return;
            }
            Stop();
            if (debugThreading) Debug.Log($"{Name()}: joining thread");
            if (!thread.Join(joinTimeout))
            {
                Debug.LogWarning($"{Name()}: thread did not stop in {joinTimeout}ms. Aborting.");
                thread.Abort();
            }
            if (!thread.Join(joinTimeout))
            {
                // xxxjack a stack trace would be nice, but apparently mono doesn't support GetStackTrace...
                Debug.LogError($"{Name()}: thread did not stop and could not be aborted. Please restart application.");
                return;
            }
            if (debugThreading) Debug.Log($"{Name()}: thread stopped and joined");
        }

        /// <summary>
        /// Called in the worker thread when the thread is about to stop. Should clean up any resources in use.
        /// </summary>
        public virtual void AsyncOnStop()
        {
            if (debugThreading) Debug.Log($"{Name()}: thread stopped");
        }

        private void AsyncRunner()
        {
            if (debugThreading) Debug.Log($"{Name()}: thread started");
            try
            {
                while (isRunning)
                {
                    AsyncUpdate();
                    System.Threading.Thread.Sleep(loopInterval);
                }
            }
#pragma warning disable CS0168
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                throw;
#else
                Debug.Log($"{Name()}: Update(): Exception: {e}\n{e.StackTrace}");
                Debug.LogError("Error encountered for representation of some participant. This participant will probably seem frozen from now on.");
#endif
            }
            if (debugThreading) Debug.Log($"{Name()}: thread preparing to stop");
            try
            {
                AsyncOnStop();
            }
#pragma warning disable CS0168
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                throw;
#else
                Debug.Log($"{Name()}: AsyncOnStop(): Exception: {e}\n{e.StackTrace}");
                Debug.LogError($"Error encountered while cleaning up {Name()}");
#endif
            }
            if (debugThreading) Debug.Log($"{Name()}: thread stopped");
        }

        /// <summary>
        /// Method that is called periodically. Must be overridden by subclass.
        /// </summary>
        protected abstract void AsyncUpdate();
    }
}
