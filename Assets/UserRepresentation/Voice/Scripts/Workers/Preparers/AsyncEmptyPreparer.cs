using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class AsyncEmptyPreparer : AsyncPreparer
    {
        float[] circularBuffer;
        int bufferSize;
        int writePosition;
        int readPosition;


        public AsyncEmptyPreparer() : base(null)
        {
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            //            if (byteArray.Length != 0) byteArray.Dispose();
            Debug.Log("EmptyPreparer Sopped");
        }
        public override bool LatchFrame()
        {
            return false;
        }

        public override void Synchronize()
        {
        }

        protected override void Update()
        {
            base.Update();
        }
    }
}
