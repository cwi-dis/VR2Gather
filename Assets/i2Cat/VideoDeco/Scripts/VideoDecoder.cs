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
                                        videoData = (System.IntPtr)tmpDataArray[0];
                                        videoDataSize = tmpLineSizeArray[0] * frame->height;
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

        /*
         * 
         * static void decode(AVCodecContext *dec_ctx, AVPacket *pkt, AVFrame *frame,
	FILE *outfile)
{
	int i, ch;
	int ret, data_size;
	//  send the packet with the compressed data to the decoder
        ret = avcodec_send_packet(dec_ctx, pkt);
	if (ret< 0) {
		fprintf(stderr, "Error submitting the packet to the decoder\n");
        exit(1);
    }
	//  read all the output frames (in general there may be any number of them
	while (ret >= 0) {
		ret = avcodec_receive_frame(dec_ctx, frame);
		if (ret == AVERROR(EAGAIN) || ret == AVERROR_EOF)
			return;
		else if (ret< 0) {
			fprintf(stderr, "Error during decoding\n");
    exit(1);
}
data_size = av_get_bytes_per_sample(dec_ctx->sample_fmt);
		if (data_size< 0) {
			//  This should not occur, checking just for paranoia 
			fprintf(stderr, "Failed to calculate data size\n");
exit(1);
		}
		for (i = 0; i<frame->nb_samples; i++)
			for (ch = 0; ch<dec_ctx->channels; ch++)
				fwrite(frame->data[ch] + data_size* i, 1, data_size, outfile);
	}
}
int main(int argc, char** argv) {
    const char* outfilename, *filename;
    const AVCodec* codec;
    AVCodecContext* c = NULL;
    AVCodecParserContext* parser = NULL;
    int len, ret;
    FILE* f, *outfile;
    uint8_t inbuf[AUDIO_INBUF_SIZE + AV_INPUT_BUFFER_PADDING_SIZE];
    uint8_t* data;
    size_t data_size;
    AVPacket* pkt;
    AVFrame* decoded_frame = NULL;
    if (argc <= 2) {
        fprintf(stderr, "Usage: %s <input file> <output file>\n", argv[0]);
        exit(0);
    }
    filename = argv[1];
    outfilename = argv[2];
    pkt = av_packet_alloc();
    //  find the MPEG audio decoder
    codec = avcodec_find_decoder(AV_CODEC_ID_MP2);
    if (!codec) {
        fprintf(stderr, "Codec not found\n");
        exit(1);
    }
    parser = av_parser_init(codec->id);
    if (!parser) {
        fprintf(stderr, "Parser not found\n");
        exit(1);
    }
    c = avcodec_alloc_context3(codec);
    if (!c) {
        fprintf(stderr, "Could not allocate audio codec context\n");
        exit(1);
    }
    //  open it 
    if (avcodec_open2(c, codec, NULL) < 0) {
        fprintf(stderr, "Could not open codec\n");
        exit(1);
    }
    f = fopen(filename, "rb");
    if (!f) {
        fprintf(stderr, "Could not open %s\n", filename);
        exit(1);
    }
    outfile = fopen(outfilename, "wb");
    if (!outfile) {
        av_free(c);
        exit(1);
    }
    // decode until eof 
    data = inbuf;
    data_size = fread(inbuf, 1, AUDIO_INBUF_SIZE, f);
    while (data_size > 0) {
        if (!decoded_frame) {
            if (!(decoded_frame = av_frame_alloc())) {
                fprintf(stderr, "Could not allocate audio frame\n");
                exit(1);
            }
        }
        ret = av_parser_parse2(parser, c, &pkt->data, &pkt->size,
            data, data_size,
            AV_NOPTS_VALUE, AV_NOPTS_VALUE, 0);
        if (ret < 0) {
            fprintf(stderr, "Error while parsing\n");
            exit(1);
        }
        data += ret;
        data_size -= ret;
        if (pkt->size)
            decode(c, pkt, decoded_frame, outfile);
        if (data_size < AUDIO_REFILL_THRESH) {
            memmove(inbuf, data, data_size);
            data = inbuf;
            len = fread(data + data_size, 1,
                AUDIO_INBUF_SIZE - data_size, f);
            if (len > 0)
                data_size += len;
        }
    }
    // flush the decoder
    pkt->data = NULL;
    pkt->size = 0;
    decode(c, pkt, decoded_frame, outfile);
    fclose(outfile);
    fclose(f);
    avcodec_free_context(&c);
    av_parser_close(parser);
    av_frame_free(&decoded_frame);
    av_packet_free(&pkt);
    return 0;
}
         * 
         * 
         * 
         * 
         * 
// Resampling audio

#include <libavutil/opt.h>
#include <libavutil/channel_layout.h>
#include <libavutil/samplefmt.h>
#include <libswresample/swresample.h>
static int get_format_from_sample_fmt(const char **fmt,
                                      enum AVSampleFormat sample_fmt)
{
    int i;
    struct sample_fmt_entry {
        enum AVSampleFormat sample_fmt; const char *fmt_be, *fmt_le;
    } sample_fmt_entries[] = {
        { AV_SAMPLE_FMT_U8,  "u8",    "u8"    },
        { AV_SAMPLE_FMT_S16, "s16be", "s16le" },
        { AV_SAMPLE_FMT_S32, "s32be", "s32le" },
        { AV_SAMPLE_FMT_FLT, "f32be", "f32le" },
        { AV_SAMPLE_FMT_DBL, "f64be", "f64le" },
    };
    *fmt = NULL;
    for (i = 0; i < FF_ARRAY_ELEMS(sample_fmt_entries); i++) {
        struct sample_fmt_entry *entry = &sample_fmt_entries[i];
        if (sample_fmt == entry->sample_fmt) {
            *fmt = AV_NE(entry->fmt_be, entry->fmt_le);
            return 0;
        }
    }
    fprintf(stderr,
            "Sample format %s not supported as output format\n",
            av_get_sample_fmt_name(sample_fmt));
    return AVERROR(EINVAL);
}
// Fill dst buffer with nb_samples, generated starting from t.
        static void fill_samples(double* dst, int nb_samples, int nb_channels, int sample_rate, double* t) {
            int i, j;
            double tincr = 1.0 / sample_rate, *dstp = dst;
            const double c = 2 * M_PI * 440.0;
            // generate sin tone with 440Hz frequency and duplicated channels 
            for (i = 0; i < nb_samples; i++) {
                *dstp = sin(c * *t);
                for (j = 1; j < nb_channels; j++)
                    dstp[j] = dstp[0];
                dstp += nb_channels;
                *t += tincr;
            }
        }
        int main(int argc, char** argv) {
            int64_t src_ch_layout = AV_CH_LAYOUT_STEREO, dst_ch_layout = AV_CH_LAYOUT_SURROUND;
            int src_rate = 48000, dst_rate = 44100;
            uint8_t** src_data = NULL, **dst_data = NULL;
            int src_nb_channels = 0, dst_nb_channels = 0;
            int src_linesize, dst_linesize;
            int src_nb_samples = 1024, dst_nb_samples, max_dst_nb_samples;
    enum AVSampleFormat src_sample_fmt = AV_SAMPLE_FMT_DBL, dst_sample_fmt = AV_SAMPLE_FMT_S16;
    const char* dst_filename = NULL;
        FILE* dst_file;
        int dst_bufsize;
        const char* fmt;
        struct SwrContext *swr_ctx;
    double t;
        int ret;
    if (argc != 2) {
        fprintf(stderr, "Usage: %s output_file\n"
                "API example program to show how to resample an audio stream with libswresample.\n"
                "This program generates a series of audio frames, resamples them to a specified "
                "output format and rate and saves them to an output file named output_file.\n",
            argv[0]);
        exit(1);
    }
    dst_filename = argv[1];
    dst_file = fopen(dst_filename, "wb");
    if (!dst_file) {
        fprintf(stderr, "Could not open destination file %s\n", dst_filename);
    exit(1);
}
// create resampler context
swr_ctx = swr_alloc();
    if (!swr_ctx) {
        fprintf(stderr, "Could not allocate resampler context\n");
ret = AVERROR(ENOMEM);
        goto end;
    }
    // set options
    av_opt_set_int(swr_ctx, "in_channel_layout", src_ch_layout, 0);
    av_opt_set_int(swr_ctx, "in_sample_rate", src_rate, 0);
    av_opt_set_sample_fmt(swr_ctx, "in_sample_fmt", src_sample_fmt, 0);
    av_opt_set_int(swr_ctx, "out_channel_layout", dst_ch_layout, 0);
    av_opt_set_int(swr_ctx, "out_sample_rate", dst_rate, 0);
    av_opt_set_sample_fmt(swr_ctx, "out_sample_fmt", dst_sample_fmt, 0);
    // initialize the resampling context
    if ((ret = swr_init(swr_ctx)) < 0) {
        fprintf(stderr, "Failed to initialize the resampling context\n");
        goto end;
    }
    // allocate source and destination samples buffers 
    src_nb_channels = av_get_channel_layout_nb_channels(src_ch_layout);
    ret = av_samples_alloc_array_and_samples(&src_data, &src_linesize, src_nb_channels,
                                         src_nb_samples, src_sample_fmt, 0);
    if (ret< 0) {
        fprintf(stderr, "Could not allocate source samples\n");
        goto end;
    }
    // compute the number of converted samples: buffering is avoided
    // ensuring that the output buffer will contain at least all the
    // converted input samples 
    max_dst_nb_samples = dst_nb_samples =
        av_rescale_rnd(src_nb_samples, dst_rate, src_rate, AV_ROUND_UP);
    // buffer is going to be directly written to a rawaudio file, no alignment 
    dst_nb_channels = av_get_channel_layout_nb_channels(dst_ch_layout);
    ret = av_samples_alloc_array_and_samples(&dst_data, &dst_linesize, dst_nb_channels,
                                         dst_nb_samples, dst_sample_fmt, 0);
    if (ret< 0) {
        fprintf(stderr, "Could not allocate destination samples\n");
        goto end;
    }
    t = 0;
    do {
        // generate synthetic audio 
        fill_samples((double*) src_data[0], src_nb_samples, src_nb_channels, src_rate, &t);
        // compute destination number of samples
        dst_nb_samples = av_rescale_rnd(swr_get_delay(swr_ctx, src_rate) +
                                src_nb_samples, dst_rate, src_rate, AV_ROUND_UP);
        if (dst_nb_samples > max_dst_nb_samples) {
            av_freep(&dst_data[0]);
        ret = av_samples_alloc(dst_data, &dst_linesize, dst_nb_channels,
                                dst_nb_samples, dst_sample_fmt, 1);
            if (ret< 0)
                break;
            max_dst_nb_samples = dst_nb_samples;
        }
        // convert to destination format
        ret = swr_convert(swr_ctx, dst_data, dst_nb_samples, (const uint8_t**)src_data, src_nb_samples);
        if (ret< 0) {
            fprintf(stderr, "Error while converting\n");
            goto end;
        }
        dst_bufsize = av_samples_get_buffer_size(&dst_linesize, dst_nb_channels,
                                                 ret, dst_sample_fmt, 1);
        if (dst_bufsize< 0) {
            fprintf(stderr, "Could not get sample buffer size\n");
            goto end;
        }
        printf("t:%f in:%d out:%d\n", t, src_nb_samples, ret);
        fwrite(dst_data[0], 1, dst_bufsize, dst_file);
    } while (t< 10);
    if ((ret = get_format_from_sample_fmt(&fmt, dst_sample_fmt)) < 0)
        goto end;
    fprintf(stderr, "Resampling succeeded. Play the output file with the command:\n"
            "ffplay -f %s -channel_layout %"PRId64" -channels %d -ar %d %s\n",
            fmt, dst_ch_layout, dst_nb_channels, dst_rate, dst_filename);
end:
    if (dst_file)
        fclose(dst_file);
    if (src_data)
        av_freep(&src_data[0]);
    av_freep(&src_data);
    if (dst_data)
        av_freep(&dst_data[0]);
    av_freep(&dst_data);
    swr_free(&swr_ctx);
    return ret< 0;
}
         * 
         * 
         * 
         * */


    }
}
