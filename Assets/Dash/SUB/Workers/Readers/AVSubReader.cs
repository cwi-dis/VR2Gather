using UnityEngine;

namespace Workers
{
    public class AVSubReader : BaseSubReader
    {
        public enum CCCC : uint
        {
            MP4A = 0x6134706D,
            AVC1 = 0x31637661,
            AAC = 0x5f636161,
            H264 = 0x34363268
        };

        public AVSubReader(string cfg, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue, NeedsSomething needsVideo = null, NeedsSomething needsAudio = null, bool _bDropFrames = false)
         : base(cfg, null, 0, _bDropFrames)
        {
            outQueues = new QueueThreadSafe[2] { _outQueue, _out2Queue };
            int videoStream = -1;
            int audioStream = -1;
            InitDash();
            if (needsVideo())
            {
                for (int i = 0; i < streamCount; ++i)
                {
                    if (stream4CCs[i] == (uint)CCCC.AVC1 || stream4CCs[i] == (uint)CCCC.H264)
                    {
                        videoStream = i;
                        break;
                    }
                }
                if (videoStream < 0)
                {
                    Debug.LogError($"AVSubReader: could not find video in {streamCount} streams in {cfg}");
                }
            }
            if (needsAudio())
            {
                for (int i = 0; i < streamCount; ++i)
                {
                    if (stream4CCs[i] == (uint)CCCC.MP4A || stream4CCs[i] == (uint)CCCC.AAC)
                    {
                        audioStream = i;
                        break;
                    }
                }
                if (audioStream < 0)
                {
                    Debug.LogError($"AVSubReader: could not find audio in {streamCount} streams in {cfg}");
                }
            }
            streamIndexes = new int[2] { videoStream, audioStream }; // xxxjack wrong

        }
    }
}