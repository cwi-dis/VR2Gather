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
    AVCodecContext* c;
    AVPacket* pkt;
    AVFrame* frame;

    // Start is called before the first frame update
    void Start() {
        pkt = ffmpeg.av_packet_alloc();
        codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_MPEG1VIDEO);
        parser = ffmpeg.av_parser_init((int)codec->id);
        c = ffmpeg.avcodec_alloc_context3(codec);
        ffmpeg.avcodec_open2(c, codec, null);
        frame = ffmpeg.av_frame_alloc();
       
    }

    void Update() {
        if (!finish) {
            #region WithoutDecoder
            if (!wDecoder) {
                try {
                    using (var stream = new FileStream(infilename, FileMode.Open, FileAccess.Read)) {
                        byte[] bytes = new byte[stream.Length];
                        int numBytesToRead = (int)stream.Length;
                        int numBytesRead = 0;

                        while (numBytesToRead > 0) {
                            // Read may return anything from 0 to numBytesToRead.
                            int n = stream.Read(bytes, numBytesRead, numBytesToRead);

                            // Break when the end of the file is reached.
                            if (n == 0)
                                break;

                            numBytesRead += n;
                            numBytesToRead -= n;
                        }
                        numBytesToRead = bytes.Length;

                        using (FileStream fsNew = new FileStream(outfilename, FileMode.Create, FileAccess.Write)) {
                            fsNew.Write(bytes, 0, numBytesToRead);
                        }
                    }
                    finish = true;
                }
                catch (FileNotFoundException ioEx) {
                    Debug.Log(ioEx.Message);
                }
            }
            #endregion
            #region WithDecoder
            else {
                try {
                    using (var stream = new FileStream(infilename, FileMode.Open, FileAccess.Read)) {
                        byte[] bytes = new byte[stream.Length];
                        int numBytesToRead = (int)stream.Length;
                        int numBytesRead = 0;
                        byte* data;
                        int data_size;
                        ffmpeg.av_init_packet(pkt);

                        while (numBytesToRead > 0) {
                            // Read may return anything from 0 to numBytesToRead.
                            int n = stream.Read(bytes, numBytesRead, numBytesToRead);
                                                        
                            // Break when the end of the file is reached.
                            if (n == 0)
                                break;

                            numBytesRead += n;
                            numBytesToRead -= n;
                        }
                        numBytesToRead = bytes.Length;

                        //DO DECODING STUFF

                        data = (byte*)Marshal.AllocHGlobal(bytes.Length);
                        Marshal.Copy(bytes, 0, (IntPtr)data, bytes.Length);
                        data_size = bytes.Length;

                        while (data_size > 0) {
                            int ret = ffmpeg.av_parser_parse2(parser, c, &pkt->data, &pkt->size, data, data_size, AV_NOPTS_VALUE, AV_NOPTS_VALUE, 0);
                            if (ret < 0) Debug.Log("Error while parsing");

                            data += ret;
                            data_size -= ret;

                            if (pkt->size > 0)
                                frame = DecodeFile();
                                //DecodeFile(c, frame, pkt, outfilename);
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
                }
                catch (FileNotFoundException ioEx) {
                    Debug.Log(ioEx.Message);
                }
            }
            #endregion
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

        int ret = ffmpeg.avcodec_send_packet(c, pkt);
        if (ret < 0) Debug.Log("Error sending a packet for decoding");

        while (ret >= 0) {
            ret = ffmpeg.avcodec_receive_frame(c, frame);
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF) return null;
            else if (ret < 0) Debug.Log("Error during decoding");

            Debug.Log("saving frame " + c->frame_number);

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
        }
        catch (Exception ex) {
            Debug.Log("Exception caught in process: " + ex);
        }
    }
}
