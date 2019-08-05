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

    AVCodec* codec;
    AVCodecParserContext* parser;
    AVCodecContext* c;
    AVPacket* pkt;
    AVFrame* frame;

    // Start is called before the first frame update
    void Start() {
        FFmpegInvoke.av_register_all();

        codec = FFmpegInvoke.avcodec_find_decoder(AVCodecID.CODEC_ID_MPEG1VIDEO);
        parser = FFmpegInvoke.av_parser_init((int)codec->id);
        c = FFmpegInvoke.avcodec_alloc_context3(codec);
        FFmpegInvoke.avcodec_open2(c, codec, null);
        frame = FFmpegInvoke.avcodec_alloc_frame();
    }

    void Update() {
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

                    fixed (byte* pBytes = bytes)

                        DecodeFile(pBytes, bytes.Length);

                    //using (FileStream fsNew = new FileStream(outfilename, FileMode.Create, FileAccess.Write)) {
                    //    fsNew.Write(bytes, 0, numBytesToRead);
                    //}
                }
            }
            catch (FileNotFoundException ioEx) {
                Debug.Log(ioEx.Message);
            }
        }
        #endregion
    }

    void DecodeStream(byte* pData, int sz) {
        AVPacket packet;
        FFmpegInvoke.av_init_packet(&packet);

        packet.data = pData;
        packet.size = sz;
        int framefinished = 0;
        int nres = FFmpegInvoke.avcodec_decode_video2(c, frame, &framefinished, &packet);

        byte[] arr = new byte[frame->height];
        Marshal.Copy((IntPtr)frame->data_0, arr, 0, frame->height);

        if (framefinished != 0) {
            // do the yuv magic and call a consumer
            try {
                using (var fs = new FileStream(outfilename, FileMode.Create, FileAccess.Write)) {
                    fs.Write(arr, 0, arr.Length);
                }
            }
            catch (Exception ex) {
                Console.WriteLine("Exception caught in process: {0}", ex);
                return;
            }
        }

        return;
    }

    void DecodeFile(byte* pData, int sz) {
        AVPacket packet;
        FFmpegInvoke.av_init_packet(&packet);

        packet.data = pData;
        packet.size = sz;
        int framefinished = 0;
        int nres = FFmpegInvoke.avcodec_decode_video2(c, frame, &framefinished, &packet);

        byte[] arr = new byte[frame->height];
        Marshal.Copy((IntPtr)frame->data_0, arr, 0, frame->height);

        if (framefinished != 0) {
            // do the yuv magic and call a consumer
            try {
                using (var fs = new FileStream(outfilename, FileMode.Create, FileAccess.Write)) {
                    fs.Write(arr, 0, arr.Length);
                }
            }
            catch (Exception ex) {
                Console.WriteLine("Exception caught in process: {0}", ex);
                return;
            }
        }

        return;
    }
}
