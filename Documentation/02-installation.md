# Installing VR2Gather for developers

This section explains what you need to install to get started with developing a VR2Gather experience.

The TL;DR is: you do not want to clone this repository, you want to install VR2Gather through the Unity package manager into your project.

There is a sample repository <https://github.com/cwi-dis/VR2Gather_sample> that does just that.

## Hardware and operating system

Currently the best platform for development is Windows 10/11 64 bit, with a decent GPU. An OpenXR-compatible HMD (Oculus Quest, Vive) is good to have but not a strict requirement, a lot of interaction can be done (or at least tested) with keyboard and mouse.

Linux worked at some time in the past but is untested at the moment.

Mac works, with two caveats:

- There is no support for OpenXR (and hence no support for an HMD) in Unity at the moment.
- The situation with RGBD cameras on MacOS is a bit of a mess. No problem for development, but deploying to Mac is not optimal.

## Software

### Prerequisites

- You need Unity. Install the Unity Hub. Don't manually install Unity Editor versions: this will lead to no end of pain in the future.

- You need Visual Studio. 2022 is current but 2019 should work. Install the Unity development additions. When you install a Unity Editor from the Unity Hub it can install VS for you, that is fine. You want the Unity additions because it makes debugging a _lot_ easier.

- You need to have `git` and `git lfs` installed on your development machine. And you need to have git lfs enabled for your user account for all repositories (run the command `git lfs install`). Ensure you do this **before** trying to open the `VR2Gather_sample` project in Unity. Failure to do this will cause the Unity Package Manager to download and cache a broken version of VR2Gather.
	- If you had not read these instructions before trying to install VR2Gather you may be able to fix things by clearing out all Unity's package caches and the `Library` folder inside your project.

- Optional: the software to control your HMD. Probably the Oculus desktop software or SteamVR. 

- You need a working NTP (or other time synchronization implementation) on your system, if your system clock is more than about 100 milliseconds off from "real time" you will suffer problems.


- A VR2Gather orchestrator, running somewhere on a public IP address (or at least an IP address reachable by all computers you are going to use for VR2Gather). For light use and development, you can use our CWI-DIS orchestrator (the address of which is pre-configured in the code). But at some point you will have to run your own copy. The new version 2 orchestrator is open source, and can be built from the sources at <https://github.com/cwi-dis/vr2gather-orchestrator-v2>.

### cwipc

You probably need the [CWI Point Cloud](https://github.com/cwi-dis/cwipc) package. The github page, <https://github.com/cwi-dis/cwipc>, has installation instructions. 

If you don't have an RGBD camera you need not bother with Intel Realsense or Microsoft Kinect support, but you still want `cwipc` to be able to see participants that do have a camera.

> You will later add the `cwipc_unity` package to your Unity project, but this package still requires the `cwipc` native package to be installed on your machine.

## Creating a Unity project with VR2Gather

### Porting an "old" VR2Gather project

Instructions are in the [Port a VRTApp to the package](31-port-to-package.md) document.

### Starting from scratch

If you are starting from scratch it is easiest to make a copy of our <https://github.com/cwi-dis/VR2Gather_sample> repository.

This project is pre-configured so it already includes the VR2Gather package and all if its dependencies.

### Adding VR2Gather to an existing Unity project

If you already have a Unity project and want to add VR2Gather to it:

- Ensure you use the new Input System (not the old Input Manager)
- Ensure you have done `git lfs install` (see comment above).
- Add the `nl.cwi.dis.cwipc` package, by github url:

  ```
  git+https://github.com/cwi-dis/cwipc_unity?path=/nl.cwi.dis.cwipc
  ```
- Add the Unity SocketIO package by itisnajim. By github URL:
  
  ```
  git+https://github.com/troeggla/SocketIOUnity.git#b43e1fa081328eea08f8a7c05c54eba14c97ae22
  ```
  
- Add the `nl.cwi.dis.vr2gather.nativelibraries` package, by github url:
 
  ```
  git+https://github.com/cwi-dis/VR2G-nativeLibraries?path=/nl.cwi.dis.vr2gather.nativelibraries
  ```
- Add the `nl.cwi.dis.vr2gather.nativelibraries.webrtc` package, by github url:
 
  ```
  git+https://github.com/cwi-dis/VR2G-nativeLibraries-webrtc?path=/nl.cwi.dis.vr2gather.nativelibraries.webrtc
  ```
  
> NOTE: Those two step needs to be done before the next step, it seems. We are not quite sure why...
  
- Add the `nl.cwi.dis.vr2gather` package, by github url:

  ```
  git+https://github.com/cwi-dis/VR2Gather?path=/nl.cwi.dis.vr2gather
  ```
- Add the _VR2Gather Essential Assets_ from Package Manager->VR2Gather->Samples tab. These are some essential assets, the most important one being the `LoginManager` scene that must be used as the first scene in your application (because it creates the shared session).

- Next, in _Project Settings_, you probably want to add an XR plugin (for example _OpenXR_) and you probably want to add at least one Interaction Profile (for example Oculus Touch Controller).

- You may also need to add all the scenes you need to the _Build Settings_.

You should now be able to run the two "example experiences": _Pilot 0_ and _Technical Playground_. The first one is a minimal space for at most 4 people to meet. The second one is similar, but it has a few extra objects such as a mirror and some shared objects such as a "photo camera" that you can use to take pictures, and mud balls that you can throw at each other.

You will not yet be able to use your own scenes with VR2Gather support: see [Creating an experience](10-createnew.md) for instructions on how to modify your scene and how to include it as a scenario for VR2Gather.

## Preparing for later updates of VR2Gather

You will probably want to update your version of VR2Gather and its samples at some later point in time, to get access to bug fixes and new features and such.

For this reason it is a **very good idea** (bold used intentionally) not to modify the samples, and not to copy scripts or prefabs from the VR2Gather package.

In stead, create your own prefab variant (or C# subclass) where you implement your changes to the original.

This way, when the underlying prefab or script is modified in a future version of VR2Gather you will have both any new features provided by the VR2Gather base object and your modifications.

## Next steps

The [Creating an experience](10-createnew.md) or [Walkthrough](03-walkthrough.md) sections, or go back to the [Developer Overview](01-overview.md)
