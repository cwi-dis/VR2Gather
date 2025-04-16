# VR2Gather - Creating a new experience

First read the [Installation guide](Documentation/02-installation.md) and follow all steps there.

## How things are named in the code and documentation

There are a lot of terms going to be used in this document. So let us start by explaining them.

- **Scene** is a Unity term: all the `GameObjects` and such.
- A **Scenario** is something the participants can experience. It may consist of a single **Scene**, but it could have multiple scenes, with all participants going from one scene to the next together.
- A **Session** is a number of **Participants** experiencing a **Scenario**. One participant creates a **Session**, which is then advertised at the orchestrator. Other participants can then join that **Session**. At some point in time the creator starts the session, and then all participants start with the starting **Scene** of the **Scenario**.

## Creating a new experience

- You have followed the steps in the [Installation guide](Documentation/02-installation.md) and you have a Unity project with VR2Gather enabled. Now you want to add your own scenario. There are two options:
	- Create a new scene by copying the `Pilot0` scene. Subclass any component that needs different functionality (for example `PilotController`) and fix the scene to refer to the new component. Add your GameObjects, and remove GameObjects you don't need.
	- You already have a scene that works in "normal" Unity. This scene will have to be adapted for using VR2Gather. The most important step is that your VRRig and interaction GameObjects will have to be changed. The [Comparison to standard Unity practices](11-differences.md) document will explain.
	
- Now you need to create a _Scenario_. In the `LoginManager` there is a GameObject `Tool_ScenarioRegistry` with a `ScenarioRegistry` object. Here you add your scenario. **But**: it is best to create a new prefab variant `Tool_ScenarioRegistry_mine` and make your changes in there. See the _"preparing for updates"_ section in [Installing VR2Gather for Developers](02-installation.md).

  The `ScenarioID` must be globally unique (use a uuid-generator once). The `ScenarioSceneName` is the first Scene used (the one you created in the previous step). The `Name` and `Description` are for humans only: when the first participant creates a session they select this scenario. Other participants then see the name and description when they select the session to join it. 
- Next you need to ensure your scene is available for loading at runtime. In the _Build Settings..._ dialog you can add your scene to the list.
- You can now try your new scenario:
  - Open the `LoginManager` scene and _Play_ it.
  - Login to the orchestrator.
  - In the _Settings_ you can set your representation and microphone, this will be remembered between runs.
  - _Create_ a session.
  - Select your new scenario from the popup menu.
  - _Start_ the session.
- To try your scenario with two uses: The first user follows the steps above, the second user uses _Join_ to join the session that the first user created. And the first user waits until the second user has joined before selecting _Start_.

You probably need new interactable objects. Create these using one of the prefabs from the [Prefabs](04-prefabs.md) section as an example, make sure you copy materials and subclass any components that need changing).

Again, it is a good idea to create your new objects as prefab variants of `PFB_Grabbable` or `PFB_Trigger` or one of those: that way any future updates to the underlying logic by a new version of VR2Gather is automatically picked up by your application.

> You can now test these new interactables in `SoloPlayground` and `TechnicalPlayground`, as explained in the [Walkthrough](03-walkthrough.md) section. Then you add them to your scene.

If you need multiple scenes: your `PilotController` can open a new scene for you, and help you ensuring that if one participant goes to the new scene this also happens for all the other participants.


If you need additional functionality in your `P_Player` and `P_Self_player`, for example if you have multiple avatars that you want to switch between: subclass the PlayerControllers and provide the new functionality there. Then create variants of `P_Player` and `P_Player_Self`, reference the new controller and add any GameObjects you need. Finally references these new prefabs in your scene's `SessionPlayersManager`.

> xxxjack there is probably a lot more that should be said here, but I don't know what. I will update as I get requests for help.
