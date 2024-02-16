# VR2Gather - Walktrough of application structure

This section  gives a very high level overview of how a VR2Gather experience works at runtime, how the project is organized and how the various scenes, prefabs, assemblies, packages and scripts fit together.

You will see the term _Pilot_ used a lot in this document and in the project. For historical reasons this is the term we use for an experience: a Pilot is one or more participants running an experience concurrently and interacting with that experience and with each other.

The other term you will often see is _Player_. This is one of the participants that is currently immersed in an experience.

The third term is _Session_, which is a group of _Player_ s that are currently experiencing a _Pilot_ together.

## Runtime structure

Each participant runs a local copy of the application (or, for development, runs the experience from within the Unity Editor). These copies all implement the full "business logic" of the experience. 

The applications communicate through a central cloud-based `Orchestrator` that is only responsible for forwarding messages between the application instances, and for forwarding live media streams (conversational audio, RGBD streams) between participants. There is no business logic in the Orchestrator.

The orchestrator also helps with synchronizing the experience between participants, by allowing to find the offset between local system time (NTP-based) and orchestrator system time.

The orchestrator also handles creation and advertisement of sessions.

For some actions in the experience (think: creating a new virtual object) it is important that the action is coordinated. This is implemented by having one application instance designated the _master instance_, and have such actions always done first on the master. Currently, the master instance is always the instance that created the session.

## Github Repository Structure

The core of VR2Gather is open source, but obviously this is not the case for the experiences: a VR2Gather experience will usually contain content that can not be made available as open source.

