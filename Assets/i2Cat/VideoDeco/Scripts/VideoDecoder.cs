using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Workers {
    public class VideoFrame {
        int width;
        int height;
    }
    public unsafe class VideoDecoder : BaseWorker {
        public string url;
        AVCodec*                codecVideo;
        AVCodec*                codecAudio;
        AVCodecParserContext*   videoParser;
        AVCodecParserContext*   audioParser;
        AVCodecContext*         codecVideo_ctx;
        AVCodecContext*         codecAudio_ctx;
        AVPacket*               videoPacket;
        AVPacket*               audioPacket;
        AVFrame*                videoFrame;
        AVFrame*                audioFrame;

        SwsContext*             swsYUV2RGBCtx;
        SwrContext*             swrCtx;
        byte_ptrArray4          tmpDataArray;
        int_array4              tmpLineSizeArray;
        byte*                 _pictureFrameData;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool videoIsReady { get; set; }

        public System.IntPtr videoData { get; private set; }
        public int videoDataSize { get; private set; }

        public VideoDecoder() : base(WorkerType.Run) {
            videoIsReady = false;
            videoPacket = ffmpeg.av_packet_alloc();
            audioPacket = ffmpeg.av_packet_alloc();
            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VideoDecoder Stopped");
        }

        NTPTools.NTPTime tempTime;
        protected override void Update() {
            base.Update();
            if (token != null) {
                if (token.isVideo ) {
                    if (!videoIsReady) {
                        if (codecVideo == null) CreateVideoCodec();
                        ffmpeg.av_init_packet(videoPacket);
                        videoPacket->data = (byte*)token.currentBuffer; // <-- Romain way
                        videoPacket->size = token.currentSize;
                        videoPacket->pts = token.info.timestamp;
                        /*
                         * int bytes_used = ffmpeg.av_parser_parse2(videoParser, codecVideo_ctx, &videoPacket->data, &videoPacket->size, (byte*)token.currentBuffer, token.currentSize, ffmpeg.AV_NOPTS_VALUE, ffmpeg.AV_NOPTS_VALUE, 0);
                        if (bytes_used < 0) {
                            Debug.Log($"Error parsing {bytes_used}");
                            return;
                        }
                        */
                        if (videoPacket->size > 0) {
                            int ret2 = ffmpeg.avcodec_send_packet(codecVideo_ctx, videoPacket);
                            if (ret2 < 0) {
                                ShowError(ret2, $"Error sending a packet for video decoding token.currentSize {token.currentSize} videoPacket->size {videoPacket->size}");
                            } else {
                                while (ret2 >= 0) {
                                    ret2 = ffmpeg.avcodec_receive_frame(codecVideo_ctx, videoFrame);
                                    if (ret2 >= 0 && ret2 != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret2 != ffmpeg.AVERROR_EOF) {
                                        CreateYUV2RGBFilter();
                                        int ret = ffmpeg.sws_scale(swsYUV2RGBCtx, videoFrame->data, videoFrame->linesize, 0, videoFrame->height, tmpDataArray, tmpLineSizeArray);
                                        videoData = (System.IntPtr)tmpDataArray[0];
                                        videoDataSize = tmpLineSizeArray[0] * videoFrame->height;
                                        videoIsReady = true;
                                        //Debug.Log($"framerate {codecVideo_ctx->framerate.num} frame_offset {codecVideo_ctx->framerate.den}");
                                        //Debug.Log($"cur_offset {videoParser->cur_offset} frame_offset {videoParser->frame_offset}");

                                        // Debug.Log($"Video data -> ready pkt_pos {videoFrame->pkt_pos} pkt_duration {videoFrame->pkt_duration}");
                                    } else
                                        if (ret2 != -11)
                                            Debug.Log($"ret2 {ffmpeg.AVERROR(ffmpeg.EAGAIN)}");
                                }
                            }
                        }
                        Next();
                    }
                } else {
                    // Audio-
                    if (codecAudio == null) CreateAudioCodec();
                    ffmpeg.av_init_packet(audioPacket);
                    audioPacket->data = (byte*)token.currentBuffer; // <-- Romain way
                    audioPacket->size = token.currentSize;
                    audioPacket->pts = token.info.timestamp;
                    /*
                                        int bytes_used = ffmpeg.av_parser_parse2(audioParser, codecAudio_ctx, &audioPacket->data, &audioPacket->size, (byte*)token.currentBuffer, token.currentSize, ffmpeg.AV_NOPTS_VALUE, ffmpeg.AV_NOPTS_VALUE, 0);
                                        if (bytes_used < 0) {
                                            Debug.Log($"Error parsing {bytes_used}");
                                            return;
                                        }
                    */
                    if (audioPacket->size > 0) {
                        int ret2 = ffmpeg.avcodec_send_packet(codecAudio_ctx, audioPacket);
                        if (ret2 < 0) {
                            ShowError(ret2, $"Error sending a packet for audio decoding token.currentSize {token.currentSize} audioPacket->size {audioPacket->size}");
                        } else {
                            while (ret2 >= 0) {
                                ret2 = ffmpeg.avcodec_receive_frame(codecAudio_ctx, audioFrame);
                                if (ret2 >= 0 && ret2 != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret2 != ffmpeg.AVERROR_EOF) {
                                    CreateResampleFilter();

                                    Debug.Log($"Audio data -> ready nb_samples {audioFrame->nb_samples} sample_rate {audioFrame->sample_rate}  channel_layout {audioFrame->channel_layout} format {audioFrame->format}");
                                    fixed (byte** tmp = (byte*[])audioFrame->data) {
                                        int ret = ffmpeg.swr_convert(swrCtx, dst_data, dst_nb_samples, tmp, audioFrame->nb_samples);
                                        if (ret < 0) {
                                            ShowError(ret, "Error while converting");
                                        } else {
                                            Debug.Log($"Todo oK!! dst_nb_samples {dst_nb_samples}");
                                        }
                                    }
                                } else
                                    if (ret2 != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret2 != ffmpeg.AVERROR_INVALIDDATA) {
                                            ShowError(ret2, $"Error receiving frame for audio decoding");
                                    }
                            }
                        }
                    }
                    Next();

                }
            }
        }

        void CreateVideoCodec() {
            codecVideo = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (codecVideo != null) {
                codecVideo_ctx = ffmpeg.avcodec_alloc_context3(codecVideo);
                if (codecVideo_ctx != null) {
                    videoParser = ffmpeg.av_parser_init((int)codecVideo->id);
                    if (videoParser != null) {
                        //XX Romain FIX XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                        //copy decoder specific info
                        var info = token.info;
                        codecVideo_ctx->extradata = (byte*)ffmpeg.av_calloc(1, (ulong)info.dsi_size + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE);
                        Marshal.Copy(info.dsi, 0, (System.IntPtr)codecVideo_ctx->extradata, info.dsi_size);
                        codecVideo_ctx->extradata_size = info.dsi_size;
                        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                        int ret = ffmpeg.avcodec_open2(codecVideo_ctx, codecVideo, null);


                        videoFrame = ffmpeg.av_frame_alloc();
                    } else Debug.Log("av_parser_init ERROR");
                } else Debug.Log("avcodec_alloc_context3 ERROR");
            } else Debug.Log("avcodec_find_decoder ERROR");

        }

        void CreateYUV2RGBFilter() {
            if((System.IntPtr)swsYUV2RGBCtx == System.IntPtr.Zero) {
                int num_bytes = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_RGB24, videoFrame->width, videoFrame->height, 1);
                _pictureFrameData = (byte*)ffmpeg.av_malloc((ulong)num_bytes);
                ffmpeg.av_image_fill_arrays(ref tmpDataArray, ref tmpLineSizeArray, (byte*)_pictureFrameData, AVPixelFormat.AV_PIX_FMT_RGB24, videoFrame->width, videoFrame->height, 1);
                swsYUV2RGBCtx = ffmpeg.sws_getContext(videoFrame->width, videoFrame->height, AVPixelFormat.AV_PIX_FMT_YUV420P, videoFrame->width, videoFrame->height, AVPixelFormat.AV_PIX_FMT_RGB24, 0, null, null, null);
                Width = videoFrame->width;
                Height = videoFrame->height;
            }
        }

        void CreateAudioCodec() {
            codecAudio = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_AAC);
            if (codecAudio != null) {
                codecAudio_ctx = ffmpeg.avcodec_alloc_context3(codecAudio);
                if (codecAudio_ctx != null) {
                    audioParser = ffmpeg.av_parser_init((int)codecAudio->id);
                    if (audioParser != null) {
                        //XX Romain FIX XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                        //copy decoder specific info
                        var info = token.info;
                        codecAudio_ctx->extradata = (byte*)ffmpeg.av_calloc(1, (ulong)info.dsi_size + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE);
                        Marshal.Copy(info.dsi, 0, (System.IntPtr)codecAudio_ctx->extradata, info.dsi_size);
                        codecAudio_ctx->extradata_size = info.dsi_size;
                        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                        int ret = ffmpeg.avcodec_open2(codecAudio_ctx, codecAudio, null);
                        Debug.Log($"avcodec_open2 returns {ret}");
                        audioFrame = ffmpeg.av_frame_alloc();
                    } else Debug.Log("av_parser_init ERROR");
                } else Debug.Log("avcodec_alloc_context3 ERROR");
            } else Debug.Log("avcodec_find_decoder ERROR");

        }

        byte** dst_data;
        int dst_linesize;
        int max_dst_nb_samples;
        int dst_nb_samples;
        byte*[] arrayAudio = new byte*[1];

        void CreateResampleFilter() {
            if ((System.IntPtr)swrCtx == System.IntPtr.Zero) {
                swrCtx = ffmpeg.swr_alloc();
                int src_nb_samples = 1024;
                int dst_rate = 16000;

                ffmpeg.av_opt_set_int(swrCtx, "in_channel_layout", (long)audioFrame->channel_layout, 0);          // Source layout
                ffmpeg.av_opt_set_int(swrCtx, "in_sample_rate", audioFrame->sample_rate, 0);                // Source sample rate.
                ffmpeg.av_opt_set_sample_fmt(swrCtx, "in_sample_fmt", (AVSampleFormat)audioFrame->format, 0); // Source sample format.

                ffmpeg.av_opt_set_int(swrCtx, "out_channel_layout", ffmpeg.AV_CH_LAYOUT_MONO, 0);           // Target layout
                ffmpeg.av_opt_set_int(swrCtx, "out_sample_rate", dst_rate, 0);                                 // Target sample rate.
                ffmpeg.av_opt_set_sample_fmt(swrCtx, "out_sample_fmt", AVSampleFormat.AV_SAMPLE_FMT_FLTP, 0);// Target sample format. // AV_SAMPLE_FMT_FLTP
                int ret = 0;
                /* initialize the resampling context */
                if ((ret = ffmpeg.swr_init(swrCtx)) < 0) {
                    Debug.Log("ERROR");
//                    fprintf(stderr, "Failed to initialize the resampling context\n");
//                    goto end;
                }


                max_dst_nb_samples = dst_nb_samples = (int)ffmpeg.av_rescale_rnd(src_nb_samples, dst_rate, audioFrame->sample_rate, AVRounding.AV_ROUND_UP);
                // buffer is going to be directly written to a rawaudio file, no alignment 
                int dst_nb_channels = ffmpeg.av_get_channel_layout_nb_channels(ffmpeg.AV_CH_LAYOUT_MONO);
                fixed (byte*** data = &dst_data) {
                    fixed (int* linesize = &dst_linesize) {
                        ret = ffmpeg.av_samples_alloc_array_and_samples(data, linesize, dst_nb_channels, dst_nb_samples, AVSampleFormat.AV_SAMPLE_FMT_FLTP, 0);
                    }
                }
            }
        }


        byte* errbuf = null;
        void ShowError(int err, string message) {
            if(errbuf==null) errbuf = (byte*)Marshal.AllocHGlobal(128);
            ffmpeg.av_strerror(err, errbuf, 128);
            string err_txt = Marshal.PtrToStringAnsi((System.IntPtr)errbuf);
            Debug.Log($"{message} {err} {err_txt}");

        }
    }
}
