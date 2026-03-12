using Cwipc;
using VRT.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VRT.Core.VRTConfig.RepresentationConfigType;
using static VRT.Core.VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType;
using System;

namespace VRT.UserRepresentation.PointCloud
{
    public class  PointCloudCapturerFactory
    {
        public static AsyncPointCloudReader Create(VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType config, QueueThreadSafe selfPreparerQueue, QueueThreadSafe encoderQueue) { 
            string configFilename = config.CameraConfig.configFilename;
            if (!string.IsNullOrEmpty(configFilename))
            {
                configFilename = VRTConfig.ConfigFilename(configFilename, allowSearch:true, label:"Cameraconfig");
            }
            switch(config.variant)
            {
                case VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.RepresentationPointcloudVariant.camera:
                    return new AsyncCameraReader(configFilename, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.RepresentationPointcloudVariant.synthetic:
                   return new AsyncSyntheticReader(config.frameRate, config.SyntheticConfig.nPoints, selfPreparerQueue, encoderQueue);
                case VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.RepresentationPointcloudVariant.prerecorded:
                    var prConfig = config.PrerecordedConfig;
                    if (prConfig.folder == null || prConfig.folder == "")
                    {
                        throw new System.Exception($"PointCloudCapturerFactory: missing self-user RepresentationPointcloudConfig.PrerecordedConfig.folder config");
                    }
                    string prerecordedFolder = VRTConfig.ConfigFilename(prConfig.folder, allowSearch:true, label:"Precorded pointcloud folder");
                    Debug.Log($"prConfig.folder: {prerecordedFolder}");
                    if (!System.IO.Directory.Exists(prerecordedFolder))
                    {
                        throw new System.Exception($"PointCloudCapturerFactory: folder {prerecordedFolder} does not exist");
                    }
                    return new AsyncPrerecordedReader(prerecordedFolder, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.RepresentationPointcloudVariant.proxy:
                    var ProxyReaderConfig = config.ProxyConfig;
                    return new ProxyReader(ProxyReaderConfig.localIP, ProxyReaderConfig.port, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                case VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.RepresentationPointcloudVariant.remote:
                    var rcConfig = config.RemoteConfig;
                    return new AsyncNetworkCaptureReader(rcConfig.url, rcConfig.isCompressed, selfPreparerQueue, encoderQueue);
                case VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.RepresentationPointcloudVariant.developer:
                    try
                    {
                        return new AsyncCameraReader(configFilename, config.voxelSize, config.frameRate, selfPreparerQueue, encoderQueue);
                    }
                    #pragma warning disable CS0168
                    catch (Exception e)
                    {
                        Debug.LogWarning("PointCloudCapturerFactory: fallback to synthetic capturer");
                        return new AsyncSyntheticReader(config.frameRate, config.SyntheticConfig.nPoints, selfPreparerQueue, encoderQueue);
                    }
                default:
                    throw new System.Exception($"PointCloudCapturerFactory: bad variant {config.variant}");
             }
        }
    }
}
