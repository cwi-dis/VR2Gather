using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;

public class FFMpegDecoder : MonoBehaviour
{
    private IntPtr pFormatContext;
    private FFmpeg.AVFormatContext formatContext;

    private FFmpeg.AVCodecContext videoCodecContext;
    private IntPtr pVideoCodecContext;

    private FFmpeg.AVFrame videoFrame;
    private IntPtr pVideoFrame;

    private FFmpeg.AVCodec videoCodec;
    private IntPtr pVideoCodec;

    private IntPtr pVideoStream;

    private FFmpeg.AVCodecContext audioCodecContext;
    private IntPtr pAudioCodecContext;

    private FFmpeg.AVRational timebase;
    private IntPtr pAudioStream;

    private FFmpeg.AVCodec audioCodec;
    private IntPtr pAudioCodec;

    private readonly String path;
    private int videoStartIndex = -1;
    private int videoSampleRate;
    private int audioStartIndex = -1;
    private int audioSampleRate;
    private int format;
    private const int VIDEO_FRAME_SIZE = 5000;
    private const int AUDIO_FRAME_SIZE = 5000;
    private byte[] audioSamples = new byte[AUDIO_FRAME_SIZE];
    private float[] videoSamples = new float[VIDEO_FRAME_SIZE];
    private int sampleSize = -1;
    private bool isAudioStream = false;
    private bool isVideoStream = false;

    private const int TIMESTAMP_BASE = 1000000;

    public FFMpegDecoder() {
        FFmpeg.av_register_all();
    }

    public FFMpegDecoder(string url) {
        FFmpeg.av_register_all();
        Open(url);
    }

    ~FFMpegDecoder() {
        if (pFormatContext != IntPtr.Zero)
            FFmpeg.av_close_input_file(pFormatContext);
        FFmpeg.av_free_static();
    }

    public void Reset() {
        if (pFormatContext != IntPtr.Zero)
            FFmpeg.av_close_input_file(pFormatContext);
        sampleSize = -1;
        audioStartIndex = -1;
    }

    public bool Open(string path) {
        Reset();

        int ret;
        ret = FFmpeg.av_open_input_file(out pFormatContext, path, IntPtr.Zero, 0, IntPtr.Zero);

        if (ret < 0) {
            Console.WriteLine("couldn't open input file");
            return false;
        }

        ret = FFmpeg.av_find_stream_info(pFormatContext);

        if (ret < 0) {
            Console.WriteLine("couldn't find stream information");
            return false;
        }

        formatContext = (FFmpeg.AVFormatContext)
            Marshal.PtrToStructure(pFormatContext, typeof(FFmpeg.AVFormatContext));

        for (int i = 0; i < formatContext.nb_streams; ++i) {
            FFmpeg.AVStream stream = (FFmpeg.AVStream)
                   Marshal.PtrToStructure(formatContext.streams[i], typeof(FFmpeg.AVStream));

            FFmpeg.AVCodecContext codec = (FFmpeg.AVCodecContext)
                   Marshal.PtrToStructure(stream.codec, typeof(FFmpeg.AVCodecContext));

            if (codec.codec_type == FFmpeg.CodecType.CODEC_TYPE_AUDIO && audioStartIndex == -1) {
                this.pAudioCodecContext = stream.codec;
                this.pAudioStream = formatContext.streams[i];
                this.audioCodecContext = codec;
                this.audioStartIndex = i;
                this.timebase = stream.time_base;

                pAudioCodec = FFmpeg.avcodec_find_decoder(this.audioCodecContext.codec_id);
                if (pAudioCodec == IntPtr.Zero) {
                    Console.WriteLine("couldn't find codec");
                    return false;
                }

                FFmpeg.avcodec_open(stream.codec, pAudioCodec);
            }
            else if (codec.codec_type == FFmpeg.CodecType.CODEC_TYPE_VIDEO && videoStartIndex == -1) {
                this.pVideoCodec = stream.codec;
                this.pVideoStream = formatContext.streams[i];
                this.videoCodecContext = codec;
                this.videoStartIndex = i;

                pVideoCodec = FFmpeg.avcodec_find_decoder(this.videoCodecContext.codec_id);
                if (pVideoCodec == IntPtr.Zero) {
                    Console.WriteLine("couldn't find codec");
                    return false;
                }

                FFmpeg.avcodec_open(stream.codec, pVideoCodec);
            }
        }

        if (audioStartIndex == -1) {
            Console.WriteLine("Couldn't find audio stream");
            return false;
        }
        if (videoStartIndex == -1) {
            Console.WriteLine("Couldn't find video stream");
            return false;
        }

        audioSampleRate = audioCodecContext.sample_rate;
        videoSampleRate = videoCodecContext.sample_rate;

        return true;
    }

    static int count = 0;
    public bool Stream(byte[] inData, int inCount, float[] outData) {
        int result;

        IntPtr pPacket = Marshal.AllocHGlobal(inData.Length);
        Marshal.Copy(inData, 0, pPacket, inData.Length);

        result = FFmpeg.av_read_frame(pFormatContext, pPacket);
        if (result < 0)
            return false;
        count++;

        int frameSize = 0;
        IntPtr pAVFrame = IntPtr.Zero;
        FFmpeg.AVPacket packet = (FFmpeg.AVPacket)
                            Marshal.PtrToStructure(pPacket, typeof(FFmpeg.AVPacket));
        Marshal.FreeHGlobal(pPacket);

        if (packet.stream_index != this.audioStartIndex) {
            this.isAudioStream = false;
            return true;
        }
        this.isAudioStream = true;
        if (packet.stream_index != this.videoStartIndex) {
            this.isVideoStream = false;
            return true;
        }
        this.isVideoStream = true;

        try {
            pAVFrame = FFmpeg.avcodec_alloc_frame();

            int size = FFmpeg.avcodec_decode_video(pAudioCodecContext, pAVFrame,
                    ref frameSize, packet.data, packet.size);

            this.sampleSize = frameSize;
            Marshal.Copy(pAVFrame, outData, 0, VIDEO_FRAME_SIZE);
        }
        catch (Exception e) {
            Console.WriteLine(e.Message);
            return false;
        }
        finally {
            Marshal.FreeHGlobal(pAVFrame);
        }

        return true;
    }

    public float[] Samples {
        get { return videoSamples; }
    }

    public int SampleSize {
        get { return sampleSize; }
    }

    public int Format {
        get { return format; }
    }

    public int Frequency {
        get { return audioSampleRate; }
    }

    public bool IsAudioStream {
        get { return isAudioStream; }
    }

    public bool IsVideoStream {
        get { return isVideoStream; }
    }
}
