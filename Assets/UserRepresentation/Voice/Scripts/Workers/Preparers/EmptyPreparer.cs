using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTCore;

namespace Voice
{
    public class EmptyPreparer : BaseWorker
    {
        float[] circularBuffer;
        int bufferSize;
        int writePosition;
        int readPosition;


        public EmptyPreparer(WorkerType _type = WorkerType.Run) : base(_type)
        {
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            //            if (byteArray.Length != 0) byteArray.Dispose();
            Debug.Log("EmptyPreparer Sopped");
        }

        protected override void Update()
        {
            base.Update();
        }
    }
}
