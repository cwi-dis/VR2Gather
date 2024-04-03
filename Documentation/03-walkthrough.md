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

Here is a diagram that paints a picture of how things fit together:

![](./VR2Gather-software-structure.pdf)

> We need to ensure that the picture above and the description of the modules below use the same naming convention.

## Unity Package Structure

The`nl.cwi.dis.vr2gather` package is structured into a number of _assemblies_, each with a toplevel folder (or near-toplevel folder), and each with its own substructure of `Scripts`, `Prefabs`, etc. There is also one external package that is indispensible.

In a roughly bottom-to-top order of dependencies these are:

- `VRTCore` has base classes and interfaces for general infrastructure, configuration storage and some things that didn't have anywhere else to go.
- `VRTInitializer` has a few more such classes, but is separate because of assembly dependency issues in Unity.
- `VRTMedia`, `VRTVideo` and `VRTProfiler` are probably historic artefacts.
- `VRTUI` has some user interface elements used in various places.
- `VRTUserPointClouds` has everything that has to do with using point clouds as your user representation (think: avatar).
- `VRTUserWebCam` is the implementation of an avatar that has a "screen" that shows the image from your webcam.
- `VRTUserVoice` has the code that allows participants to talk to each other in an experience.
- `VRTTransportSocketIO`, `VRTTransportDash` and `VRTTransportTCP` handle streaming of the user representation streams (from the previous set of assemblies) over the net, so that participants can see and hear each other.
- `VRTOrchestrator` has the communication code and the stubs to allow VR2Gather application instances to communicate via the orchestrator.
- `VRTCommon` has most of the implementation of the VR2Gather framework. The functionality and components here are described in the [Prefabs](04-prefabs.md) section.

You should have imported `VRT Essential Assets` into your `Samples`. This has things that are specific to a certain experience (for an experience that is included in the base repository), they are described in the _Scene Overview_ subsection, below.

## Scene Overview

The VR2Gather essential assets contains 4 main scenes:

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
