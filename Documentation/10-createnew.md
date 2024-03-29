# VR2Gather - Creating a new experience

A new VR2Gather experience should have its own Git repository and include VR2Gather as a Unity package.

The [toplevel README file](../README.md) has the currently valid set of steps you need to take.

## How things are named in the code and documentation

There are a lot of terms going to be used in this document. So let us start by explaining them.

- **Scene** is a Unity term: all the `GameObjects` and such.
- A **Scenario** is something the participants can experience. It may consist of a single **Scene**, but it could have multiple scenes, with all participants going from one scene to the next together.
- A **Session** is a number of **Participants** experiencing a **Scenario**. One participant creates a **Session**, which is then advertised at the orchestrator. Other participants can then join that **Session**. At some point in time the creator starts the session, and then all participants start with the starting **Scene** of the **Scenario**.

## Creating a new experience

- Create a repo `VRTApp-MyNewExperience`. In there, create a Unity project `VRTApp-MyNewExperience` and possibly a toplevel readme. Follow the steps in the [toplevel README file](../README.md) to add VR2Gather to your project.
- Create a new scene by copying Pilot0, for example. Subclass any component that needs different functionality (for example `PilotController`) and fix the scene to refer to the new component.
- Now you need to create a _Scenario_. In the `LoginManager` there is a GameObject `Tool_ScenarioRegistry` with a `ScenarioRegistry` object. Here you add your scenario. The `ScenarioID` must be globally unique (use a uuid-generator once). The `ScenarioSceneName` is the first Scene used (the one you created in the previous step). The `Name` and `Description` are for humans only: when the first participant creates a session they select this scenario. Other participants then see the name and description when they select the session to join it. 

You probably need new interactable objects. Create these using one of the prefabs from the [Prefabs](04-prefabs.md) section as an example, make sure you copy materials and subclass any components that need changing).

You can now test these new interactables in `SoloPlayground` and `TechnicalPlayground`, as explained in the [Walkthrough](03-walkthrough.md) section. Then you add them to your scene.

If you need multiple scenes: your `PilotController` can open a new scene for you.

> xxxjack need to provide an example

If you need additional functionality in your `P_Player` and `P_Self_player`, for example if you have multiple avatars that you want to switch between: subclass the PlayerControllers and provide the new functionality there. Then create variants of `P_Player` and `P_Player_Self`, reference the new controller and add any GameObjects you need. Finally references these new prefabs in your scene's `SessionPlayersManager`.

> xxxjack there is probably a lot more that should be said here, but I don't know what. I will update as I get requests for help.
