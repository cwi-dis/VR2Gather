# Installing VR2Gather for developers

This section explains what you need to install to get started with developing a VR2Gather experience.

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

- Optional: the software to control your HMD. Probably the Oculus desktop software or SteamVR. 

- You need a working NTP (or other time synchronization implementation) on your system, if your system clock is more than about 100 milliseconds off from "real time" you will suffer problems.


- A VR2Gather orchestrator, running somewhere on a public IP address (or at least an IP address reachable by all computers you are going to use for VR2Gather). For light use and development, you can use our CWI-DIS orchestrator (the address of which is pre-configured in the code). But at some point you will have to run your own copy. The new version 2 orchestrator is open source, and can be built from the sources at <https://github.com/cwi-dis/vr2gather-orchestrator-v2>.

### cwipc

You probably need the [CWI Point Cloud](https://github.com/cwi-dis/cwipc) package. The github page, <https://github.com/cwi-dis/cwipc>, has installation instructions. 

If you don't have an RGBD camera you need not bother with Intel Realsense or Microsoft Kinect support, but you still want `cwipc` to be able to see participants that do have a camera.

> You will later add the `cwipc_unity` package to your Unity project, but this package still requires the `cwipc` native package to be installed on your machine.

### BestHTTP

You need a copy of the **Best Socket.IO Bundle** (the successor to BestHTTP) by Tivadar György Nagy. There are a number of ways to get this:

- We (the CWI DIS group) have a license for a limited number of seats. If you are a member of the CWI DIS group you will have access to our copy. It will be automatically pulled in as a Unity git package from a repository you should have access to.
- If you are a close collaborator with the CWI DIS group: we may have a seat license to share. Contact us, and we will give you access to the private git submodule.
- Otherwise you will have to buy your own seat license and install it through the Unity Package Manager.

## Creating a Unity project with VR2Gather

### Porting an "old" VR2Gather project

Instructions are in the [Port a VRTApp to the package](31-port-to-package.md) document.

### Starting from scratch

If you are starting from scratch it is easiest to make a copy of our empty `VRTApp-Sample` project:

- You can find it at the top level of the <https://github.com/cwi-dis/VR2Gather> repository.
- You may be able to download it as a ZIP file, instructions to be provided later.

This project is pre-configured so it already includes the VR2Gather package and all if its dependencies. **But note:** it has references to our private (CWI DIS) BestHTTP packages, see the note above.

### Adding VR2Gather to an existing Unity project

If you already have a Unity project and want to add VR2Gather to it:

- Ensure you use the new Input System (not the old Input Manager)
- Add the 4 **Best Socket.IO Bundle** packages mentioned above to your project.
- Add the `nl.cwi.dis.cwipc` package, by github url:

  ```
  git+https://github.com/cwi-dis/cwipc_unity?path=/nl.cwi.dis.cwipc
  ```

- Add the `nl.cwi.dis.vr2gather.nativelibraries` package, by github url:
 
  ```
  git+https://github.com/cwi-dis/VR2G-nativeLibraries?path=/nl.cwi.dis.vr2gather.nativelibraries
  ```
  
  > This step needs to be done before the next step, it seems. We are not quite sure why...
- Add the `nl.cwi.dis.vr2gather` package, by github url:

  ```
  git+https://github.com/cwi-dis/VR2Gather?path=/nl.cwi.dis.vr2gather
  ```
- FAdd the _VR2Gather Essential Assets_ from Package Manager->VR2Gather->Samples tab. These are some essential assets, the most important one being the `LoginManager` scene that must be used as the first scene in your application (because it creates the shared session).

- Next, in _Project Settings_, you probably want to add an XR plugin (for example _OpenXR_) and you probably want to add at least one Interaction Profile (for example Oculus Touch Controller).

- You may also need to add all the scenes you need to the _Build Settings_.

You should now be able to run the two "default experiences": _Pilot 0_ and _Technical Playground_.

You will not yet be able to use your own scenes with VR2Gather support: see [Creating an experience](10-createnew.md) for instructions on how to modify your scene and how to include it as a scenario for VR2Gather.


## Next steps

The [Creating an experience](10-createnew.md) or [Walkthrough](03-walkthrough.md) sections, or go back to the [Developer Overview](01-overview.md)
