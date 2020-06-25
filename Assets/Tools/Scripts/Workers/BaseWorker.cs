using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class BaseWorker {
        public enum WorkerType { Init, Run, End };

        protected bool          bRunning = false;
        System.Threading.Thread thread;
        public bool isRunning { get { return bRunning; } }
        WorkerType type;
        protected int loopInterval = 1; // How many milliseconds to sleep in the runloop
        protected int joinTimeout = 5000; // How many milliseconds to wait for thread completion before we abort it.
        protected const bool debugThreading = true;

        public BaseWorker(WorkerType _type= WorkerType.Run) {
            type = _type;
        }

        public virtual string Name()
        {
            return $"{this.GetType().Name}";
        }

        protected virtual void Start() {
            bRunning = true;
            thread = new System.Threading.Thread(new System.Threading.ThreadStart(_Update));
            thread.Name = Name();
            thread.Start();
        }

        public virtual void Stop() {
            bRunning = false;
        }

        public virtual void StopAndWait() {
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

        void _Update() {
            if (debugThreading) Debug.Log($"{Name()}: thread started");
            try
            {
                while (bRunning)
                {
                    Update();
                    System.Threading.Thread.Sleep(loopInterval);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{Name()}: Update(): Exception: {e}\n{e.StackTrace}");
            }
            if (debugThreading) Debug.Log($"{Name()}: thread stopping");
            try
            {
                OnStop();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{Name()}: OnStop(): Exception: {e}\n{e.StackTrace}");
            }
            if (debugThreading) Debug.Log($"{Name()}: thread stopped");
        }
        protected virtual void Update(){ }
    }
}