For this reason VR2Gather uses [Git Submodules](https://git-scm.com/book/en/v2/Git-Tools-Submodules) to structure the project into manageable parts.

The `VR2Gather` repository is the main repository, and should not contain anything that can not be open sourced.

> Let us repeat this statement, in bold: **The main VR2Gather repository should not contain any material that cannot be distributed as open source**.
> 
> So even if you fork the VR2Gather repository it is probably still a good idea to create a submodule repository for content you know you will probably never want to open source.

There is a submodule `Assets/ExternalAssets/VR2G-nativeLibraries` that contains a couple of native DLLs (and `.so` for Linux, and `.dylib` for Mac). Specifically, these are the DLLs for the Motion Spell low-latency DASH implementation. This code is not open source, but the DLLs may be used and redistributed by VR2Gather.

Each new experience will get its own branch in the main repository and its own git submodule. So, if you are developing an experience `MyGreatExperience` you will have a branch `develop/MyGreatExperience`, and later a branch `deployment/MyGreatExperience` and on that branch (_only on that branch!_) there will be a submodule `Assets/ExternalPilots/VR2G-MyGreatExperience`.

### Branch names

A quick note on branch names, to keep things manageable:

- Experiences under development have a branch name starting with `development/`.
- The `master` branch should almost always be in a runnable state, and should almost never get individual commits. 
- Most other branches should have a corresponding issue in Github, and the branch name should be called after the issue number. So, I was typing this on branch `54-documentation`, which has been merged into `master` when complete, and then deleted.
- Temporary branches should start with `exp-` and have an indication about the owner. So I would create a branch `exp-jack-wildidea` for something I would work on without an issue, and eventually either throw that branch away (if it was a bad idea) or merge it into an issue branch (if it was a good idea).


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
- `Transport/SocketIO`, `Transport/Dash` and `Transport/TCP` handle streaming of the user representation streams (from the previous set of assemblies) over the net, so that participants can see and hear each other.
- `Orchestrator` has the communication code and the stubs to allow VR2Gather application instances to communicate via the orchestrator.
- `Pilots/Common` has most of the implementation of the VR2Gather framework. The functionality and components here are described in the [Prefabs](04-prefabs.md) section.
- The other assemblies under `Pilots` have things that are specific to a certain experience (for an experience that is included in the base repository), they are described in the _Scene Overview_ subsection, below.
- `PilotsExternal` is where the git repository for the experience under development should live.

## Scene Overview

The VR2Gather repository contains 4 main scenes:

- `LoginManager` is the first scene of any VR2Gather experience.
- `Pilot0` is the "Hello World" of VR2Gather experiences: a near-minimal example.
- `TechnicalPlayground` is a copy of Pilot0 where you can easily try out new prefabs, interactions and other functionality.
- `SoloPlayground` is very similar, but it has all orchestrator communication disabled or replaced, so it allows you to do (limited) testing of new functionality on your own, even while offline, without access to the orchestrator.

### Pilot0

Even though LoginManager is the first scene experienced it is better to walk through Pilot0 first, because it the basis of a "normal" VR2Gather scene.

You should open `Assets/Pilots/Pilot0/Scenes/Pilot0` in the hierarchy view while reading this section.

At the top level there is a`Tool_scenesetup` (from a prefab) that contains the per-scene logic:

- The `PilotController` object has three components that together manage the session:

	- `SessionController` manages comunicating with the orchestrator to allow joining the session and leaving it.
	- `PilotController` (or a subclass of it) manages the local copy of the scene, such as fading it in and out at beginnging and end, and it manages any transition to a follow-on scene.
	- `SessionPlayersManager` manages instantiating the prefabs for the participants: a single `SelfPlayerPrefab` for the local participant and a `PlayerPrefab` for each of the other participants in the session.

	> As a result of this paradigm the scene does not contain the usual Unity objects like the main camera and the controller objects, because these are part of the `SelfPlayerPrefab`. This also means that there are a few scripts to fix things up after self-player creation (like telling interactables where the interaction manager is), and some things may be unexpected (if your script tries to use `Camera.Main` in its `Start()` method it will notice there is no main camera).
- `XR Interaction Manager` is farily standard.
- `ErrorManager` handles showing important error messages to the user (even when in VR)
- `TilingConfigDistributor` and `SyncConfigDistributor` are components that ensure temporal and spatial consistency within a session.
- `PlayerInitialLocations` holds the game objects where players will initially be placed. Referenced by the `SessionPlayerManager`, above.


The `Floor` and `Tables` objects contain the actual scene. It is worth noticing that the `Floor` has a `TeleportationArea` component allowing the participants to teleport anywhere.

On each table there is an instance of `PFB_Pilot0ButtonObject`, a gadget that can make a "pling" sound when the button is pressed and that can be picked up and dropped elsewhere (or thrown over the side of the world:-). When one participant interacts with the gadget other participants should see and hear that interaction too.

### LoginManager

This is the starting scene of the VR2Gather application. It connects to the orchestrator, allows the user to log in and either join an existing session, or create a new session for a specific pilot.

It allows the participant to interact with the user interface using HMD controller rays, or using keyboard and mouse when not using an HMD. The participant can select their self-representation (avatar) and some other settings, like which microphone or camera to use.

When the participant that created the scene presses the `Start` button a command is sent (via the orchestrator) to make all application instances transition the the scene that belongs to the selected experience.

If you compare this scene to _Pilot0_ you will notice a few differences:

- This scene has a `VRTInitializer` that does global application initialization. This GameObject also has the `VRTConfig` component that has all the global configuration information (such as the URL at which to contact the orchestrator), and it has a `Tool_ScenarioRegistry` object that has the definition of the scenarios known by this copy of VR2Gather.

  The configuration settings can be overriden at runtime by placing a `config.json` file in the same directory as where the executable lives, or (for Unity Editor) next to the Assets folder.
  
  The VRTInitializer GameObject is placed in DontDestroyOnLoad, so subsequent scenes also have access to the global configuration.
- The `LoginController` does not have the session components (because there is no session yet), nor the PlayersManager.
-  It uses a pre-created self-player (for virtual camera, controllers and self-representation), `P_Self_login`.

The scene does have an `OrchestratorLogin` object, which contains the user interface plus the control logic for it.

There is also an `OrchestratorController` object, which has the code that handles session creation and joining, user preferences, etc. This object also contains the general code for communicating through other instances in the experience via the orchestrator, therefore this object is moved to the `DontDestroyOnLoad` objects so that it survives scene transitions.

### TechnicalPlayground

This is basically a copy of `Pilot0`, with some virtual artefacts added. Over time these artefacts come and go: this is the area where developers can try out new functionality.

As of this writing, this scene the following props (aside from the Pilot0 gadgets):

- A mirror where participants can see themselves and each other.
- A button that also makes pling sounds but cannot be moved.
- A small mudball that can be grabbed or thrown.
- A big mudball, it cannot be grabbed but if you push it it should move.
- A mudball generator: press the button to create a new mudball.

All of these gadgets should be coordinated: if one participant does something with an object all the other participants should see the result.

### SoloPlayground

This is similar to TechnicalPlayground, except it is single-user and does not require the orchestrator. Not all interaction works without the orchestrator but a lot does. And it is much easier to debug (for example) how an object attaches to your hand if you don't have to go through the whole sequence of creating a scene, starting it, etc. every time you have changed some code.

Generally, if you create a new interactable object you do this in a prefab. You then first test interacting with this prefab by putting it in SoloPlayground and interacting with it. Then you put it in TechnicalPlayground and check that its actions are coordinated between application instances. Only then do you put it in your own experience scene.

## Next steps

You should now have a basic understanding of the overall structure of a VR2Gather experience.

The [Prefabs](04-prefabs.md) section is a good continuation point, or the [Comparison to standard Unity VR](11-differences.md) section, or go back to the [Developer Overview](01-overview.md)
