using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.UserRepresentation.Voice
{
    public class AsyncEmptyReader : AsyncWorker
    {
 
        public AsyncEmptyReader() : base()
        {
            NoUpdateCallsNeeded();
            Start();
        }

        protected override void AsyncUpdate()
        {
        }

    }
}