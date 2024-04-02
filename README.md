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
	- We have a license for a limited number of seats. If you are a member of the CWI DIS group you will have access to our copy. It will be automatically pulled in as a Unity git package from a repository you should have access to.
	- If you are a close collaborator with the CWI DIS group: we may have a seat license to share. Contact us, and we will give you access to the private git submodule.
	- Otherwise you will have to buy your own seat license and install it through the Unity Package Manager.

## Installing the VR2Gather package

It is assumed you already have a Unity 3D project, otherwise create one.

In the Unity Package Manager do the following:

- Add the 4 **Best Socket.IO Bundle** mentioned above to your project.
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
  git+https://github.com/cwi-dis/VR2Gather?path=/nl.cwi.dis.vr2gather#142-restructure
  ```
- For the VR2Gather project, Add the _VR2Gather Essential Assets_ from Package Manager->VR2Gather->Samples tab. These are some essential assets, the most important one being the `LoginManager` scene that must be used as the first scene in your application (because it creates the shared session).

Next, in _Project Settings_, you probably want to add an XR plugin (for example _OpenXR_) and you probably want to add at least one Interaction Profile (for example Oculus Touch Controller).

You may also need to add all the scenes you need to the _Build Settings_.

You should now be able to run the two "default experiences": _Pilot 0_ and _Technical Playground_. To add new experiences, or adapt your existing scenes for VR2Gather, see the documentation below.

## Developer documentation

There is some preliminary documentation in [Documentation/01-overview.md](Documentation/01-overview.md) explaining how to develop your own VR experiences using VR2Gather.

### Developing on VR2Gather itself

If you want to make changes to VR2Gather you should check out this repository, <https://github.com/cwi-dis/VR2Gather>. But really only then: otherwise just install the Unity package through the package manager.

At the toplevel folder you will find the source of the Unity package, `nl.cwi.dis.vr2gather`.

You will also find 3 Unity projects:

- `VRTApp-Develop` is a pretty empty project that imports `nl.cwi.dis.vr2gather` by relative pathname. This has the advantage that as you make changes to any of the files from the package these changes will be made in-place, so you can then commit and push them later. There is also a trick with a symlink used to include the Samples into this project.
- `VRTApp-TestGitPackage` imports the package normally, i.e. using the github URL. So after you have made changes using VRTApp-Develop and pushed those changes you can open VRTApp-TestGitPackage, update the package, re-install the samples, and check that your changes actually work and have been pushed.

After making changes, and before pushing or testing with VRTApp-TestGitPackage you should _always_ change the package version number in `package.json`. Otherwise the Unity package manager will think that package has not changed and it will not re-import it.