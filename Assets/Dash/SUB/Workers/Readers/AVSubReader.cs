using UnityEngine;

namespace Workers
{
    public class AVSubReader : BaseSubReader
    {
        public AVSubReader(string cfg, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue, NeedsSomething needsVideo = null, NeedsSomething needsAudio = null, bool _bDropFrames = false)
         : base(cfg, _outQueue, _out2Queue, needsVideo, needsAudio, _bDropFrames)
        {

        }
    }
}