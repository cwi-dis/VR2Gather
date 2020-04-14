using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class BaseWorker {
        public enum WorkerType { Init, Run, End };

        bool bRunning = false;
        public bool isStopped { get; private set; }
        System.Threading.Thread thread;
        public Token        token { get; set; }
        protected List<BaseWorker>  nexts =  new List<BaseWorker>();
        public bool isRunning { get { return bRunning; } }
        WorkerType type;

        public BaseWorker(WorkerType _type= WorkerType.Run) {
            type = _type;
        }

        public BaseWorker AddNext(BaseWorker _next) {
            nexts.Add(_next);
            return _next;
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

        public void Next() {
                if (type == WorkerType.Init)
                    token.currentForks = token.totalForks;
                else {
                    if (type == WorkerType.End) {
                        if (token.original != null) token = token.original;
                        token.currentForks--;
                        if (token.currentForks != 0) {
                            token = null;
                            return;
                        }
                    }
                }
                for (int i = 0; i < nexts.Count; ++i)
                    if (i > 0)
                        nexts[i].token = new Token(token);
                    else
                        nexts[i].token = token;
                token = null;
        }

//        public virtual int  available { get { return 0; } }
//        public virtual bool GetBuffer(float[] dst, int len) { return false;  }
    }
}
