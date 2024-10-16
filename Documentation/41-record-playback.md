# VR2Gather - Record and playback

Sometimes it may be useful to record a user, both their point cloud representation and their position, orientation and gaze direction, during a session.

For example, if you want to do things like performance measurements, it is nice to have all data and behaviour recorded so it can later play it back indentically with different settings, protocols, etc.

At the moment this only works for experiences that consist of a single scene (as recording will be restarted and overwritten after a scene change).

## Recording

- To capture the RGBD camera streams during the session: edit `cameraconfig.json` and add a toplevel entry `system.record_to_directory=rgbddirectory` where `rgbddirectory` is an absolute or relative path to a directory where the RGBD streams will be recorded (one file per camera). Create that directory. Ensure `sync_master_serial` is set.
- To capture user position and orientation during the session: edit `config.json` and add a field `LocalUser.PositionTracker.outputFile=positionfilename`, where `positionfilename` is the relative or absolute pathname where the timestamped JSON position data will be saved.
- Run the session.
- Save the `rgbddirectory` and `positionfilename` files.
- Undo the changes to `cameraconfig.json` and `config.json` so you don't accidentally overwrite the data.

## Playback

- Create a new `cameraconfig.json` that is based on the existing one, but uses the recorded files (so using type `kinect_offline` in stead of `kinect`, or similar for realsense).
  - Ensure you reference the correct input file for each camera.
  - Ensure you have removed the `record_to_directory` setting.
  - You can test this by running `cwipc_view` in the directory where the new cameraconfig is. It should playback the captured content.
- Edit `config.json` and
  - change `LocalUser.PCSelfConfig.CameraReaderConfig.configFilename` to refer to the new cameraconfig file created in the previous step
  - add a field `LocalUser.PositionTracker.inputFile=positionfilename`, where `positionfilename` is the relative or absolute pathname where the timestamped JSON position data will be played back from,
- Run the session. Now, for this user the point clouds should be taken from the recording of the previous session, and the users position and orientation should also be played back from the previous session.
