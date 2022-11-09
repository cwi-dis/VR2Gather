using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class AsyncVoiceNullPreparer : AsyncPreparer
    {
        float[] circularBuffer;
        int bufferSize;
        int writePosition;
        int readPosition;


        public AsyncVoiceNullPreparer() : base(null)
        {
            Start();
        }

        public override void AsyncOnStop()
        {
            base.AsyncOnStop();
            //            if (byteArray.Length != 0) byteArray.Dispose();
            Debug.Log($"{Name()}: Stopped");
        }
        public override bool LatchFrame()
        {
            return false;
        }

        public override void Synchronize()
        {
        }

        protected override void AsyncUpdate()
        {
            base.AsyncUpdate();
        }
    }
}
