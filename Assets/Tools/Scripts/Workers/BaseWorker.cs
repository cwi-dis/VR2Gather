using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class BaseWorker {
        public enum WorkerType { Init, Run, End };

        bool                    bRunning = false;
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

        public void Stop() {
            bRunning = false;
        }

        public void StopAndWait() {
            Stop();
            while (!isStopped) {
                System.Threading.Thread.Sleep(10);
            }
        }

        public virtual void OnStop() { }

        void _Update() {
            isStopped = false;
            while (bRunning) {
                Update();
                System.Threading.Thread.Sleep(1);
            }
            /*
            // Wait to stop.
            bool waitNext = false;
            do {
                for (int i = 0; i < nexts.Count; ++i)
                    if (nexts[i].type != WorkerType.Init && nexts[i].bRunning)
                        waitNext = true;
            } while (waitNext);
            */
            OnStop();
            isStopped = true;
        }
        protected virtual void Update(){ }
//        public virtual int  available { get { return 0; } }
//        public virtual bool GetBuffer(float[] dst, int len) { return false;  }
    }
}
