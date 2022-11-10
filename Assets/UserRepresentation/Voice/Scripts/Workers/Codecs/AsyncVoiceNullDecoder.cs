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
            NoUpdateCallsNeeded();
            Start();
        }

        protected override void AsyncUpdate()
        {
        }
    }
}