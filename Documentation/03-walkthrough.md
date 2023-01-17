# VR2Gather - Walktrough of application structure

This section  gives a very high level overview of how a VR2Gather experience works at runtime, how the project is organized and how the various scenes, prefabs, assemblies, packages and scripts fit together.

You will see the term _Pilot_ used a lot in this document and in the project. For historical reasons this is the term we use for an experience: a Pilot is one or more participants running an experience concurrently and interacting with that experience and with each other.

The other term you will often see is _Player_. This is one of the participants that is currently immersed in an experience.

## Runtime structure

Each participant runs a local copy of the application (or, for development, runs the experience from within the Unity Editor). These copies all implement the full "business logic" of the experience. 

The applications communicate through a central cloud-based `Orchestrator` that is only responsible for forwarding messages between the application instances, and for forwarding live media streams (conversational audio, RGBD streams) between participants. There is no business logic in the Orchestrator.

The orchestrator also helps with synchronizing the experience between participants, by allowing to find the offset between local system time (NTP-based) and orchestrator system time.

> As of this writing, early 2023, the orchestrator is also responsible for session management and for some of the participant settings, such as which avatar representation to use. This orchestrator is expected to be replaced in the near future.

For some actions in the experience (think: creating a new virtual object) it is important that the action is coordinated. This is implemented by having one application instance designated the _master instance_, and have such actions always done first on the master.

## Github Repository Structure

The core of VR2Gather is intended to become open source soon, but obviously this is not the case for the experiences. Moreover, there are currently still some modules that cannot be open sourced.

For this reason VR2Gather uses [Git Submodules](https://git-scm.com/book/en/v2/Git-Tools-Submodules) to structure the project into manageable parts.

The `VR2Gather` repository is the main repository, and should not contain anything that can not be open sourced.

There is a submodule `Assets/ExternalAssets/VR2G-basthttp` that contains a package that is needed to communicate with the orchestrator.

Each new experience will get its own branch in the main repository and its own git submodule. So, if you are developing an experience `MyGreatExperience` you will have a branch `develop/MyGreatExperience`, and later a branch `deployment/MyGreatExperience` and on that branch (_only on that branch!_) there will be a submodule `Assets/ExternalPilots/VR2G-MyGreatExperience`.

### Branch names

A quick note on branch names, to keep things manageable:

- Experiences under development have a branch name starting with `development/`.
- The `master` branch should almost always be in a runnable state, and should almost never get individual commits. 
- Most other branches should have a corresponding issue in Github, and the branch name should be called after the issue number. So, I am typing this on branch `issue54-documentation`, which will be merged into `master` when complete, and then deleted.
- Temporary branches should start with `exp-` and have an indication about the owner. So I would create a branch `exp-jack-wildidea` for something I would work on without an issue, and eventually either throw that branch away (if it was a bad idea) or merge it into an issue branch (if it was a good idea).

> There is a potential problem with issue numbers because we have multiple repositories, and multiple forks of VR2Gather as well. Let's hope that doesn't turn out to be too big a problem.

## Unity Project Structure

The Unity project is structured into a number of _assemblies_, each with a toplevel folder (or near-toplevel folder), and each with its own substructure of `Scripts`, `Prefabs`, etc. There is also one external package that is indispensible.

> The plan is to turn this assembly-based structure into a package-based structure at some point in the future.

In a roughly bottom-to-top order of dependencies these are:

- `cwipc_unity` is a package that contains the point cloud capturers, renderers and compressors. But: in addition it contains a few classes for memory management, shared thread-safe queues and other infrastructure that is used throughout VR2Gather.
- `XR` and `XRI` are part of the Unity XR and XR Interaction toolkits.
- `VRTCore` has base classes and interfaces for general infrastructure, configuration storage and some things that didn't have anywhere else to go.
- `VRTInitializer` has a few more such classes, but is separate because of assembly dependency issues in Unity.
- `VRTMedia`, `VRTVideo` and `VRTProfiler` are probably hisotirc artefacts.
- `UserRepresentation/PointClouds` has everything that has to do with using point clouds as your user representation (think: avatar).
- `UserRepresentation/WebCam` is the implementation of an avatar that has a "screen" that shows the image from your webcam.
- `UserRepresentation/Voice` has the code that allows participants to talk to each other in an experience.
- `Transport/SocketIO` and `Transport/TCP` handle streaming of the user representation streams (from the previous set of assemblies) over the net, so that participants can see and hear each other.
- `Orchestrator` has the communication code and the stubs to allow VR2Gather application instances to communicate via the orchestrator.
- `DevelopmentTests` has various scenes and sripts that were used during development. Some may still work.
- `Pilots/Common` has most of the implementation of the VR2Gather framework.
- The other assemblies under `Pilots` have things that are specific to a certain experience (for an experience that is included in the base repository)
- `PilotsExternal` is where the git repository for the experience under development should live.

