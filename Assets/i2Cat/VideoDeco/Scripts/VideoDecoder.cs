using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Workers {
    public unsafe class VideoDecoder : BaseWorker {
        int bufferLength;

        FFMpegDecoder decoder;
        public string url;
        AVCodec* codec;

        public VideoDecoder() : base(WorkerType.Run) {
            //decoder = new FFMpegDecoder(url);
            bufferLength = 320;
            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VideoDecoder Stopped");
        }

        float[] receiveBuffer;
        float[] receiveBuffer2;
        NTPTools.NTPTime tempTime;
        protected override void Update() {
            base.Update();
            if (token != null) {
                if (receiveBuffer == null) {
                    receiveBuffer = new float[bufferLength];
                }
                Debug.Log( $"token.currentSize {token.currentSize}");
                codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_MPEG1VIDEO);


                //decoder.Stream(token.currentByteArray, token.currentByteArray.Length, receiveBuffer);

                token.currentFloatArray = receiveBuffer;
                token.currentSize = receiveBuffer.Length;
                Next();
            }
        }
    }
}
