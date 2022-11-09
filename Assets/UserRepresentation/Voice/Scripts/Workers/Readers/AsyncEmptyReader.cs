using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class AsyncEmptyReader : AsyncWorker
    {
 
        public AsyncEmptyReader() : base(0)
        {
            Start();
        }
    }
}