# Creating a new experience with a submodule

- Branch `master` to `development/transmixr_ch_mvp`
- Checkout branch
- Create `VRTApp_transmixr_ch_mvp` on github
	- It may be a good idea to add an empty readme or something now, before adding as a submodule
- ```git submodule add ../VRTApp_transmixr_ch_mvp Assets/PilotsExternal/VRTApp_transmixr_ch_mvp```
- `mkdir Assets/PilotsExternal/VRTApp_transmixr_ch_mvp/transmixr_ch_mvp`
- Create folder `Scenes` in the submodule
- Create empty scene
- Add `Tool_scenesetup`
- Remove camera
- Add to Build Settings "Scenes in Build"
- Add new scenario to `LoginManager` -> `Tool_ScenarioRegistry`
	- Ensure you get a new UUID!
	- Put it at the top of the list
- At this point you play with `git submodule add` and such until your `.gitmodules` and `git status` output look correct. :-)
- 

