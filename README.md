# Test Bed Unity Project.

This repository will keep a bunch of unit test to check all the components developed within the VRTogether consortium.

This repository also contain the end to end solution with the integration of all the components working together.

It currently uses **Unity 2019.3.9f1** and runs on Windows 10.

## Installation
For the installation and the correct usage of this project must be followed the next steps:

-   Install **Intel RealSense2 SDK v2.25.0**. [https://github.com/IntelRealSense/librealsense](https://github.com/IntelRealSense/librealsense)
    
-   Install **PCL 1.8**.  [http://pointclouds.org/downloads/](http://pointclouds.org/downloads/)
    
-   Install **libjpeg-turbo**. [https://libjpeg-turbo.org](https://libjpeg-turbo.org/)
    
-   Download **cwipc_util v2.7**  [https://baltig.viaccess-orca.com:8443/VRT/nativeclient-group/cwipc_util/releases](https://baltig.viaccess-orca.com:8443/VRT/nativeclient-group/cwipc_util/-/releases)
    
-   Download **cwipc_codec v2.7**  [https://baltig.viaccess-orca.com:8443/VRT/nativeclient-group/cwipc_codec/releases](https://baltig.viaccess-orca.com:8443/VRT/nativeclient-group/cwipc_util/-/releases)
    
-   Download **cwipc_realsense2 v2.7**  [https://baltig.viaccess-orca.com:8443/VRT/nativeclient-group/cwipc_realsense2/releases](https://baltig.viaccess-orca.com:8443/VRT/nativeclient-group/cwipc_util/-/releases)

-   Download **SUB v51**  [https://baltig.viaccess-orca.com:8443/VRT/nativeclient-group/SUB/releases](https://baltig.viaccess-orca.com:8443/VRT/nativeclient-group/SUB/releases)
    
-   Download **Bin2Dash v42**  [https://baltig.viaccess-orca.com:8443/VRT/nativeclient-group/EncodingEncapsulation/releases](https://baltig.viaccess-orca.com:8443/VRT/nativeclient-group/EncodingEncapsulation/releases)
-   **Most important**: Add all of them in your machine’s path.

Other option is follow the instructions of each GitLab repo from cwipc_x, sub and bin2dash.

## Build

having the previous already done, is create a Native Unity Player binary to be executed by the end user. To do that it could be done by 2 different ways:

### Using Editor's UI

Opening the project inside the editor and making the build through the UI.
- Open TestBed using Unity 2019.3.9f1.
- Press File.
- Select Build Settings.
- Target the desired Platform. (Windows)
- Select the desired Architecture. (x86_64)
- Press Build Button.

![How to Build](images/Build.png)

### Using Command Line

Running the following command.
    
`> Unity.exe -batchmode -projectPath "path_to_project" -buildWindows64Player "path_to_place_build"`

You can also change the  -buildWindows64Player command by the following to target other OS:

-   Windows x86: `-buildWindowsPlayer`
    
-   Windows x64: `-buildWindows64Player`
    
-   Linux x64: `-buildLinux64Player`
    
-   OSX: `-buildOSXUniversalPlayer`

## Scenes

### DevelopmentTests/PointClouds

Scene to test the Point Clouds compoments developed by CWI.

- How to run this test:
	```
	Open scene.
	Press play.
	```

- Result:
	```
	A point cloud from "StreamingAssets/pcl_frame1.ply" file.
	```

### MS/Signals/Scenes/Signals

Scene to test the signals unity bridge compoments developed by Motion Spell.

- How to run this test:
	```
	Open scene.
	Press play.
	Inspecting Signals object in the hierarchy you can change the stream name and the URL.
	```

- Result:
	```
	Console log showing several values: Streamer pointer value, if it can play the URL and stream count.
	In case of error, console shows it;
	```

