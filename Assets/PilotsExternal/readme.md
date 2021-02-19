# Placeholder for more pilots

This directory is a placeholder (and this readme file
to ensure the directory exists and gets a .meta and all that).

Checkout the right branch in VRTApplication, then do something like

```
cd vrtapplication
git submodule add ../VRTApp-cwi_cake.git Assets/PilotsExternal/CWI_cake
```

Note that using a relative URL is better that an absolute URL: this will keep things working when we
move to another git host.

You may also have to add a reference to _PilotRegistry.cs_ and you make have to use _File_ -> _Build Settings_ -> _Scenes in Build_ to include the scenes in the build.

The suggested usage of submodules is that we _don't_ include any submodules on the main branches (_develop_, _master_) but in stead
create a development and deployment branch per Pilot (or group of pilots). On those branches we then do the `git submodule add`,
so the main branches are "uncontaminated".
