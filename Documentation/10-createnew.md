# VR2Gather - Creating a new experience

A new experience should have its own Git repository, but because VR2Gather is currently a framework that repository should be a submodule of the VR2Gather repo.

**NOTE**: it is very important that the main repository be open-sourceable, so please make very sure that you put no content there that you do not want to make available, or that you cannot share.

> We will move to a package-based structure in the future. This section will then change.


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

The new scene needs to be known to the orchestrator. For the time being: edit `PilotRegistry.cs` and return your new scene for one of the development pilots `"Development"`, `"Development 2"` or `"Development 3"`.

You probably need new interactable objects. Create these in your submodule (using one of the prefabs from the [Prefabs](04-prefabs.md) section as an example, make sure you copy materials and subclass any components that need changing).

You can now test these new interactables in `SoloPlayground` and `TechnicalPlayground`, as explained in the [Walkthrough](03-walkthrough.md) section. Then you add them to your scene.

If you need multiple scenes: you `PilotController` can open a new scene for you.

> xxxjack need to provide an example

If you need additional functionality in your `P_Player` and `P_Self_player`, for example if you have multiple avatars that you want to switch between: subclass the PlayerControllers and provide the new functionality there. Then create variants of `P_Player` and `P_Player_Self`, reference the new controller and add any GameObjects you need. Finally references these new prefabs in your scene's `SessionPlayersManager`.

> xxxjack there is probably a lot more that should be said here, but I don't know what. I will update as I get requests for help.
