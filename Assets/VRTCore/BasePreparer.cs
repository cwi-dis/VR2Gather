using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTCore
{
    public class BasePreparer : BaseWorker
    {
        Synchronizer synchroniser = null;
        public BasePreparer(WorkerType _type = WorkerType.Run) : base(_type)
        {
        }

        public void SetSynchroniser(Synchronizer _synchroniser)
        {
            synchroniser = _synchroniser;
        }

        public virtual void Synchronize()
        {
            // Synchronize playout for the current frame with other preparers (if needed)
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