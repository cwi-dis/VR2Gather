using Cwipc;
using VRT.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VRT.Core.VRTConfig._Representation;
using static VRT.Core.VRTConfig._Representation._PointcloudRepresentationConfig;
using System;

namespace VRT.UserRepresentation.PointCloud
{
    public class  PointCloudCapturerFactory
    {
        public static AsyncPointCloudReader Create(VRTConfig._Representation._PointcloudRepresentationConfig config, QueueThreadSafe selfPreparerQueue, QueueThreadSafe encoderQueue) { 
            string configFilename = config.CameraReaderConfig.configFilename;
            if (!string.IsNullOrEmpty(configFilename))
            {
                configFilename = VRTConfig.ConfigFilename(configFilename, allowSearch:true, label:"Cameraconfig");
            }
            switch(config.capturerType)
            {
                case VRTConfig._Representation._PointcloudRepresentationConfig.PCCapturerType.camera:
                    return new AsyncCameraReader(configFilename, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig._Representation._PointcloudRepresentationConfig.PCCapturerType.synthetic:
                   return new AsyncSyntheticReader(config.frameRate, config.SynthReaderConfig.nPoints, selfPreparerQueue, encoderQueue);
                case VRTConfig._Representation._PointcloudRepresentationConfig.PCCapturerType.prerecorded:
                    var prConfig = config.PrerecordedReaderConfig;
                    if (prConfig.folder == null || prConfig.folder == "")
                    {
                        throw new System.Exception($"PointCloudCapturerFactory: missing self-user PointcloudRepresentationConfig.PrerecordedReaderConfig.folder config");
                    }
                    string prerecordedFolder = VRTConfig.ConfigFilename(prConfig.folder, allowSearch:true, label:"Precorded pointcloud folder");
                    Debug.Log($"prConfig.folder: {prerecordedFolder}");
                    if (!System.IO.Directory.Exists(prerecordedFolder))
                    {
                        throw new System.Exception($"PointCloudCapturerFactory: folder {prerecordedFolder} does not exist");
                    }
                    return new AsyncPrerecordedReader(prerecordedFolder, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig._Representation._PointcloudRepresentationConfig.PCCapturerType.proxy:
                    var ProxyReaderConfig = config.ProxyReaderConfig;
                    return new ProxyReader(ProxyReaderConfig.localIP, ProxyReaderConfig.port, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig._Representation._PointcloudRepresentationConfig.PCCapturerType.remote:
                    var rcConfig = config.RemoteCameraReaderConfig;
                    return new AsyncNetworkCaptureReader(rcConfig.url, rcConfig.isCompressed, selfPreparerQueue, encoderQueue);
                case VRTConfig._Representation._PointcloudRepresentationConfig.PCCapturerType.developer:
                    try
                    {
                        return new AsyncCameraReader(configFilename, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
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
