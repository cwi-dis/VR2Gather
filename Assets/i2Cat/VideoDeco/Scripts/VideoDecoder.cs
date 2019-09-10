using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Workers {
    public unsafe class VideoDecoder : BaseWorker {
        public string url;
        AVCodec* codec;
        AVCodecParserContext* parser;
        AVCodecContext* codec_ctx;
        AVPacket* packet;
        AVFrame* frame;

        public VideoDecoder() : base(WorkerType.Run) {

            packet = ffmpeg.av_packet_alloc();
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
                if (codec == null) CreateCodec();
                //decoder.Stream(token.currentByteArray, token.currentByteArray.Length, receiveBuffer);
                ffmpeg.av_init_packet(packet);
                int bytes_used = ffmpeg.av_parser_parse2(parser, codec_ctx, &packet->data, &packet->size, (byte*)token.currentBuffer, token.currentSize, ffmpeg.AV_NOPTS_VALUE, ffmpeg.AV_NOPTS_VALUE, 0);
                if (bytes_used < 0) {
                    Debug.Log($"Error parsing {bytes_used}");
                    return;
                }
                /*
                packet->data = (byte*)token.currentBuffer;
                packet->size = token.currentSize;
                int frame_finished;
                int ret = ffmpeg.avcodec_decode_video2(codec_ctx, frame, &frame_finished, packet);
                if (ret < 0) {
                    byte* errbuf = (byte*)Marshal.AllocHGlobal(128);
                    ffmpeg.av_strerror(ret, errbuf, 128);
                    string err_txt = Marshal.PtrToStringAnsi((System.IntPtr)errbuf);
                    Debug.Log($"Error sending a packet for decoding {ret} {err_txt}");
                }else
                    Debug.Log($"ret {ret} frame_finished {frame_finished}");
                */
                if (packet->size > 0) {
                    int ret2 = ffmpeg.avcodec_send_packet(codec_ctx, packet);
                    if (ret2 < 0) {
                        byte* errbuf = (byte*)Marshal.AllocHGlobal(128);
                        ffmpeg.av_strerror(ret2, errbuf, 128);
                        string err_txt = Marshal.PtrToStringAnsi((System.IntPtr)errbuf);
                        Debug.Log($"Error sending a packet for decoding {ret2} {err_txt}");
                    } else {
                        while (ret2 >= 0) {
                            ret2 = ffmpeg.avcodec_receive_frame(codec_ctx, frame);
                            if (ret2 >= 0 && ret2 != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret2 != ffmpeg.AVERROR_EOF) {
                                Debug.Log($"Data line size {frame->linesize[0]} width {frame->width} height {frame->height} frame {codec_ctx->frame_number}");
                            } else
                                if(ret2!=-11)
                                    Debug.Log($"ret2 {ffmpeg.AVERROR(ffmpeg.EAGAIN)}");
                        }
                    }
                }
                //token.currentFloatArray = receiveBuffer;
                //token.currentSize = receiveBuffer.Length;
                Next();
            }
        }

        void CreateCodec() {
            codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (codec != null) {
                codec_ctx = ffmpeg.avcodec_alloc_context3(codec);
                if (codec_ctx != null) {
                    //codec_ctx->width = 2732;
                    //codec_ctx->height = 1366;
                    //                    codec_ctx->extradata = (byte*)System.IntPtr.Zero;
                    //                    codec_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
                    parser = ffmpeg.av_parser_init((int)codec->id);
                    if (parser != null) {
                        //XX Romain FIX XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                        //copy decoder specific info
                        var info = token.info;
                        codec_ctx->extradata = (byte*)ffmpeg.av_calloc(1, (ulong)info.dsi_size + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE);
                        Marshal.Copy(info.dsi, 0, (System.IntPtr)codec_ctx->extradata, info.dsi_size);
                        codec_ctx->extradata_size = info.dsi_size;
                        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

                        int ret = ffmpeg.avcodec_open2(codec_ctx, codec, null);
                        frame = ffmpeg.av_frame_alloc();
                    } else Debug.Log("av_parser_init ERROR");
                } else Debug.Log("avcodec_alloc_context3 ERROR");
            } else Debug.Log("avcodec_find_decoder ERROR");

        }
    }
}
