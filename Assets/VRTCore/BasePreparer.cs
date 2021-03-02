using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTCore
{
    public class BasePreparer : BaseWorker
    {
        Synchronizer synchronizer = null;
        public BasePreparer(WorkerType _type = WorkerType.Run) : base(_type)
        {
        }

        public void SetSynchronizer(Synchronizer _synchronizer)
        {
            synchronizer = _synchronizer;
        }

        public virtual void Synchronize()
        {
            // Synchronize playout for the current frame with other preparers (if needed)
            Debug.Log($"{Name()}: xxxjack Synchronize on {UnityEngine.Time.frameCount}");
        }

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
        }

        // Update is called once per frame
        protected override void Update()
        {
            base.Update();
        }
    }
}