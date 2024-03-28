using UnityEngine;
using VRT.Core;
using System.Collections.Generic;
using Cwipc;

namespace VRT.Transport.Dash
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;

    public class AsyncSubAVReader : AsyncSubReader
    {
        public enum CCCC : uint
        {
            MP4A = 0x6134706D,
            AVC1 = 0x31637661,
            AAC = 0x5f636161,
            H264 = 0x34363268
        };

        public AsyncSubAVReader(string url, string streamName, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue)
         : base(url, streamName)
        {
            lock (this)
            {
                int videoStream = -1;
                int audioStream = -1;
                if (!InitDash())
                {
                    throw new System.Exception($"{Name()}: Sub({url}) did not start playing");
                }
                // Check VideoStream
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
                    Debug.Log($"{Name()}: could not find video in {streamCount} streams in {url + streamName}");
                    Debug.LogError($"No video stream in {streamName}");
                }
                // Check AudioStream
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
                    Debug.Log($"{Name()}: could not find audio in {streamCount} streams in {url + streamName}");
                    Debug.LogError($"No audio stream in {streamName}");
                }
                perTileInfo = new TileOrMediaInfo[]
                {
                new TileOrMediaInfo()
                {
                    outQueue = _outQueue,
                    streamIndexes = new List<int> {videoStream }
                },
                new TileOrMediaInfo()
                {
                    outQueue = _out2Queue,
                    streamIndexes = new List<int> {audioStream}
                },
                };


                InitThread();
                Start();
            }
        }
    }
}