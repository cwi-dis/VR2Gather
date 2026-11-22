# VR2Gather - Unity package for immersive social VR

VR2Gather is an Unity package to allow creating immersive social VR applications in Unity. Participants in a VR2Gather-based experience can be represented as live volumetric video, and see themselves as they are captured live, thereby allowing more realistic social interaction than in avatar-only based systems.

VR2Gather experiences do not rely on a central cloud-based game engine. Each participant runs a local copy of the application, and communication and synchronization is handled through a central experience-agnostic  _Orchestrator_ that handles forwarding of control messages, point cloud streams and conversational audio between the participants.

VR2Gather is a descendent from `VRTApplication` created in the [VRTogether](https://vrtogether.eu) project, and further developed in the [Mediascape XR](https://www.dis.cwi.nl/funding/mediascape/) and [Transmixr](https://transmixr.eu) projects. Current development is primarily done by the [CWI DIS group](https://www.dis.cwi.nl).

> This work was supported through "PPS programmatoeslag TKI" Fund of the Dutch Ministry of Economic Affairs and Climate Policy and CLICKNL, the European Commission H2020 program, under the grant agreement 762111, VRTogether, http://vrtogether.eu/, and the European Commission Horizon Europe program, under the grant agreement 101070109, TRANSMIXR, https://transmixr.eu/. Funded by the European Union.

If you use this code or parts thereof, we kindly ask you to cite the relevant publication:

>I. Viola, J. Jansen, S. Subramanyam, I. Reimat and P. Cesar, "VR2Gather: A Collaborative, Social Virtual Reality System for Adaptive, Multiparty Real-Time Communication," in IEEE MultiMedia, vol. 30, no. 2, pp. 48-59, April-June 2023, doi: 10.1109/MMUL.2023.3263943.

Except where otherwise noted VR2Gather is copyright Centrum Wiskunde & Informatica, and distributed under the MIT license.

## Installing the VR2Gather package

VR2Gather requires Unity 2022.3.

VR2Gather requires a fair number of Unity packages and native packages. All of these are either freely available through the Unity Package Manager, or available as open source.

Please read the [Installation guide](Documentation/02-installation.md).

We mean it: please read the [Installation guide](Documentation/02-installation.md). Also if you have used VR2Gather before. A lot of things have changed, and ensuring you have all the right bits and pieces installed, and you have done so in the right order, will save you a lot of headaches.

There is more documentation in [Documentation/01-overview.md](Documentation/01-overview.md) explaining how to develop your own VR experiences using VR2Gather.

## Developing on VR2Gather itself

If you want to make changes to VR2Gather you should check out this repository, <https://github.com/cwi-dis/VR2Gather>. But really only then: otherwise just install the Unity package through the package manager (following the instructions above). The sample project that used to be available in this repository is no longer there, you really want to use <https://github.com/cwi-dis/VR2Gather_sample> as is explained in the installation guide.

At the toplevel folder you will find the source of the Unity package, `nl.cwi.dis.vr2gather`.

You will also find a Unity project `VRTApp-Develop` which is a pretty empty project that imports `nl.cwi.dis.vr2gather` by relative pathname. This has the advantage that as you make changes to any of the files from the package these changes will be made in-place, so you can then commit and push them later. There is also a trick with a symlink used to include the Samples into this project.

`VRTApp-Develop-URP` is the same project for the Universal Render Pipeline (URP); `VRTApp-Develop` itself uses the Built-in Render Pipeline (BIRP), without the URP package installed. Both import the same package and the same Samples, so a change made in one shows up in the other. Check changes to materials and shaders in both.

  **Note:** to use `VRTApp-Develop` on Windows you **must** first enable symlinks with the following steps. You may have to do a fresh clone of the repository (after following these steps):

  - First enable them for Windows itself, in _Settings, Privacy & Security_, _Developer Mode_
    - This setting seems to have moved for some version of Windows 11. Search for _Developer_ in the System Settings.
  - Then run the following two commands (the second only if you have checked out already):
    
    ```
    git config --global core.symlinks true
    git config --local core.symlinks true

    ```
After making changes, and before pushing or testing with `VR2Gather_sample` you should _always_ change the package version number in `nl.cwi.dis.vr2gather/package.json` and commit and push. Otherwise the Unity package manager will think that package has not changed and it will not re-import it.

## Render pipeline (BIRP/URP) plan

Unity is deprecating the Built-in Render Pipeline (BIRP) in 6000.5, so VR2Gather has to work in Universal Render Pipeline (URP) apps, while still working in BIRP apps for as long as those exist:

- There is one `master`. The package and the samples are made *dual*: every shader and material works in both pipelines, without per-pipeline copies (Shader Graph shaders with both targets, or hand-written shaders with a URP SubShader guarded by `PackageRequirements`).
- `VRTApp-Develop` (BIRP, without the URP package) and `VRTApp-Develop-URP` are the development projects, and CI builds both.
- Not everything is dual yet: many materials still use the BIRP `Standard` shader, and render pink in URP.

See [issue #256](https://github.com/cwi-dis/VR2Gather/issues/256) for the full plan and rationale.
