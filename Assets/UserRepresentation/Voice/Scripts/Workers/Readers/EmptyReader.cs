using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTCore;

namespace VRTVoice
{
    public class EmptyReader : BaseWorker
    {
        Coroutine coroutine;

        public EmptyReader() : base(WorkerType.Init)
        {
            Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        public override void OnStop()
        {
            base.OnStop();
            Debug.Log("EmptyReader Sopped");
        }

    }
}