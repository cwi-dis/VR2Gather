using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FFmpeg.AutoGen;
using VRT.Core;
using Cwipc;

namespace VRT.Video
{
    public unsafe class VideoFilter
    {
        SwsContext* swsFilterContext;
        byte_ptrArray4 tmpDataArray;
        int_array4 tmpLineSizeArray;
        byte* pictureFrameData;
        int width;
        int height;
        int[] srcStride;

        public VideoFilter(int width, int height, AVPixelFormat source, AVPixelFormat target)
        {
            if (VRTConfig.Instance.ffmpegDLLDir != "")
            {
                FFmpeg.AutoGen.ffmpeg.RootPath = VRTConfig.Instance.ffmpegDLLDir;
            }
            srcStride = new int[] { ffmpeg.av_image_get_buffer_size(source, width, 1, 1) };
            int num_bytes = ffmpeg.av_image_get_buffer_size(target, width, height, 1);
            pictureFrameData = (byte*)ffmpeg.av_malloc((ulong)num_bytes);
            ffmpeg.av_image_fill_arrays(ref tmpDataArray, ref tmpLineSizeArray, pictureFrameData, target, width, height, 1);
            swsFilterContext = ffmpeg.sws_getContext(width, height, source, width, height, target, 0, null, null, null);
            this.width = width;
            this.height = height;
        }

        public NativeMemoryChunk Process(System.IntPtr srcSlice)
        {
            int ret = ffmpeg.sws_scale(swsFilterContext, new byte*[] { (byte*)srcSlice }, srcStride, 0, height, tmpDataArray, tmpLineSizeArray);
            NativeMemoryChunk videoData = new NativeMemoryChunk(tmpLineSizeArray[0] * height);
            System.Buffer.MemoryCopy(tmpDataArray[0], (byte*)videoData.pointer, videoData.length, videoData.length);
            return videoData;
        }

        public bool Process(byte*[] srcSlice, ref byte_ptrArray8 dst, ref int_array8 dstStride)
        {
            int ret = ffmpeg.sws_scale(swsFilterContext, srcSlice, srcStride, 0, height, dst, dstStride);
            return ret >= 0;
        }

        public int GetLineSize()
        {
            return srcStride[0];
        }
    }
}