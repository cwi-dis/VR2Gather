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
        AVCodec*              codec;
        AVCodecParserContext* parser;
        AVCodecContext*       codec_ctx;
        AVPacket*             packet;
        AVFrame*              frame;

        SwsContext*           swsCtx;
        byte_ptrArray4        tmpDataArray;
        int_array4            tmpLineSizeArray;
        byte*                 _pictureFrameData;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool videoIsReady { get; set; }

        public System.IntPtr videoData { get; private set; }
        public int videoDataSize { get; private set; }

        public VideoDecoder() : base(WorkerType.Run) {
            videoIsReady = false;
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
                if (token.isVideo ) {
                    if (!videoIsReady) {
                        if (codec == null) CreateVideoCodec();
                        //decoder.Stream(token.currentByteArray, token.currentByteArray.Length, receiveBuffer);
                        ffmpeg.av_init_packet(packet);
                        int bytes_used = ffmpeg.av_parser_parse2(parser, codec_ctx, &packet->data, &packet->size, (byte*)token.currentBuffer, token.currentSize, ffmpeg.AV_NOPTS_VALUE, ffmpeg.AV_NOPTS_VALUE, 0);
                        if (bytes_used < 0) {
                            Debug.Log($"Error parsing {bytes_used}");
                            return;
                        }
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
                                        CreateResizeFilter();
                                        int ret = ffmpeg.sws_scale(swsCtx, frame->data, frame->linesize, 0, frame->height, tmpDataArray, tmpLineSizeArray);

                                        videoDataSize = tmpLineSizeArray[0] * frame->height;
                                        videoData = (System.IntPtr)tmpDataArray[0];
                                        videoIsReady = true;
                                    } else
                                        if (ret2 != -11)
                                        Debug.Log($"ret2 {ffmpeg.AVERROR(ffmpeg.EAGAIN)}");
                                }
                            }
                        }
                        //token.currentFloatArray = receiveBuffer;
                        //token.currentSize = receiveBuffer.Length;
                        Next();
                    }
                }
                else
                    Next();
            }
        }

        void CreateVideoCodec() {
            codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (codec != null) {
                codec_ctx = ffmpeg.avcodec_alloc_context3(codec);
                if (codec_ctx != null) {
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

        void CreateResizeFilter() {
            if((System.IntPtr)swsCtx == System.IntPtr.Zero) {
                int num_bytes = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_RGB24, frame->width, frame->height, 1);
                _pictureFrameData = (byte*)ffmpeg.av_malloc((ulong)num_bytes);
                ffmpeg.av_image_fill_arrays(ref tmpDataArray, ref tmpLineSizeArray, (byte*)_pictureFrameData, AVPixelFormat.AV_PIX_FMT_RGB24, frame->width, frame->height, 1);
                swsCtx = ffmpeg.sws_getContext(frame->width, frame->height,
                    AVPixelFormat.AV_PIX_FMT_YUV420P, frame->width, frame->height,
                    AVPixelFormat.AV_PIX_FMT_RGB24, 0, null, null, null);
                Width = frame->width;
                Height = frame->height;
            }
        }
    }
}
