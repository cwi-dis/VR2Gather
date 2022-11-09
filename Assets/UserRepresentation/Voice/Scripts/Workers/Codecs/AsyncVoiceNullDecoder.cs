using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class AsyncVoiceNullDecoder : AsyncWorker
    {
        public AsyncVoiceNullDecoder() : base()
        {
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            Debug.Log($"{Name()}: Stopped");
        }

        protected override void Update()
        {
            base.Update();
        }
    }
}