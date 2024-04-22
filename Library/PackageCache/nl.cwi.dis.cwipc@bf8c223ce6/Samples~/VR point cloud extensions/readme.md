## CWIPC VR Samples

These samples are part of the `cwipc_unity` package, <https://github.com/cwi-dis/cwipc_unity>. You are free to use, modify, etc.

These samples show how to use live point cloud streams with OpenXR VR headsets and controllers. **Note:** to use these samples you should also install the `Simple` samples because some components such as orchestrator and character controllers are used from those samples.

When you use a VR HMD and controllers (Oculus Quest and Vive have been tested, others may work) you can teleport around the play area and do discrete turns (which is useful if you have a single RGBD camera: you can turn your virtual point cloud representation to look at the other participant without turning around in the real world, which would cause only your side or back to be captured).

> Turn with an Oculus controller using the joystick on either controller. Turn with a Vive controller by using trackpad left/right and trackpad press at the same time, again on either controller.
> Teleport by using the `grip` on both Oculus and Vive controllers.

You can re-center your self-representation to make your virtual point cloud body correctly overlap your real body.

> With Oculus controllers, hold `trigger` and use joystick up/down to adjust the height of your eyes. Hold trigger and press down the joystick to temporarily decouple your virtual point cloud body from your HMD tracking, so you can move your physical body "to the right place" respective to your virtual point cloud body.

**Note:** You can also use these scenes without a VR HMD, then you use keyboard and mouse to move and position your viewpoint. From the keyboard `WASD` moves, arrow keys turn and adjust view point height. With the mouse left button moves and right button allows turning and adjusting height.

## Scenes

- `VRPrerecordedPointCloudViewer` is like `PrerecordedPointCloudViewer` but allows you to adjust your position and view angle.
- `VRTwoUsers` is like `TwoUsersTiled` but allows you to move your character andre-center your self-representation as explained above. 

## Prefabs

- `cwipc_view_interaction` contains the XR rig, movement controller and all the other objects needed to allow using XR and XR Input. All input actions are defined in `VRPointCloudSelf.inputactions`.
- `cwipc_avatar_self_xr` is like `cwipc_avatar_self_tiled` but it uses `cwipc_view_interaction` to implement XR viewing, interaction and recentering.

## Scripts

Recentering you self-view is implemented using the `ViewAdjust` script.
