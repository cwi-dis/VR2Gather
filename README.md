# Test Bed Unity Proyect.

This repository will keep a bunch of unit test to check all the components developed within the VRTogether consortium.

It currently uses Unity 2018.2.21f1 and runs on Windows 10.

## Scenes

### CWI/Scenes/PointClouds

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

