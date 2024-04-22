# CWI Point Cloud Unity Package

This package allows capture and display of
point clouds and various operations such as compression for transmission, reading and writing to disk, etc.

The primary use case is live point clouds of humans: capture a volumetric video representation of yourself, live, and use that in stead of an avatar.

Capturing can be done using Intel Realsense cameras, Microsoft Azure Kinect cameras and there is preliminary support for capturing using the depth camera from an Android mobile phone. Point cloud streams can also be read from disk, for example from the cwipc-sxr ddataset or the 8i dataset. Finally there is a _synthetic_ pointcloud generator that allows you to have an approximately human-sized point cloud prepresentation with no addinional hardware.

The capturer (or actually the underlying `cwipc` library) supports using multiple depth cameras to create a fused point cloud, which could therefore be full 360 degress if you have more than 3 cameras.

After capturing the point cloud stream can be displayed locally, for example for self-view in a VR application.

The point cloud stream can also be compressed and transmitted to another Unity instance, where it can be decompressed and rendered. This allows creation of fully immersive VR experiences where participants are "themselves" in stead of avatars.

Optionally the point cloud streams can be tiled and compressed at multiple quality levels. This allows the receiver to only select the tiles that are visible to the user, or select certain tiles at lower qualities. This leads to much lower bandwidth, CPU and GPU usage with limited loss of visual quality.

## Shortcomings

This is a first beta release, mainly to show you what is possible. It is probably not ready for production yet, unless you already have a full application framework in place where you could drop this in.

Things we know are missing, which we will add in subsequent releases:

- Currently the only supported protocol is a direct TCP connection. No firewall traversal, no fan-out to multiple receivers. We plan to make socketio, probably HTTP DASH and possibly WebRTC available in the future.

  In the mean time, the API interfaces are abstract enough that if you already have a transport protocol it is probably easy to drop in.
- No audio, but you may have that in your framework. We will make something available in the future.
- No synchronizer to ensure that tiled streams are played out synchronously (with each other, and with audio). Planned for a future release.
- No viewer-side tile selector. Planned for a future release.
- No included native plugin DLLs. Planned for a future release.

## System Requirements

The package works on Windows, MacOS and Linux. Android support (specifically for the Oculus Quest) is under development.

At the moment the package does not contain the native libraries and runtime packages needed, these need to be installed separately on your system. See <http://github.com/cwipc> for full instructions, but generally speaking you need to install the following software:

- cwipc
- libpcl
- librealsense2 (if you want to use a RealSense camera)
- k4a  and k4abt (if you want to use an Azure Kinect camera, unsupported on MacOS)

## Samples

The best place to start is the samples:

- `SimplePointCloudDisplay` is a simple scene that renders a dynmic point cloud stream. By default it shows the synthetic point cloud, but it can also show a live stream from a camera, a prerecorded stream from disk or a stream received over the net.

- `SimpleSession` is a two-user scene. User one is the local user, a self view is shown and the point clouds for this user are transmitted over the net. The user two point clouds are received from the net and also rendered. You can run this scene on two machines connected to the same local network and specify the two hostnames (by editing the `controller` in the scene).

  It is also possible to run the scene on a single machine, then `localhost` is used for sending and receiving and you will simply see two copies of yourself, one as self-view and one a little bit away (and with a small delay).
  
- `TiledSession` is an extended version of `SimpleSession` which will send tiled streams.

## Prefabs

A number of prefabs are available to drop into your scene:

- `cwipc_display` is an object to display a point cloud stream. Usage is shown in the `SimplePointCloudDisplay` sample.
- `cwipc_avatar_self_simple` and `cwipc_avatar_other_simple` are the prefabs for `SimpleSession`. The first contains unity camera, capturer, self view, compression and transmission. The second contains reception, decompression and display. These require some sort of a session controller to initialize them, see for example `SampleTwoUserSessionController.cs`.
- `cwipc_avatar_self` and `cwipc_avatar_other` are similar in structure but used tiled streams.

## Main scripts

- `AbstractPointCloudSource` and its subclasses `KinectPointCloudReader`, `RealsensePointCloudReader`, `SyntheticPointCloudReader` and `PrerecordedPointCloudReader` are the capturers.
- `AsyncPointCloudReader` and `AsyncPointCloudWriter` are the transmission and reception modules. One implementation is provided in this package, `AsyncTCPReader` and `AsyncTCPWriter` which transmit streams over a simple direct TCP conntection. We have code for more useful transport mechanisms such as socketio or HTTP DASH, but we are still working on ways to make these available as open source.
- `AbstractPointCloudEncoder`, `AbstractPointCloudDecoder` and its subclasses `AsyncPCEncoder`, `AsyncPCDecoder`, `AynsPCNullEncoder` and `AsyncPCNullDecoder` are the encoders and decoders. The first two use the MPEG Anchor point cloud codec, the last two use no compression, just serialization.
- `AsyncPointCloudPreparer` prepares a point cloud stream for a point cloud renderer. 
- Finally `PointCloudRenderer` shows the point cloud stream, using the `PointCloud.shader` shader.
