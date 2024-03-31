# VR2Gather - Creating a new experience

A new experience should have its own Git repository, but because VR2Gather is currently a framework that repository should be a submodule of the VR2Gather repo.

**NOTE**: it is very important that the main repository be open-sourceable, so please make very sure that you put no content there that you do not want to make available, or that you cannot share.

> We will move to a package-based structure in the future. This section will then change.

## How things are named in the code and documentation

There are a lot of terms going to be used in this document. So let us start by explaining them.

- _Scene_ is a Unity term: all the `GameObjects` and such.
- A _Scenario_ is something the participants can experience. It may consist of a single _Scene_, but it may have multiple scenes, with all participants going from one scene to the next together.
- A _Session_ is a number of people experiencing a _Scenario_. Some participant creates a _Session_, which is then advertised at the orchestrator. Other participants can then join that _Session_. At some point in time the creator starts the session, and then all participants start with the starting _Scene_ of the _Scenario_.

## Creating a new experience

Follow these steps to start a new experience, lets say `MyNewExperience`:

1. Create a branch `develop/MyNewExperience` and check out.
2. Separately, for example on Github, create a repo `VR2GApp-MyNewExperience`. In there, create a directory `MyNewExperience` and possibly a toplevel readme. All your assets should go under the subdirectory.
3. In the VR2Gather folder, add the new submodule with

   ```
   git submodule add ../VR2GApp-MyNewExperience Assets/PilotsExternal/VR2GApp-MyNewExperience
   ```
   
   Note the relative URL to refer to the submodule: this will make your life easier in the futre, if we have to move git providers.
   
Now you can create subdirectories `Scenes`, `Prefabs`, `Scripts`, etc. under `Assets/PilotsExternal/VR2GApp-MyNewExperience/MyNewExperience` (**note** the path).

Create a new scene by copying Pilot0, for example. Subclass any component that needs different functionality (for example `PilotController`) and fix the scene to refer to the new component.

Now you need to create a _Scenario_. In the `LoginManager` there is a GameObject `Tool_ScenarioRegistry` with a `ScenarioRegistry` object. Here you add your scenario. The `ScenarioID` must be globally unique (use a uuid-generator once). The `ScenarioSceneName` is the first Scene used (the one you created in the previous step). The `Name` and `Description` are for humans only: when the first participant creates a session they select this scenario. Other participants then see the name and description when they select the session to join it. 

You probably need new interactable objects. Create these in your submodule (using one of the prefabs from the [Prefabs](04-prefabs.md) section as an example, make sure you copy materials and subclass any components that need changing).

You can now test these new interactables in `SoloPlayground` and `TechnicalPlayground`, as explained in the [Walkthrough](03-walkthrough.md) section. Then you add them to your scene.

If you need multiple scenes: your `PilotController` can open a new scene for you.

> xxxjack need to provide an example

If you need additional functionality in your `P_Player` and `P_Self_player`, for example if you have multiple avatars that you want to switch between: subclass the PlayerControllers and provide the new functionality there. Then create variants of `P_Player` and `P_Player_Self`, reference the new controller and add any GameObjects you need. Finally references these new prefabs in your scene's `SessionPlayersManager`.

> xxxjack there is probably a lot more that should be said here, but I don't know what. I will update as I get requests for help.
