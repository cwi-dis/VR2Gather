using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Core
{
    public abstract class BasePreparer : BaseWorker
    {
        protected Synchronizer synchronizer = null;

        public BasePreparer(WorkerType _type = WorkerType.Run) : base(_type)
        {
        }

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public void SetSynchronizer(Synchronizer _synchronizer)
        {
            synchronizer = _synchronizer;
            Debug.Log($"{Name()}: xxxjack SetSynchronizer({synchronizer}, {synchronizer?.Name()})");
        }

        public abstract void Synchronize();

        public abstract bool LatchFrame();
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