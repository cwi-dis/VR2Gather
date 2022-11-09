using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class AsyncEmptyDecoder : AsyncWorker
    {
        public AsyncEmptyDecoder() : base()
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