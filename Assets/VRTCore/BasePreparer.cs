using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTCore
{
    public abstract class BasePreparer : BaseWorker
    {
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        protected Synchronizer synchronizer = null;

        public BasePreparer(WorkerType _type = WorkerType.Run) : base(_type)
        {
        }
        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public void SetSynchronizer(Synchronizer _synchronizer)
        {
            synchronizer = _synchronizer;
        }

        public abstract void Synchronize();

        public abstract void LatchFrame();
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