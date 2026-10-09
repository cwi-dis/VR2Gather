# VR2Gather - Building the application

## Building from the command line

`VRTApp-Develop/scripts/build.sh` builds a player and zips it:

```
VRTApp-Develop/scripts/build.sh            # for the current platform
VRTApp-Develop/scripts/build.sh windows    # or mac, linux
```

- It uses the Unity CLI (`unity build`), which reads the Unity version from `ProjectSettings/ProjectVersion.txt` and finds the matching Editor. Install the CLI first, with `curl -fsSL https://unity.com/install.sh | bash` (macOS, Linux) or `irm https://unity.com/install.ps1 | iex` (Windows PowerShell). On Windows, run the script from Git Bash.
- The project must not be open in a Unity Editor while building.
- The player goes to `VRTApp-Develop/Builds/<platform>/`, the zip to `Builds/<productName>-<platform>.zip`, and the build log to `Builds/buildlog-<platform>.txt`. Unity's debug-symbol folders (`*_DoNotShip`, `*_DontShip…`) are left out of the zip.
- Building for another platform than the one you're on needs that platform's build support module in the Editor.
- cwipc must be installed on the build machine, otherwise its native libraries aren't found.

The script is the same in every VR2Gather project: it finds the project from its own location and the app name from `productName` in the Player Settings. To use it in your own project, copy it to `scripts/build.sh` in your project folder.

There is currently no CI build; see issue #355.

## Older notes (may be outdated)

Here is old stuff from the toplevel readme:


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

## Github Actions

- Needs a license. See <https://game.ci/docs/github/activation> for how to get one and install it.
