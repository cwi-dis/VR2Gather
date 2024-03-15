# VR2Gather - Unity application framework for immersive social VR

VR2Gather is an application framework to allow creating immersive social VR applications in Unity. Participants in a VR2Gather-based experience can be represented as live volumetric video, and see themselves as they are captured live, thereby allowing more realistic social interaction than in avatar-only based systems.

VR2Gather experiences do not rely on a central cloud-based game engine. Each participant runs a local copy of the application, and communication and synchronization is handled through a central experience-agnostic  _Orchestrator_ that handles forwarding of control messages, point cloud streams and conversational audio between the participants.

VR2Gather is a descendent from `VRTApplication` created in the [VRTogether](https://vrtogether.eu) project, and further developed in the [Mediascape XR](https://www.dis.cwi.nl/funding/mediascape/) and [Transmixr](https://transmixr.eu) projects. Current development is primarily done by the [CWI DIS group](https://www.dis.cwi.nl).

> This work was supported through "PPS programmatoeslag TKI" Fund of the Dutch Ministry of Economic Affairs and Climate Policy and CLICKNL, the European Commission H2020 program, under the grant agreement 762111, VRTogether, http://vrtogether.eu/, and the European Commission Horizon Europe program, under the grant agreement 101070109, TRANSMIXR, https://transmixr.eu/. Funded by the European Union.

If you use this code or parts thereof, we kindly ask you to cite the relevant publication:

>I. Viola, J. Jansen, S. Subramanyam, I. Reimat and P. Cesar, "VR2Gather: A Collaborative, Social Virtual Reality System for Adaptive, Multiparty Real-Time Communication," in IEEE MultiMedia, vol. 30, no. 2, pp. 48-59, April-June 2023, doi: 10.1109/MMUL.2023.3263943.

Except where otherwise noted VR2Gather is copyright Centrum Wiskunde & Informatica, and distributed under the MIT license.

## Dependencies

VR2Gather requires Unity 2022.3.

VR2Gather requires a fair number of packages, but all of these are either freely available through the Unity Package Manager, or available as open source (and the Unity project should download all of these automatically).

But to use the current version of VR2Gather (as of February 2024) you need two prerequisites that may need a bit of work:

- A VR2Gather orchestrator, running somewhere on a public IP address. For light use and development, you can use our CWI-DIS orchestrator (the address of which is pre-configured in the code). But at some point you will have to run your own copy. The new version 2 orchestrator is open source, and can be built from the sources at <https://github.com/cwi-dis/vr2gather-orchestrator-v2>.
- You need a copy of the **Best Socket.IO Bundle** (the successor to BestHTTP) by Tivadar György Nagy. There are a number of ways to get this:
	- We have a license for a limited number of seats. If you are a member of the CWI DIS group you will have access to our copy. It will be automatically pulled in as a `git submodule` into `Packages/External/VR2G-besthttp`.
	- If you are a close collaborator with the CWI DIS group: we may have a seat license to share. Contact us, and we will give you access to the private git submodule.
	- Otherwise you will have to buy your own seat license and install it through the Unity Package Manager.

A potential issue with the current state of the code is that VR2Gather is currently structured as a framework, not as a set of packages: the VR2Gather repository should be the top-level repository, with your application-specific code as a git submodule. We are working on restructuring this, but at the moment it is probably easiest to fork the VR2Gather repository and then create a branch and a submodule for your work.

## Developer documentation

There is some preliminary documentation in [Documentation/01-overview.md](Documentation/01-overview.md)
