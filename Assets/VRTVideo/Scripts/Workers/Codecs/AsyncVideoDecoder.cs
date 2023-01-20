using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using VRT.Core;
using Cwipc;
namespace VRT.Video
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;

    public class VideoFrame
    {
        int width;
        int height;
    }
    public unsafe class AsyncVideoDecoder : AsyncWorker
    {
        public string url;
        AVCodec* codecVideo;
        AVCodec* codecAudio;
        AVCodecParserContext* videoParser;
        AVCodecParserContext* audioParser;
        AVCodecContext* codecVideo_ctx;
        AVCodecContext* codecAudio_ctx;
        AVPacket* videoPacket;
        AVPacket* audioPacket;
        AVFrame* videoFrame;
        AVFrame* audioFrame;

        SwsContext* swsYUV2RGBCtx;
        SwrContext* swrCtx;
        byte_ptrArray4 tmpDataArray;
        int_array4 tmpLineSizeArray;
        byte* _pictureFrameData;

        AVCodecID codec;

        public int Width { get; private set; }
        public int Height { get; private set; }

        //        public System.IntPtr videoData { get; private set; }
        public int videoDataSize { get; private set; }
        QueueThreadSafe inVideoQueue;
        QueueThreadSafe inAudioQueue;
        QueueThreadSafe outVideoQueue;
        QueueThreadSafe outAudioQueue;

        public AsyncVideoDecoder(AVCodecID codec, QueueThreadSafe _inVideoQueue, QueueThreadSafe _inAudioQueue, QueueThreadSafe _outVideoQueue, QueueThreadSafe _outAudioQueue) : base()
        {
            this.codec = codec;
            inVideoQueue = _inVideoQueue;
            inAudioQueue = _inAudioQueue;
            outVideoQueue = _outVideoQueue;
            outAudioQueue = _outAudioQueue;
            if (VRTConfig.Instance.ffmpegDLLDir != "")
            {
                FFmpeg.AutoGen.ffmpeg.RootPath = VRTConfig.Instance.ffmpegDLLDir;
            }
            videoPacket = ffmpeg.av_packet_alloc();
            audioPacket = ffmpeg.av_packet_alloc();
            Start();
        }

        public override void AsyncOnStop()
        {
            base.AsyncOnStop();
            Debug.Log("{Name()}: Stopped");
        }

        protected override void AsyncUpdate()
        {
            if (inVideoQueue._CanDequeue() && outVideoQueue._CanEnqueue())
            {
                NativeMemoryChunk mc = (NativeMemoryChunk)inVideoQueue.Dequeue();
                if (codecVideo == null) CreateVideoCodec(mc);
                videoPacket->data = (byte*)mc.pointer; // <-- Romain way
                videoPacket->size = mc.length;
                videoPacket->pts = mc.metadata.timestamp;
                if (videoPacket->size > 0)
                {
                    int ret2 = ffmpeg.avcodec_send_packet(codecVideo_ctx, videoPacket);
                    if (ret2 < 0)
                    {
                        ShowError(ret2, $"Error sending a packet for video decoding token.currentSize {mc.length} videoPacket->size {videoPacket->size}");
                    }
                    else
                    {
                        while (ret2 >= 0)
                        {
                            ret2 = ffmpeg.avcodec_receive_frame(codecVideo_ctx, videoFrame);
                            if (ret2 >= 0 && ret2 != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret2 != ffmpeg.AVERROR_EOF)
                            {
                                CreateYUV2RGBFilter();
                                int ret = ffmpeg.sws_scale(swsYUV2RGBCtx, videoFrame->data, videoFrame->linesize, 0, videoFrame->height, tmpDataArray, tmpLineSizeArray);
                                videoDataSize = tmpLineSizeArray[0] * videoFrame->height;
                                NativeMemoryChunk videoData = new NativeMemoryChunk(tmpLineSizeArray[0] * videoFrame->height);
                                System.Buffer.MemoryCopy(tmpDataArray[0], (byte*)videoData.pointer, videoData.length, videoData.length);
                                videoData.metadata.timestamp = videoFrame->pts;
                                outVideoQueue.Enqueue(videoData);
                            }
                            else
                            {
                                if (ret2 != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret2 != ffmpeg.AVERROR_EOF)
                                {
                                    Debug.LogError($"VideoDecoder: avcodec_receive_frame returned {ret2}");
                                }
                            }
                        }
                    }
                }
                mc.free();
            }

            if (inAudioQueue != null && outAudioQueue != null && inAudioQueue._CanDequeue() && outAudioQueue._CanEnqueue())
            {
                NativeMemoryChunk mc = (NativeMemoryChunk)inAudioQueue.Dequeue();
                // Audio-
                if (codecAudio == null) CreateAudioCodec(mc);
                audioPacket->data = (byte*)mc.pointer; // <-- Romain way2
                audioPacket->size = mc.length;
                audioPacket->pts = mc.metadata.timestamp; // token.info.timestamp;
                if (audioPacket->size > 0)
                {
                    int ret2 = ffmpeg.avcodec_send_packet(codecAudio_ctx, audioPacket);
                    if (ret2 < 0)
                    {
                        ShowError(ret2, $"Error sending a packet for audio decoding token.currentSize {mc.length} audioPacket->size {audioPacket->size}");
                    }
                    else
                    {
                        while (ret2 >= 0)
                        {
                            ret2 = ffmpeg.avcodec_receive_frame(codecAudio_ctx, audioFrame);
                            if (ret2 >= 0 && ret2 != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret2 != ffmpeg.AVERROR_EOF)
                            {
                                fixed (byte** tmp = (byte*[])audioFrame->data)
                                {
                                    FloatMemoryChunk audioData = new FloatMemoryChunk(audioFrame->nb_samples * 2);
                                    float* src = (float*)tmp[0];
                                    float* dst = (float*)audioData.pointer.ToInt64();
                                    for (int i = 0; i < audioFrame->nb_samples; i++)
                                    {
                                        audioData.buffer[i * 2 + 0] = src[i];
                                        audioData.buffer[i * 2 + 1] = src[i];
                                    }
                                    outAudioQueue.Enqueue(audioData);
                                }
                            }
                            else
                                if (ret2 != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret2 != ffmpeg.AVERROR_INVALIDDATA)
                            {
                                ShowError(ret2, $"Error receiving frame for audio decoding");
                            }
                        }
                    }
                }
                mc.free();
            }
        }

        void CreateVideoCodec(NativeMemoryChunk mc)
        {
            codecVideo = ffmpeg.avcodec_find_decoder(codec);
            if (codecVideo != null)
            {
                codecVideo_ctx = ffmpeg.avcodec_alloc_context3(codecVideo);
                if (codecVideo_ctx != null)
                {
                    videoParser = ffmpeg.av_parser_init((int)codecVideo->id);
                    if (videoParser != null)
                    {
                        //XX Romain FIX XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                        //copy decoder specific info
                        var info = mc.metadata;
                        if (info != null && info.dsi_size != 0)
                        {
                            codecVideo_ctx->extradata = (byte*)ffmpeg.av_calloc(1, (ulong)info.dsi_size + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE);
                            Marshal.Copy(info.dsi, 0, (System.IntPtr)codecVideo_ctx->extradata, info.dsi_size);
                            codecVideo_ctx->extradata_size = info.dsi_size;
                        }
                        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                        int ret = ffmpeg.avcodec_open2(codecVideo_ctx, codecVideo, null);


                        videoFrame = ffmpeg.av_frame_alloc();
                    }
                    else Debug.Log("av_parser_init ERROR");
                }
                else Debug.Log("avcodec_alloc_context3 ERROR");
            }
            else Debug.LogError("[FPA] avcodec_find_decoder ERROR");

        }

        void CreateYUV2RGBFilter()
        {
            if ((System.IntPtr)swsYUV2RGBCtx == System.IntPtr.Zero)
            {
                int num_bytes = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_RGB24, videoFrame->width, videoFrame->height, 1);
                _pictureFrameData = (byte*)ffmpeg.av_malloc((ulong)num_bytes);
                ffmpeg.av_image_fill_arrays(ref tmpDataArray, ref tmpLineSizeArray, _pictureFrameData, AVPixelFormat.AV_PIX_FMT_RGB24, videoFrame->width, videoFrame->height, 1);
                swsYUV2RGBCtx = ffmpeg.sws_getContext(videoFrame->width, videoFrame->height, AVPixelFormat.AV_PIX_FMT_YUV420P, videoFrame->width, videoFrame->height, AVPixelFormat.AV_PIX_FMT_RGB24, 0, null, null, null);


                int_array4 srcTable, dstTable;
                srcTable[0] = 104597; srcTable[1] = 132201; srcTable[2] = 25675; srcTable[3] = 53279;  // { 104597, 132201, 25675, 53279 }, /* ITU-R Rec. 624-4 System B, G
                dstTable[0] = 117489; dstTable[1] = 138438; dstTable[2] = 13975; dstTable[3] = 34925; // { 117489, 138438, 13975, 34925 }, /* ITU-R Rec. 709 (1990)
#if !(UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
                int brightness = 0;
                int contrast = 65536;
                int saturation = 65536;
                int srcRange = 0;
                int dstRange = 0;
                // xxxjack this is probably needed for good color quality, but it seems to crash on the mac...
                int ret = ffmpeg.sws_setColorspaceDetails(swsYUV2RGBCtx, srcTable, srcRange, dstTable, dstRange, brightness, contrast, saturation);
#endif
                Width = videoFrame->width;
                Height = videoFrame->height;
            }
        }
        void CreateAudioCodec(NativeMemoryChunk mc)
        {
            codecAudio = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_AAC);
            if (codecAudio != null)
            {
                codecAudio_ctx = ffmpeg.avcodec_alloc_context3(codecAudio);
                if (codecAudio_ctx != null)
                {
                    audioParser = ffmpeg.av_parser_init((int)codecAudio->id);
                    if (audioParser != null)
                    {
                        //XX Romain FIX XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                        //copy decoder specific info
                        var info = mc.metadata;
                        codecAudio_ctx->extradata = (byte*)ffmpeg.av_calloc(1, (ulong)info.dsi_size + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE);
                        Marshal.Copy(info.dsi, 0, (System.IntPtr)codecAudio_ctx->extradata, info.dsi_size);
                        codecAudio_ctx->extradata_size = info.dsi_size;
                        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                        int ret = ffmpeg.avcodec_open2(codecAudio_ctx, codecAudio, null);
                        Debug.Log($"avcodec_open2 returns {ret}");
                        audioFrame = ffmpeg.av_frame_alloc();
                    }
                    else Debug.Log("av_parser_init ERROR");
                }
                else Debug.Log("avcodec_alloc_context3 ERROR");
            }
            else Debug.Log("avcodec_find_decoder ERROR");
        }

        byte** dst_data;
        int dst_linesize;
        int max_dst_nb_samples;
        int dst_nb_samples;
        byte*[] arrayAudio = new byte*[1];

        void CreateResampleFilter()
        {
            if ((System.IntPtr)swrCtx == System.IntPtr.Zero)
            {
                swrCtx = ffmpeg.swr_alloc();
                int src_nb_samples = 1024;
                int dst_rate = VRTConfig.Instance.audioSampleRate;

                ffmpeg.av_opt_set_int(swrCtx, "in_channel_layout", (long)audioFrame->channel_layout, 0);          // Source layout
                ffmpeg.av_opt_set_int(swrCtx, "in_sample_rate", audioFrame->sample_rate, 0);                // Source sample rate.
                ffmpeg.av_opt_set_sample_fmt(swrCtx, "in_sample_fmt", (AVSampleFormat)audioFrame->format, 0); // Source sample format.
                ffmpeg.av_opt_set_int(swrCtx, "out_channel_layout", (long)ffmpeg.AV_CH_LAYOUT_MONO, 0); // Target layout
                ffmpeg.av_opt_set_int(swrCtx, "out_sample_rate", dst_rate, 0); // Target sample rate.
                ffmpeg.av_opt_set_sample_fmt(swrCtx, "out_sample_fmt", AVSampleFormat.AV_SAMPLE_FMT_FLTP, 0); // Target sample format. // AV_SAMPLE_FMT_FLTP
                int ret = 0;
                /* initialize the resampling context */
                if ((ret = ffmpeg.swr_init(swrCtx)) < 0)
                {
                    Debug.Log("ERROR");
                    //                    fprintf(stderr, "Failed to initialize the resampling context\n");
                    //                    goto end;
                }

                dst_nb_samples = (int)ffmpeg.av_rescale_rnd(src_nb_samples, dst_rate, audioFrame->sample_rate, AVRounding.AV_ROUND_UP);
                // buffer is going to be directly written to a rawaudio file, no alignment 
                int dst_nb_channels = ffmpeg.av_get_channel_layout_nb_channels(ffmpeg.AV_CH_LAYOUT_MONO);
                fixed (byte*** data = &dst_data)
                {
                    fixed (int* linesize = &dst_linesize)
                    {
                        ret = ffmpeg.av_samples_alloc_array_and_samples(data, linesize, dst_nb_channels, dst_nb_samples, AVSampleFormat.AV_SAMPLE_FMT_FLTP, 0);
                    }
                }
            }
        }

        byte* errbuf = null;
        void ShowError(int err, string message)
        {
            if (errbuf == null) errbuf = (byte*)Marshal.AllocHGlobal(128);
            ffmpeg.av_strerror(err, errbuf, 128);
            string err_txt = Marshal.PtrToStringAnsi((System.IntPtr)errbuf);
            Debug.Log($"{Name()}: {message} {err} {err_txt}");

        }
    }
}
