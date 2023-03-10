# VR2Gather - Unity application framework for immersive social VR

VR2Gather is an application framework to allow creating immersive social VR applications in Unity. Participants in a VR2Gather-based experience can be represented as live volumetric video, and see themselves as they are captured live, thereby allowing more realistic social interaction than in avatar-only based systems.

VR2Gather experiences do not rely on a central cloud-based game engine. Each participant runs a local copy of the application, and communication and synchronization is handled through a central experience-agnostic  _Orchestrator_ that handles forwarding of control messages, point cloud sstreams and conversational audio between the participants.

VR2Gather is a descendent from `VRTApplication` created in the [VRTogether](https://vrtogether.eu) project.

VR2Gather is copyright Centrum Wiskunde & Informatica, and distributed under the MIT license.

## Warning

As of this writing (March 2022) it is not yet possible to use VR2Gather as distributed here on github without some modifications, for two main reasons.

The main problem is that the central _Orchestrator_ cannot be made available as open source.

Another problem is a dependency on (a slightly modified version of) the wonderful BestHTTP/2 package from the Unity Asset Store (in the `VR2G-BestHTTP` submodule).

We are working on various approaches to alleviate this:

- Allow easier integration of your own copy of BestHTTP/2,
- Allow for using an open alternative to BestHTTP/2,
- Allow for using an alternative orchestrator that does not require SocketIO (and therefore BestHTTP/2).

Another potential problem is that VR2Gather is currently structured as a framework, not as a set of packages: the VR2Gather repository should be the top-level repository, with your application-specific code as a git submodule. We are working on restructuring this, but at the moment it is probably easiest to fork the VR2Gather repository and then create a branch and a submodule for your work.

## Developer documentation

There is some preliminary documentation in [Documentation/01-overview.md](Documentation/01-overview.md)