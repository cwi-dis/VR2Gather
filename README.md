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

If you want to make changes to VR2Gather you should check out this repository, <https://github.com/cwi-dis/VR2Gather>. But really only then: otherwise just install the Unity package through the package manager (following the instructions above).

At the toplevel folder you will find the source of the Unity package, `nl.cwi.dis.vr2gather`.

You will also find 2 Unity projects:

- `VRTApp-Develop` is a pretty empty project that imports `nl.cwi.dis.vr2gather` by relative pathname. This has the advantage that as you make changes to any of the files from the package these changes will be made in-place, so you can then commit and push them later. There is also a trick with a symlink used to include the Samples into this project.
- `VRTApp-Sample` imports the package normally, i.e. using the github URL. So after you have made changes using VRTApp-Develop and pushed those changes you can open VRTApp-Sample, update the package, re-install the samples, and check that your changes actually work and have been pushed.

After making changes, and before pushing or testing with VRTApp-Sample you should _always_ change the package version number in `nl.cwi.dis.vr2gather/package.json`. Otherwise the Unity package manager will think that package has not changed and it will not re-import it.
