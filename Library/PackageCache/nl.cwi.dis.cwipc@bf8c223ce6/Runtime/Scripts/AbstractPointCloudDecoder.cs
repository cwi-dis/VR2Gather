using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    abstract public class AbstractPointCloudDecoder : AsyncFilter
    {
        protected AbstractPointCloudDecoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue) : base(_inQueue, _outQueue)
        {
        }
    }
}