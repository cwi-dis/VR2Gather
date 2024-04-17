# VR2Gather - Prerequisites for developers

This section explains what you need to install to get start with developing a VR2Gather experience.

## Hardware and operating system

Currently the best platform for development is Windows 10/11 64 bit, with a decent GPU. An OpenXR-compatible HMD (Oculus Quest, Vive) is good to have but not a strict requirement, a lot of interaction can be done (or at least tested) with keyboard and mouse.

Linux worked at some time in the past but is untested at the moment.

Mac works, with two caveats:

- There is no support for OpenXR (and hence no support for an HMD) in Unity at the moment.
- The situation with RGBD cameras on MacOS is a bit of a mess. No problem for development, but deploying to Mac is not optimal.

## Software

You need `git`, with LFS support. On Windows install the official Git distribution, and install the `bash` terminal window too. Ensure you check in files with Unix line feeds and check out with Windows line feeds.
It is best to use git over SSH: create an ssh key and add it to your github profile. Test that it works by typing `ssh git@github.com` in a `bash` window.

You probably want to install [Sourcetree](https://www.sourcetreeapp.com) unless you are a git command line wizard. Ensure it uses OpenSSH as its ssh client.

You need Unity. Install the Unity Hub. Don't manually install Unity Editor versions: this will lead to no end of pain in the future.

You need Visual Studio. 2022 is current but 2019 should work. Install the Unity development additions. When you install a Unity Editor from the Unity Hub it can install VS for you, that is fine. You want the Unity additions because it makes debugging a _lot_ easier.

You need the software to control your HMD. Probably the Oculus desktop software or SteamVR. 

You need a working NTP (or other time synchronization implementation) on your system, if your system clock is more than about 100 milliseconds off from "real time" you will suffer problems.

### cwipc

You probably need the [CWI Point Cloud](https://github.com/cwi-dis/cwipc) package. The github page, <https://github.com/cwi-dis/cwipc>, has installation instructions. 

If you don't have an RGBD camera you need not bother with Intel Realsense or Microsoft Kinect support, but you still want `cwipc` to be able to see participants that do have a camera.

The VR2Gather Unity project will automatically obtain the `cwipc_unity` package, but this package still requires the `cwipc` package to be installed on your machine.

## Next steps

You should now be able to open the VR2Gather project in the Unity Editor (through the Unity Hub).

The [Walkthrough](03-walkthrough.md) is a good continuation point, or go back to the [Developer Overview](01-overview.md)
