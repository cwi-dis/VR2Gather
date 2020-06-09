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
        const int loopInterval = 1; // How many milliseconds to sleep in the runloop
        const int joinTimeout = 5000; // How many milliseconds to wait for thread completion before we abort it.

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
            Stop();
            if (!thread.Join(joinTimeout))
            {
                Debug.LogWarning($"{Name()}: thread did not stop. Aborting.");
                thread.Abort();
            }
            thread.Join();
            Debug.Log($"{Name()}: thread joined");
        }

        public virtual void OnStop() { }

        void _Update() {
            Debug.Log($"{Name()}: thread started");
            try {
                while (bRunning) {
                    Update();
                    System.Threading.Thread.Sleep(loopInterval);
                }
            }catch(System.Exception e) {
                Debug.LogError($"{Name()}: Exception: {e.Message}\n{e.StackTrace}");
            }
            OnStop();
            Debug.Log($"{Name()}: thread stopped");
        }
        protected virtual void Update(){ }
    }
}
