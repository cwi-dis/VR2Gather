using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using System;
using VRT.Core;
using Cwipc;

namespace VRT.Video
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;

    public unsafe class AsyncVideoEncoder : AsyncWorker
    {

        public struct Setup
        {
            public AVCodecID codec;
            public int width;
            public int height;
            public int fps;
            public int bitrate;
        };

        public string url;
        AVCodec* codecVideo;
        AVCodecContext* codecVideo_ctx;
        AVPacket* videoPacket;
        AVFrame* videoFrame;

        SwrContext* swrAudioFilterContext;

        public int Width { get; private set; }
        public int Height { get; private set; }

        //        public System.IntPtr videoData { get; private set; }
        public int videoDataSize { get; private set; }
        QueueThreadSafe inVideoQueue;
        QueueThreadSafe inAudioQueue;
        QueueThreadSafe outVideoQueue;
        QueueThreadSafe outAudioQueue;
        VideoFilter RGB2YUV420PFilter;
        Setup setup;

        public AsyncVideoEncoder(Setup setup, QueueThreadSafe _inVideoQueue, QueueThreadSafe _inAudioQueue, QueueThreadSafe _outVideoQueue, QueueThreadSafe _outAudioQueue) : base()
        {
            this.setup = setup;
            inVideoQueue = _inVideoQueue;
            inAudioQueue = _inAudioQueue;
            outVideoQueue = _outVideoQueue;
            outAudioQueue = _outAudioQueue;
            if (VRTConfig.Instance.ffmpegDLLDir != "")
            {
                FFmpeg.AutoGen.ffmpeg.RootPath = VRTConfig.Instance.ffmpegDLLDir;
            }
            Start();
        }

        public override void AsyncOnStop()
        {
            base.AsyncOnStop();
            Debug.Log("{Name()}: Stopped");
        }
#if ENCODER_MONOTONIC_TIMESTAMPS
        long frame = 0;
#endif
        protected override void AsyncUpdate()
        {
            if (inVideoQueue._CanDequeue() && outVideoQueue._CanEnqueue())
            {
                NativeMemoryChunk mc = (NativeMemoryChunk)inVideoQueue.Dequeue();
                if (codecVideo == null) CreateVideoCodec(mc, setup.width, setup.height, setup.fps, setup.bitrate);
                if (!RGB2YUV420PFilter.Process(new byte*[] { (byte*)mc.pointer }, ref videoFrame->data, ref videoFrame->linesize))
                    Debug.LogError("Error while encoding video (RGB2YUV420PFilter.Process)");
#if ENCODER_MONOTONIC_TIMESTAMPS
                videoFrame->pts = frame++;
#else
                long tsInFps = (mc.metadata.timestamp * setup.fps) / 1000;
                videoFrame->pts = tsInFps;
#endif
                int ret = ffmpeg.avcodec_send_frame(codecVideo_ctx, videoFrame);
                mc.free();
                if (ret < 0)
                {
                    ShowError(ret, $"avcodec_send_frame");
                }
                else
                {
                    while (ret >= 0)
                    {
                        ret = ffmpeg.avcodec_receive_packet(codecVideo_ctx, videoPacket);
                        if (ret >= 0 && ret != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret != ffmpeg.AVERROR_EOF)
                        {
                            NativeMemoryChunk videoData = new NativeMemoryChunk(videoPacket->size);
                            Buffer.MemoryCopy(videoPacket->data, (void*)videoData.pointer, videoPacket->size, videoPacket->size);
#if !ENCODER_MONOTONIC_TIMESTAMPS
                            long tsInMs = (videoPacket->pts * 1000) / setup.fps;
                            videoData.metadata.timestamp = tsInMs;
#endif
                            outVideoQueue.Enqueue(videoData);
                        }
                        else
                        {
                            // It seems EAGAIN is -11 on Windows, -35 on Mac.
                            if (ret != -11 && ret != -35)
                                ShowError(ret, $"avcodec_receive_packet");
                        }
                    }
                }
            }
        }

        void CreateVideoCodec(NativeMemoryChunk mc, int width, int height, int fps, int bitRate)
        {
            videoPacket = ffmpeg.av_packet_alloc();
            if (mc.metadata.dsi_size == 12)
            {
                width = BitConverter.ToInt32(mc.metadata.dsi, 0);
                height = BitConverter.ToInt32(mc.metadata.dsi, 4);
                fps = BitConverter.ToInt32(mc.metadata.dsi, 8);
            }

            RGB2YUV420PFilter = new VideoFilter(width, height, AVPixelFormat.AV_PIX_FMT_RGB24, AVPixelFormat.AV_PIX_FMT_YUV420P);
            codecVideo = ffmpeg.avcodec_find_encoder(setup.codec);

            if (codecVideo != null)
            {
                codecVideo_ctx = ffmpeg.avcodec_alloc_context3(codecVideo);
                if (codecVideo_ctx != null)
                {
                    codecVideo_ctx->bit_rate = bitRate; // 400000
                    codecVideo_ctx->width = width;
                    codecVideo_ctx->height = height;
                    codecVideo_ctx->time_base = new AVRational() { num = 1, den = fps };
                    codecVideo_ctx->framerate = new AVRational() { num = fps, den = 1 };
                    // emit one intra frame every ten frames
                    codecVideo_ctx->gop_size = fps * 4;
                    codecVideo_ctx->max_b_frames = 0;
                    codecVideo_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;

                    if (codecVideo->id == setup.codec)
                    {//AVCodecID.AV_CODEC_ID_H264) {
                        ffmpeg.av_opt_set(codecVideo_ctx->priv_data, "preset", "ultrafast", 0);
                        ffmpeg.av_opt_set(codecVideo_ctx->priv_data, "tune", "zerolatency", 0); //"film"
                    }

                    int ret = ffmpeg.avcodec_open2(codecVideo_ctx, codecVideo, null);
                    if (ret >= 0)
                    {
                        videoFrame = ffmpeg.av_frame_alloc();
                        videoFrame->format = (int)AVPixelFormat.AV_PIX_FMT_YUV420P;
                        videoFrame->width = width;
                        videoFrame->height = height;
                        ret = ffmpeg.av_frame_get_buffer(videoFrame, 0);

                        if (ret < 0)
                            ShowError(ret, "av_frame_get_buffer");
                    }
                    else ShowError(ret, "avcodec_open2");
                }
                else Debug.LogError("avcodec_alloc_context3 ERROR");
            }
            else Debug.LogError("avcodec_find_decoder ERROR");
        }


        byte* errbuf = null;
        void ShowError(int err, string message)
        {
            if (errbuf == null) errbuf = (byte*)Marshal.AllocHGlobal(128);
            ffmpeg.av_strerror(err, errbuf, 128);
            string err_txt = Marshal.PtrToStringAnsi((IntPtr)errbuf);
            string name = Name();
            Debug.Log($"{name}: {message} {err} {err_txt}");
            Debug.LogError("Error while encoding video");

        }
    }
}
