# VR2Gather - Unity application framework for immersive social VR

VR2Gather is an application framework to allow creating immersive social VR applications.

It is a descendent from the `VRTApplication` created in the [VRTogether](https://vrtogether.eu) project.

## Quick build instructions

- check out the repository.
- install Unity, preferably Unity Hub.
- Install prerequisites:
	- cwipc from <https://github.com/cwi-dis/cwipc>
	- (optionally) bin2dash and sub (to be provided)
	- probably more that I forgot
- open the toplevel directory in Unity.
- select `LoginManager` scene
- play

## Re-installing nuget packages

Mainly ffmpeg. Check whether newer versions are available on `nuget.org`. Do the following steps:

- Update toplevel `packages.config`
- Run `nuget restore`
- `git rm` and `git add` of the subdirectories in `Assets/packages`.

