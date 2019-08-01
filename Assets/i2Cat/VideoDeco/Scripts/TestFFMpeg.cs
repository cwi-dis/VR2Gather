using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using UnityEngine.Windows;

public unsafe class TestFFMpeg : MonoBehaviour {

    private const int INBUF_SIZE = 4096;
    private const long AV_NOPTS_VALUE = 0x800000000000000;

    public string infilename;
    public string outfilename;

    AVCodec* codec;
    AVCodecParserContext* parser;
    AVCodecContext* c;
    AVPacket* pkt;
    AVFrame* frame;

    // Start is called before the first frame update
    void Start() {

        FFmpegInvoke.av_register_all();

        AVDictionary* dic;
        
        codec = FFmpegInvoke.avcodec_find_decoder(AVCodecID.CODEC_ID_MPEG1VIDEO);
        c = FFmpegInvoke.avcodec_alloc_context3(codec);
        FFmpegInvoke.avcodec_open2(c, codec, &dic);
        frame = FFmpegInvoke.avcodec_alloc_frame();
    }

    void Update() {
        try {
            using (var stream = new FileStream(infilename, FileMode.Open, FileAccess.Read)) {
                byte[] bytes = new byte[stream.Length];
                int numBytesToRead = (int)stream.Length;
                int numBytesRead = 0;

                #region WithoutDecoder
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
                #endregion

                #region WithDecoder
                #endregion

                using (FileStream fsNew = new FileStream(outfilename, FileMode.Create, FileAccess.Write)) {
                    fsNew.Write(bytes, 0, numBytesToRead);
                }
            }
        }
        catch (FileNotFoundException ioEx) {
            Debug.Log(ioEx.Message);
        }
    }

    void Decode(byte* pData, int sz) {
        AVPacket packet;
        FFmpegInvoke.av_init_packet(&packet);

        packet.data = pData;
        packet.size = sz;
        int framefinished = 0;
        int nres = FFmpegInvoke.avcodec_decode_video2(c, frame, &framefinished, &packet);

        if (framefinished != 0) {
            // do the yuv magic and call a consumer
        }

        return;
    }
}
