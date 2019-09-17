using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

public unsafe class TestFFMpeg : MonoBehaviour {

    private const int INBUF_SIZE = 4096;
    private const long AV_NOPTS_VALUE = 0x800000000000000;

    public string infilename;
    public string outfilepath;

    public bool wDecoder;
    private bool finish = false;

    AVCodec* codec;
    AVCodecParserContext* parser;
    AVCodecContext* codec_ctx;
    AVPacket* packet;
    AVFrame* frame;

    AVCodec* ff_h264_decoder;
    AVCodecParser* ff_h264_parser;

    // Start is called before the first frame update
    void Start() {
        CheckMode2(infilename, outfilepath);
    }

    public struct buffer_data {
        public byte* ptr;
        public int offset;
        public int size; ///< size left in the buffer
    };

    [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
    public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);


    int sum = 0;
    int read_function(void* opaque, byte* buf, int buf_size) {
        buffer_data *bd = (buffer_data *)opaque;
        buf_size = (int)Mathf.Min (buf_size, (bd->size- bd->offset));
        CopyMemory((IntPtr)buf, (IntPtr)(bd->ptr + bd->offset), (uint)buf_size);
        bd->offset += buf_size;
        sum += buf_size;
    return buf_size;
    }

    long seek_function(void* opaque, long position, int whence) {
        buffer_data* bd = (buffer_data*)opaque;
        Debug.Log($"seek_function offset {position} whence {whence}");
        if (whence == ffmpeg.AVSEEK_SIZE) {
            return bd->size;
        }
        bd->offset = (int)position;
        return position;
    }

