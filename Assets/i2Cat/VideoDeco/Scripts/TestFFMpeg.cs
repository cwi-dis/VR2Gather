using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;
using SharpFFmpeg;
using UnityEngine.Windows;

public class TestFFMpeg : MonoBehaviour {

    private const int INBUF_SIZE = 4096;
    private const long AV_NOPTS_VALUE = 0x800000000000000;

    public string infilename;
    public string outfilename;
    FileStream f;


    FFmpeg.AVCodec codec;
    IntPtr pCodec;
    FFmpeg.AVCodecParserContext parser;
    IntPtr pParser;
    FFmpeg.AVCodecContext c;
    IntPtr pC;
    FFmpeg.AVPacket pkt;
    IntPtr pPkt;
    FFmpeg.AVFrame frame;
    IntPtr pFrame;

    int[] inbuf = new int[INBUF_SIZE + FFmpeg.FF_INPUT_BUFFER_PADDING_SIZE];
    IntPtr data;
    int data_size;
    int ret;

    // Start is called before the first frame update
    void Start() {

        //FFmpeg.av_register_all();

        //pPkt = Marshal.AllocHGlobal(56);

        //pCodec = FFmpeg.avcodec_find_decoder(FFmpeg.CodecID.CODEC_ID_MPEG1VIDEO);
        //if (pCodec == null) {
        //    Debug.Log("Codec not found");
        //    return;
        //}

        //codec = (FFmpeg.AVCodec)Marshal.PtrToStructure(pCodec, typeof(FFmpeg.AVCodec));

        //int codecID = (int)codec.id;
        //pParser = FFmpeg.av_parser_init(codecID);
        //if (pParser == null) {
        //    Debug.Log("Parser not found");
        //    return;
        //}

        //pC = FFmpeg.avcodec_alloc_context();
        //if (pC == null) {
        //    Debug.Log("Couldn't allocate video codec context");
        //    return;
        //}

        //if (FFmpeg.avcodec_open(pC, pCodec) < 0) {
        //    Debug.Log("Couldn't open codec");
        //    return;
        //}

        //f = new FileStream(infilename, FileMode.Open, FileAccess.Read);
        //if (f == null) {
        //    Debug.Log("Couldn't open " + infilename);
        //    return;
        //}

        //pFrame = FFmpeg.avcodec_alloc_frame();
        //if (pFrame == null) {
        //    Debug.Log("Couldn't allocate video frame");
        //    return;
        //}

        //Debug.Log("End Start Success");
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
                //while (numBytesToRead > 0) {
                //    data_size = stream.Read(bytes, numBytesRead, numBytesToRead);
                //    if (data_size == 0)
                //        break;

                //    /* use the parser to split the data into frames */
                //    //data = inbuf;
                //    while (data_size > 0) {
                //        ret = FFmpeg.av_parser_parse(pParser, pC, pkt.data, ref pkt.size,
                //                               data, data_size, AV_NOPTS_VALUE, AV_NOPTS_VALUE);
                //        if (ret < 0) {
                //            Debug.Log("Error while parsing");
                //            return;
                //        }

                //        data += ret;
                //        data_size -= ret;

                //        if (pkt.size)
                //            decode(c, frame, pkt, outfilename);
                //    }

                //    numBytesRead += data_size;
                //    numBytesToRead -= data_size;
                //}
                //numBytesToRead = bytes.Length;
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
}
