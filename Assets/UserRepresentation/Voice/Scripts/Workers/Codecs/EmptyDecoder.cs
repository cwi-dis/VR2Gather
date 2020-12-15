using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTCore;

namespace VRT.UserRepresentation.Voice
{
    public class EmptyDecoder : BaseWorker
    {
        public EmptyDecoder() : base(WorkerType.Run)
        {
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            Debug.Log("EmptyDecoder Sopped");
        }

        protected override void Update()
        {
            base.Update();
        }
    }
}