using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTCore
{
    public class BaseWorker
    {
        public enum WorkerType { Init, Run, End };

        public bool isRunning { get; private set; }
        System.Threading.Thread thread;
        WorkerType type;
        protected int loopInterval = 1; // How many milliseconds to sleep in the runloop
        protected int joinTimeout = 5000; // How many milliseconds to wait for thread completion before we abort it.
        protected const bool debugThreading = true;

        public BaseWorker(WorkerType _type = WorkerType.Run)
        {
            type = _type;
        }

        public virtual string Name()
        {
            return $"{GetType().Name}";
        }

        protected virtual void Start()
        {
            isRunning = true;
            thread = new System.Threading.Thread(new System.Threading.ThreadStart(_Update));
            thread.Name = Name();
            thread.Start();
        }

        public virtual void Stop()
        {
            isRunning = false;
        }

        public virtual void StopAndWait()
        {
            if (debugThreading) Debug.Log($"{Name()}: stopping thread");
            Stop();
            if (debugThreading) Debug.Log($"{Name()}: joining thread");
            if (!thread.Join(joinTimeout))
            {
                Debug.LogWarning($"{Name()}: thread did not stop in {joinTimeout}ms. Aborting.");
                thread.Abort();
            }
            thread.Join();
            if (debugThreading) Debug.Log($"{Name()}: thread joined");
        }

        public virtual void OnStop() { }

        void _Update()
        {
            if (debugThreading) Debug.Log($"{Name()}: thread started");
            try
            {
                while (isRunning)
                {
                    Update();
                    System.Threading.Thread.Sleep(loopInterval);
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: Update(): Exception: {e}\n{e.StackTrace}");
                Debug.LogError("Error encountered for representation of some participant. This participant will probably seem frozen from now on.");
            }
            if (debugThreading) Debug.Log($"{Name()}: thread stopping");
            try
            {
                OnStop();
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: OnStop(): Exception: {e}\n{e.StackTrace}");
                Debug.LogError("Error encountered while cleaning up");
            }
            if (debugThreading) Debug.Log($"{Name()}: thread stopped");
        }
        protected virtual void Update() { }
    }
}