    void CheckMode2(string infilename, string outfilepath) {
        int avio_ctx_buffer_size = 4096;
        byte* avio_ctx_buffer;
        AVIOContext* avio_ctx;
        AVCodecContext* video_dec_ctx;
        AVCodec* video_dec;
        buffer_data bd;
        byte[] bytes = System.IO.File.ReadAllBytes(infilename);
        bd.ptr = (byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
        bd.size = bytes.Length;
        bd.offset = 0;
        int ret;

        AVFormatContext* fmt_ctx;
        
        fmt_ctx = ffmpeg.avformat_alloc_context();
        avio_ctx_buffer = (byte*)ffmpeg.av_malloc((ulong)avio_ctx_buffer_size);
        avio_ctx = ffmpeg.avio_alloc_context(avio_ctx_buffer, avio_ctx_buffer_size, 0, &bd, (avio_alloc_context_read_packet)read_function, null, (avio_alloc_context_seek)seek_function);
        if (avio_ctx==null) {
            Debug.Log("Could not create avio_ctx");
            return;

        }
        fmt_ctx->flags |= ffmpeg.AVFMT_FLAG_CUSTOM_IO;
        fmt_ctx->pb = avio_ctx;

        ret = ffmpeg.avformat_open_input(&fmt_ctx, "", null, null);
        if (ret < 0) {
            byte* errbuf = (byte*)Marshal.AllocHGlobal(128);
            ffmpeg.av_strerror(ret, errbuf, 128);
            string err_txt = Marshal.PtrToStringAnsi((IntPtr)errbuf);
            Debug.Log($"Could not open input {ret} {err_txt}");
           return;
        }

        ret = ffmpeg.avformat_find_stream_info(fmt_ctx, null);
        if (ret < 0) {
            byte* errbuf = (byte*)Marshal.AllocHGlobal(128);
            ffmpeg.av_strerror(ret, errbuf, 128);
            string err_txt = Marshal.PtrToStringAnsi((IntPtr)errbuf);
            Debug.Log($"Could not find stream information {err_txt}");
            return;
        }

        int video_stream_index = ffmpeg.av_find_best_stream(fmt_ctx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &video_dec, 0);
        if (video_stream_index < 0) {
            Debug.Log($"Cannot find a video stream in the input file");
            return;
        }
        video_dec_ctx = fmt_ctx->streams[video_stream_index]->codec;
        if ((ret = ffmpeg.avcodec_open2(video_dec_ctx, video_dec, null)) < 0) {
            Debug.Log("Cannot open video decoder");
            return;
        }
        Debug.Log($">>> width {video_dec_ctx->width} height {video_dec_ctx->height} pix_fmt {video_dec_ctx->pix_fmt} ");

        AVCodecParserContext* parser = ffmpeg.av_parser_init((int)video_dec->id);
        if (parser==null) {
            Debug.Log($"Cannot av_parser_init");
            return;
        }

        ret = ffmpeg.avcodec_open2(video_dec_ctx, video_dec, null);
        if (ret < 0) {
            Debug.Log($"Cannot avcodec_open2");
            return;
        }

        AVFrame* frame = ffmpeg.av_frame_alloc();
        AVPacket* packet = ffmpeg.av_packet_alloc();
        ffmpeg.av_init_packet(packet);
        packet->data = null;
        packet->size = 0;
        
        AVFrame* frame2 = ffmpeg.av_frame_alloc();
        frame2->width = video_dec_ctx->width;
        frame2->height = video_dec_ctx->height;
        frame2->format = (int)AVPixelFormat.AV_PIX_FMT_RGB24;
        int num_bytes = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_RGB24, video_dec_ctx->width, video_dec_ctx->height,1);
        var _pictureFrameData = ffmpeg.av_malloc((ulong)num_bytes);
        byte_ptrArray4 tmpDataArray;
        int_array4 tmpLineSizeArray;
        ffmpeg.av_image_fill_arrays(ref tmpDataArray, ref tmpLineSizeArray, (byte*)_pictureFrameData, (AVPixelFormat)frame2->format, frame2->width, frame2->height, 1);

        SwsContext* ctx = ffmpeg.sws_getContext(video_dec_ctx->width, video_dec_ctx->height,
                    AVPixelFormat.AV_PIX_FMT_YUV420P, video_dec_ctx->width, video_dec_ctx->height,
                    AVPixelFormat.AV_PIX_FMT_RGB24, 0, null, null, null);
        
        while (ffmpeg.av_read_frame(fmt_ctx, packet) >= 0) {
            if (packet->stream_index == video_stream_index) {
                ret = ffmpeg.avcodec_send_packet(video_dec_ctx, packet);
                if (ret < 0) {
                    byte* errbuf = (byte*)Marshal.AllocHGlobal(128);
                    ffmpeg.av_strerror(ret, errbuf, 128);
                    string err_txt = Marshal.PtrToStringAnsi((IntPtr)errbuf);
                    Debug.Log($"avcodec_send_packet error({packet->stream_index}): {err_txt}");
                    return;
                }
                while (ret >= 0) {
                    ret = ffmpeg.avcodec_receive_frame(video_dec_ctx, frame);
                    if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF) {
                        break;
                    } else if (ret < 0) {
                        Debug.Log("Error during decoding");
                        return;
                    }
                    Debug.Log($"Frame {video_dec_ctx->frame_number}");
                    /*
                    if (video_dec_ctx->frame_number < 50) {
                        //                        Debug.Log($"Frame {video_dec_ctx->frame_number} linesize {frame->linesize[0]}");
                            ret = ffmpeg.sws_scale(ctx, frame->data, frame->linesize, 0, frame->height, tmpDataArray, tmpLineSizeArray);
                            txt.LoadRawTextureData((IntPtr)tmpDataArray[0], tmpLineSizeArray[0] * frame->height);
                            txt.Apply();
                            System.IO.File.WriteAllBytes($"{outfilepath}/frame_{video_dec_ctx->frame_number}.png", txt.EncodeToPNG());
                    } else
                        return;
                      */

                }
                ffmpeg.av_packet_unref(packet);
            }
            //else 
            {
                Debug.Log($"Frame sum={sum} video {packet->stream_index== video_stream_index}");
                sum = 0;

            }
        }
    }
}
