using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTCore
{
    public class Token
    {
        public Token(int forks = 1) { totalForks = forks; }
        public Token(Token token) { original = token; currentBuffer = token.currentBuffer; currentSize = token.currentSize; currentPointcloud = token.currentPointcloud; }

        public int totalForks;
        public int currentForks;
        public BaseMemoryChunk currentPointcloud;
        public byte[] currentByteArray;
        public float[] currentFloatArray;
        public System.IntPtr currentBuffer;
        public int currentSize;
        public Token original;
        public NTPTools.NTPTime latency;

        // ---> userData
        public FrameInfo info;
        public bool isVideo;
        //        public bool needsVideo = true;
        ////

    }
}