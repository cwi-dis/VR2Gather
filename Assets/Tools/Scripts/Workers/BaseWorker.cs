using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class BaseWorker {
        public enum WorkerType { Init, Run, End };

        protected bool          bRunning = false;
        public bool             isStopped { get; private set; }
        System.Threading.Thread thread;
        public bool isRunning { get { return bRunning; } }
        WorkerType type;

        public BaseWorker(WorkerType _type= WorkerType.Run) {
            type = _type;
        }

        protected void Start() {
            bRunning = true;
            thread = new System.Threading.Thread(new System.Threading.ThreadStart(_Update));
            thread.Start();
        }

        public virtual void Stop() {
            bRunning = false;
        }

        public virtual void StopAndWait() {
            Stop();
            while (!isStopped) {
                System.Threading.Thread.Sleep(10);
            }
        }

        public virtual void OnStop() { }

        void _Update() {
            try {
                isStopped = false;
                while (bRunning) {
                    Update();
                    System.Threading.Thread.Sleep(1);
                }
            }catch(System.Exception e) {
                Debug.LogWarning($"Exception catched at {this.GetType()}: {e.Message}\n{e.StackTrace}");
            }
            OnStop();
            isStopped = true;
        }
        protected virtual void Update(){ }
    }
}
