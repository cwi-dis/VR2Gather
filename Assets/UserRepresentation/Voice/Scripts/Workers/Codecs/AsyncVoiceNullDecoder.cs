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

        public override void AsyncOnStop()
        {
            base.AsyncOnStop();
            Debug.Log($"{Name()}: Stopped");
        }

        protected override void AsyncUpdate()
        {
            base.AsyncUpdate();
        }
    }
}