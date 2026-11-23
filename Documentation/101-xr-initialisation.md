# XR and controller initialisation

Where the pieces are that make VR2Gather start in VR (HMD, controllers, hands) or without it (desktop: keyboard and mouse), and what to check when that breaks after a Unity, package or HMD update. The interaction itself (buttons, grabbing, teleporting) is in [Interaction](100-interaction.md). Written for issue #246; state of VR2Gather 1.4.6.

## Project settings

- **XR Plug-in Management** (Project Settings → XR Plug-in Management), per platform:
  - Standalone (Windows, macOS, Linux) and Android (Quest): the **OpenXR** loader only, with **Initialize XR on Startup** on.
  - VRTApp-Develop also has an Oculus Loader asset in `Assets/XR/Loaders/`, and VR2Gather's `package.json` depends on `com.unity.xr.oculus`, but no platform uses that loader. Whether the dependency can go hasn't been checked.
- **OpenXR** (Project Settings → XR Plug-in Management → OpenXR), per platform:
  - Interaction profiles for the controllers in use, such as Oculus Touch (and on Android the Meta Quest Touch Pro profile, plus the Meta Quest feature).
  - XR Hands' features **Hand Tracking Subsystem** and **Meta Hand Tracking Aim**, for hand tracking. The Porting Check's "Hand Tracking" check tests these two for Standalone and Android.
- **Linux** has no OpenXR runtime, and with OpenXR active its build validation refuses a Linux build. Linux players are built without XR through `VRT.Tools.CommandLineBuild.BuildWithoutXR` (`Editor/Tools/CommandLineBuild.cs`, issue #366).

## At startup

- **`config.json`**: `disableVR` (in `VRTConfig`, `Runtime/VRTCore/VRTConfig.cs`) turns VR off even when an HMD is there.
- **`VRTInitializer.Start()`** (`Runtime/VRTInitializer/VRTInitializer.cs`, on `Tool_VRTInitializer`, which is part of `Tool_scenesetup` and of the login scene): with `disableVR`, it stops and unloads XR. On macOS it also stops and unloads XR if a loader is active, as a guard against a 2023 problem where mouse input in the UI stopped working with XR active (#109). On today's Macs no loader is active at that point, and the log says "VRTInitializer: XR was not enabled".
- **`VRTConfig.ISXRActive()`** says whether an XR loader is active. Code that behaves differently with and without XR asks this, not `XRSettings` directly (with one exception, below).

## In the player (`P_Self_Player`)

The root of `P_Self_Player` is XRI's XR Origin (a nested prefab from the XRI Starter Assets), with VR2Gather's own components added.

- **`FixInteractorGameObjects`** (`Runtime/VRTCommon/Interaction/FixInteractorGameObjects.cs`), in `Start()`:
  - activates the objects in `interactionObjectsXR` (Locomotion XR) with XR, and those in `interactionObjectsNonXR` (Locomotion Non-XR, the keyboard and mouse controls) without XR;
  - without XR, disables the components in `componentsXR`. That is XRI's `XRInputModalityManager`, which switches between controllers and tracked hands: without a hand tracking subsystem it would warn "Hand Tracking Subsystem not found or not running" in its first `Update()` (#246).
- **`PlayerControllerSelf.LoadCameraTransform()` / `SaveCameraTransform()`**: with XR, the camera offset (set by "reset view") is kept in `PlayerPrefs` (`cam_pos_*`, `cam_rot_*`); without XR, the camera is put at a fixed eye height of 1.8 m and nothing is saved.
- **`VRTHeadsUpDisplay`**: the HUD's distance depends on `XRSettings.isDeviceActive` (the exception mentioned above).
- **The HUD camera** (`HUDOverlayCamera`) is switched on and, in URP, put into Main Camera's camera stack by `CameraStackSetup` (#384, #433); that's the same with and without XR.

## When it breaks

1. Run the Porting Check (Tools → VR2Gather Porting Check): it covers the OpenXR hand tracking features, the Teleport interaction layer, the input actions and the UI layers.
2. Look in the log for `VRTInitializer:` lines (XR on or off at startup) and `FixInteractors:` lines (with `debug` on in `FixInteractorGameObjects`).
3. Check the XR Plug-in Management and OpenXR settings above for the platform you're building for, especially after a Unity or package update: interaction profiles and features are sometimes reset.
