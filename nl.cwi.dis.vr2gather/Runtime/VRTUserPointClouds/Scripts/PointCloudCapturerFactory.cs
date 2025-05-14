using Cwipc;
using VRT.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VRT.Core.VRTConfig._User;
using static VRT.Core.VRTConfig._User._PCSelfConfig;
using System;

namespace VRT.UserRepresentation.PointCloud
{
    public class  PointCloudCapturerFactory
    {
        public static AsyncPointCloudReader Create(VRTConfig._User._PCSelfConfig config, QueueThreadSafe selfPreparerQueue, QueueThreadSafe encoderQueue) { 
            string configFilename = config.CameraReaderConfig.configFilename;
            if (!string.IsNullOrEmpty(configFilename))
            {
                configFilename = VRTConfig.ConfigFilename(configFilename);
            }
            switch(config.capturerType)
            {
                case VRTConfig._User._PCSelfConfig.PCCapturerType.auto:
                    return new AsyncAutoReader(configFilename, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig._User._PCSelfConfig.PCCapturerType.synthetic:
                   return new AsyncSyntheticReader(config.frameRate, config.SynthReaderConfig.nPoints, selfPreparerQueue, encoderQueue);
                case VRTConfig._User._PCSelfConfig.PCCapturerType.kinect:
                    return new AsyncKinectReader(configFilename, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig._User._PCSelfConfig.PCCapturerType.realsense:
                    return new AsyncRealsenseReader(configFilename, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig._User._PCSelfConfig.PCCapturerType.prerecorded:
                    var prConfig = config.PrerecordedReaderConfig;
                    if (prConfig.folder == null || prConfig.folder == "")
                    {
                        throw new System.Exception($"PointCloudCapturerFactory: missing self-user PCSelfConfig.PrerecordedReaderConfig.folder config");
                    }
                    string prerecordedFolder = VRTConfig.ConfigFilename(prConfig.folder);
                    Debug.Log($"prConfig.folder: {prerecordedFolder}");
                    if (!System.IO.Directory.Exists(prerecordedFolder))
                    {
                        throw new System.Exception($"PointCloudCapturerFactory: folder {prerecordedFolder} does not exist");
                    }
                    return new AsyncPrerecordedReader(prerecordedFolder, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig._User._PCSelfConfig.PCCapturerType.proxy:
                    var ProxyReaderConfig = config.ProxyReaderConfig;
                    return new ProxyReader(ProxyReaderConfig.localIP, ProxyReaderConfig.port, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig._User._PCSelfConfig.PCCapturerType.remote:
                    var rcConfig = config.RemoteCameraReaderConfig;
                    return new AsyncNetworkCaptureReader(rcConfig.url, rcConfig.isCompressed, selfPreparerQueue, encoderQueue);
                case VRTConfig._User._PCSelfConfig.PCCapturerType.developer:
                    try
                    {
                        return new AsyncAutoReader(configFilename, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                    }
                    #pragma warning disable CS0168
                    catch (Exception e)
                    {
                        Debug.LogWarning("PointCloudCapturerFactory: fallback to synthetic capturer");
                        return new AsyncSyntheticReader(config.frameRate, config.SynthReaderConfig.nPoints, selfPreparerQueue, encoderQueue);
                    }
                default:
                    throw new System.Exception($"PointCloudCapturerFactory: bad capturerType {config.capturerType}");
             }
        }
    }
}
