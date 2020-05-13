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
        const int joinTimeout = 1000; // How many milliseconds to wait for thread completion before we abort it.

        public BaseWorker(WorkerType _type= WorkerType.Run) {
            type = _type;
        }

        protected virtual void Start() {
            bRunning = true;
            thread = new System.Threading.Thread(new System.Threading.ThreadStart(_Update));
            thread.Start();
        }

        public virtual void Stop() {
            bRunning = false;
        }

        public virtual void StopAndWait() {
            Stop();
            if (!thread.Join(joinTimeout))
            {
                Debug.LogWarning($"BaseWorker {this.GetType().Name}: thread did not stop. Aborting.");
                thread.Abort();
            }
            thread.Join();
            Debug.Log($"BaseWorker {this.GetType().Name}: thread joined");
        }

        public virtual void OnStop() { }

        void _Update() {
            Debug.Log($"BaseWorker {this.GetType().Name}: thread started");
            try {
                while (bRunning) {
                    Update();
                    System.Threading.Thread.Sleep(loopInterval);
                }
            }catch(System.Exception e) {
                Debug.LogWarning($"BaseWorker {this.GetType().Name}: Exception: {e.Message}\n{e.StackTrace}");
            }
            OnStop();
        }
        protected virtual void Update(){ }
    }
}
