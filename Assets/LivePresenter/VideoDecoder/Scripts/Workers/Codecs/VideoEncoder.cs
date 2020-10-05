using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using System;

namespace Workers {
    public unsafe class VideoEncoder : BaseWorker {
        public string url;
        AVCodec*                codecVideo;
        AVCodecContext*         codecVideo_ctx;
        AVPacket*               videoPacket;
        AVFrame*                videoFrame;

        SwrContext*             swrAudioFilterContext;

        public int Width { get; private set; }
        public int Height { get; private set; }

//        public System.IntPtr videoData { get; private set; }
        public int      videoDataSize { get; private set; }
        QueueThreadSafe inVideoQueue;
        QueueThreadSafe inAudioQueue;
        QueueThreadSafe outVideoQueue;
        QueueThreadSafe outAudioQueue;
        VideoFilter     RGB2YUV420PFilter;

        public VideoEncoder(QueueThreadSafe _inVideoQueue, QueueThreadSafe _inAudioQueue, QueueThreadSafe _outVideoQueue, QueueThreadSafe _outAudioQueue) : base(WorkerType.Run) {
            inVideoQueue  = _inVideoQueue;
            inAudioQueue  = _inAudioQueue;
            outVideoQueue = _outVideoQueue;
            outAudioQueue = _outAudioQueue;
            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("{Name()}: Stopped");
        }
        long frame = 0;
        protected override void Update() {
            base.Update();
            if (inVideoQueue._CanDequeue() && outVideoQueue._CanEnqueue()) {
                NativeMemoryChunk mc = (NativeMemoryChunk)inVideoQueue.Dequeue();
                if (codecVideo == null) CreateVideoCodec(mc);
                if (!RGB2YUV420PFilter.Process(new byte*[] { (byte*)mc.pointer}, ref videoFrame->data, ref videoFrame->linesize))
                    Debug.LogError("Error RGB2YUV420PFilter.Process");
                videoFrame->pts = frame++;
                int ret = ffmpeg.avcodec_send_frame(codecVideo_ctx, videoFrame);
                mc.free();
                if (ret < 0) {
                    ShowError(ret, $"avcodec_send_frame");
                } else {
                    while (ret >= 0) {
                        ret = ffmpeg.avcodec_receive_packet(codecVideo_ctx, videoPacket);
                        if (ret >= 0 && ret != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret != ffmpeg.AVERROR_EOF) {
                            NativeMemoryChunk videoData = new NativeMemoryChunk(videoPacket->size);
                            Buffer.MemoryCopy(videoPacket->data, (void*)videoData.pointer, videoPacket->size, videoPacket->size);
                            outVideoQueue.Enqueue(videoData);
                        } else {
                            if (ret != -11)
                                ShowError(ret, $"avcodec_receive_packet");
                        }
                    }
                }
            }
        }

        void CreateVideoCodec(NativeMemoryChunk mc) {
            videoPacket = ffmpeg.av_packet_alloc();
            int width=320, height=240, fps=12;
            if (mc.info.dsi_size == 12) {
                width = BitConverter.ToInt32(mc.info.dsi, 0);
                height = BitConverter.ToInt32(mc.info.dsi, 4);
                fps = BitConverter.ToInt32(mc.info.dsi, 8);
            }

            RGB2YUV420PFilter = new VideoFilter(width, height, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_RGB24, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_YUV420P);
            codecVideo = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_H264);

            if (codecVideo != null) {
                codecVideo_ctx = ffmpeg.avcodec_alloc_context3(codecVideo);
                if (codecVideo_ctx != null) {
                    codecVideo_ctx->bit_rate        = 40000; // 400000
                    codecVideo_ctx->width           = width;
                    codecVideo_ctx->height          = height;
                    codecVideo_ctx->time_base = new AVRational() { num = 1, den = fps };
                    codecVideo_ctx->framerate = new AVRational() { num = fps, den = 1 };
                    // emit one intra frame every ten frames
                    codecVideo_ctx->gop_size        = fps * 10;
                    codecVideo_ctx->max_b_frames    = 0;
                    codecVideo_ctx->pix_fmt         = FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_YUV420P;

                    if (codecVideo->id == AVCodecID.AV_CODEC_ID_H264) {
                        ffmpeg.av_opt_set(codecVideo_ctx->priv_data, "preset", "ultrafast", 0);
                        ffmpeg.av_opt_set(codecVideo_ctx->priv_data, "tune", "zerolatency", 0);
                    }

                    int ret = ffmpeg.avcodec_open2(codecVideo_ctx, codecVideo, null);
                    if (ret >= 0) {
                        videoFrame = ffmpeg.av_frame_alloc();
                        videoFrame->format  = (int)FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_YUV420P;
                        videoFrame->width   = width;
                        videoFrame->height  = height;
                        ret = ffmpeg.av_frame_get_buffer(videoFrame, 0);

                        if (ret < 0)
                            ShowError(ret, "av_frame_get_buffer");
                    } else ShowError(ret, "avcodec_open2");
                } else Debug.Log("avcodec_alloc_context3 ERROR");
            } else Debug.Log("avcodec_find_decoder ERROR");
        }


        byte* errbuf = null;
        void ShowError(int err, string message) {
            if(errbuf==null) errbuf = (byte*)Marshal.AllocHGlobal(128);
            ffmpeg.av_strerror(err, errbuf, 128);
            string err_txt = Marshal.PtrToStringAnsi((System.IntPtr)errbuf);
            string name = Name();
            Debug.LogError($"{name}: {message} {err} {err_txt}");

        }
    }
}
