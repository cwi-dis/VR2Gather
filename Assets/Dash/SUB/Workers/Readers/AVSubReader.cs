using UnityEngine;

namespace Workers
{
    public class AVSubReader : BaseSubReader
    {
        public AVSubReader(string cfg, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue, NeedsSomething needsVideo = null, NeedsSomething needsAudio = null, bool _bDropFrames = false)
         : base(cfg, needsVideo, needsAudio, _bDropFrames)
        {
            outQueues = new QueueThreadSafe[2] { _outQueue, _out2Queue };
            streamIndexes = new int[2] { 0, 1 }; // xxxjack wrong

        }
    }
}