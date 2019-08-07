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
    public string outfilename;

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
        CheckMode2(infilename);
        return;

        AVOutputFormat* fmt = ffmpeg.av_guess_format(null, infilename, null);
        Debug.Log($"video_codec {fmt->video_codec} audio_codec {fmt->audio_codec}");

        packet = ffmpeg.av_packet_alloc();
        codec = ffmpeg.avcodec_find_decoder( AVCodecID.AV_CODEC_ID_H264);
        if (codec != null) {
            codec_ctx = ffmpeg.avcodec_alloc_context3(codec);
            if (codec_ctx != null) {
                parser = ffmpeg.av_parser_init((int)codec->id);
                if (parser != null) {
                    int ret = ffmpeg.avcodec_open2(codec_ctx, codec, null);
                    frame = ffmpeg.av_frame_alloc();
                    if (frame != null) Parse();
                } else Debug.Log("av_parser_init ERROR");
            } else Debug.Log("avcodec_alloc_context3 ERROR");
        } else Debug.Log("avcodec_find_decoder ERROR");
    }

    void Parse() {
        if (!finish) {
            try {
                using (var stream = new FileStream(infilename, FileMode.Open, FileAccess.Read)) {
                    int numBytesToRead = 4096;
                    byte[] bytes = new byte[numBytesToRead];
                    int data_size = stream.Read(bytes, 0, numBytesToRead);
                    while (data_size > 0) {
                        int offset = 0;
                        while (data_size > 0 ) {
                            ffmpeg.av_init_packet(packet);
                            byte* data = (byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(bytes, offset);
                            int bytes_used = ffmpeg.av_parser_parse2(parser, codec_ctx, &packet->data, &packet->size, data, data_size, AV_NOPTS_VALUE, AV_NOPTS_VALUE, 0);
                            if (bytes_used < 0) {
                                Debug.Log("Error parsing");
                                return;
                            }
                            offset += bytes_used;
                            data_size -= bytes_used;
                            if (packet->size > 0 )  {
                                Debug.Log($"bytes_used {bytes_used} size {packet->size}");
                                //int gotFrame;
                                int ret2 = ffmpeg.avcodec_send_packet(codec_ctx, packet);
                                if (ret2 < 0) {
                                    byte* errbuf = (byte*)Marshal.AllocHGlobal(128);
                                    ffmpeg.av_strerror(ret2, errbuf, 128);
                                    string err_txt = Marshal.PtrToStringAnsi((IntPtr)errbuf);
                                    Debug.Log($"Error sending a packet for decoding {ret2} {err_txt}");
                                } else {
                                    while (ret2 >= 0) {
                                        ret2 = ffmpeg.avcodec_receive_frame(codec_ctx, frame);
                                        if (ret2 >= 0 && ret2 != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret2 != ffmpeg.AVERROR_EOF) {
                                            Debug.Log($"saving frame {codec_ctx->frame_number}");
                                        }else
                                            Debug.Log($"ret2 {ret2}" );

                                        //SaveToFile(frame->data[0], frame->linesize[0], frame->width, frame->height);
                                    }
                                }
                            }

                            //DecodeFile(c, frame, pkt, outfilename);
                            //data_size = stream.Read(bytes, 0, numBytesToRead);
                        }
                        data_size = stream.Read(bytes, 0, numBytesToRead);
                        //Debug.Log($">> read Buffer {data_size}");
                    }
                    //int ret = ffmpeg.av_parser_parse2(parser, c, &pkt->data, &pkt->size, data, data_size, AV_NOPTS_VALUE, AV_NOPTS_VALUE, 0);
                    //if (ret < 0) {
                    //    Debug.Log("Error while parsing");
                    //}
                    //data += ret;
                    //data_size -= ret;

                    //if (pkt->size > 0)
                    //    DecodeFile();

                    //using (FileStream fsNew = new FileStream(outfilename, FileMode.Create, FileAccess.Write)) {
                    //    fsNew.Write(bytes, 0, numBytesToRead);
                    //}
                }
                finish = true;
            } catch (FileNotFoundException ioEx) {
                Debug.Log(ioEx.Message);
            }
        }
    }

    //void DecodeStream(byte* pData, int sz) {
    //    AVPacket packet;
    //    ffmpeg.av_init_packet(&packet);

    //    packet.data = pData;
    //    packet.size = sz;
    //    int framefinished = 0;
    //    int nres = ffmpeg.avcodec_decode_video2(c, frame, &framefinished, &packet);

    //    byte[] arr = new byte[frame->height];
    //    Marshal.Copy((IntPtr)frame->data_0, arr, 0, frame->height);

    //    if (framefinished != 0) {
    //        try {
    //            using (var fs = new FileStream(outfilename, FileMode.Create, FileAccess.Write)) {
    //                fs.Write(arr, 0, arr.Length);
    //            }
    //        }
    //        catch (Exception ex) {
    //            Console.WriteLine("Exception caught in process: {0}", ex);
    //        }
    //    }
    //}

    AVFrame* DecodeFile() {
        //AVPacket packet;
        //ffmpeg.av_init_packet(pkt);

        int ret = ffmpeg.avcodec_send_packet(codec_ctx, packet);
        if (ret < 0) Debug.Log("Error sending a packet for decoding");

        while (ret >= 0) {
            ret = ffmpeg.avcodec_receive_frame(codec_ctx, frame);
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF) return null;
            else if (ret < 0) Debug.Log("Error during decoding");

            Debug.Log("saving frame " + codec_ctx->frame_number);

            //SaveToFile(frame->data[0], frame->linesize[0], frame->width, frame->height);
        }

        return frame;

        //pkt->data = pData;
        //pkt->size = sz;
        //int framefinished = 0;
        //int nres = 0;

        //nres = ffmpeg.avcodec_decode_video2(c, frame, &framefinished, pkt);

        //Debug.Log("nres: " + nres);
        //Debug.Log("Frame_0: " + frame->data[0]->ToString());
        //Debug.Log("Frame_0: " + frame->data[1]->ToString());
        //Debug.Log("Frame_0: " + frame->data[2]->ToString());
        //Debug.Log("Frame_0: " + frame->data[3]->ToString());
        //Debug.Log("Frame_0: " + frame->data[4]->ToString());
        //Debug.Log("Frame_0: " + frame->data[5]->ToString());
        //Debug.Log("Frame_0: " + frame->data[6]->ToString());
        //Debug.Log("Frame_0: " + frame->data[7]->ToString());
        //Debug.Log("Height: " + frame->height);
        //Debug.Log("Width: " + frame->width);

        //byte[] arr = new byte[frame->height];
        //Marshal.Copy((IntPtr)frame->data_0, arr, 0, frame->height);

        //if (framefinished != 0) {
        //    // do the yuv magic and call a consumer
        //    try {
        //        using (var fs = new FileStream(outfilename, FileMode.Create, FileAccess.Write)) {
        //            fs.Write(arr, 0, arr.Length);
        //        }
        //    }
        //    catch (Exception ex) {
        //        Console.WriteLine("Exception caught in process: {0}", ex);
        //    }
        //}
    }

    void SaveToFile(byte* buf, int wrap, int xsize, int ysize) {
        try {
            using (var fs = new FileStream(outfilename, FileMode.OpenOrCreate, FileAccess.Write)) {

                byte[] buffer = new byte[ysize];
                Marshal.Copy((IntPtr)buf, buffer, 0, ysize);
                fs.Write(buffer, 0, ysize);
            }
        } catch (Exception ex) {
            Debug.Log("Exception caught in process: " + ex);
        }
    }

    public struct buffer_data {
        public byte* ptr;
        public int size; ///< size left in the buffer
    };

    [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
    public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

    int read_packet(void* opaque, byte* buf, int buf_size) {
        buffer_data *bd = (buffer_data *)opaque;
        Debug.Log($"read_pakage: {buf_size}");
        buf_size = (int)Mathf.Min (buf_size, bd->size);

        /* copy internal buffer data to buf */
        CopyMemory((IntPtr)bd->ptr, (IntPtr)buf, (uint)buf_size);
        bd->ptr  += buf_size;
        bd->size -= buf_size;
    return buf_size;
}

void CheckMode2(string infilename) {
        int avio_ctx_buffer_size = 80192;
        byte* avio_ctx_buffer;
        AVIOContext* avio_ctx;
        buffer_data bd;
        byte[] bytes = System.IO.File.ReadAllBytes(infilename);
        bd.ptr = (byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
        bd.size = bytes.Length;

        AVFormatContext* fmt_ctx = ffmpeg.avformat_alloc_context();
        avio_ctx_buffer = (byte*)ffmpeg.av_malloc((ulong)avio_ctx_buffer_size);
        avio_ctx = ffmpeg.avio_alloc_context(avio_ctx_buffer, avio_ctx_buffer_size, 0, &bd, (avio_alloc_context_read_packet)read_packet, null, null);
        if (avio_ctx==null) {
            Debug.Log("Could not create avio_ctx");
            return;

        }
        fmt_ctx->flags |= ffmpeg.AVFMT_FLAG_CUSTOM_IO;
        fmt_ctx->pb = avio_ctx;
        int ret = ffmpeg.avformat_open_input(&fmt_ctx, "", null, null);
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

        ffmpeg.av_dump_format(fmt_ctx, 0, infilename, 0);
    }
}
