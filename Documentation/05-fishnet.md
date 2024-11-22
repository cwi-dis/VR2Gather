# VR2Gather - Using Fish-net as network solution

### Getting started

In stead of adding the "normal" VR2Gather to your project with the package manager you add the fishnet branch, by using URL:

```
git+https://github.com/cwi-dis/VR2Gather?path=/nl.cwi.dis.vr2gather#vrtfishnet
```

Next import the samples.

### Playing around for the first time

If you create a session with the `Technical Playground` scenario you will see a lot of objects that you can interact with. The ones with the red buttons and the mudballs use VR2Gather objects, the ones with the blue buttons and the dirty snowball uses Fishnet objects.

### Adding Fishnet NetworkObjects

You can now add Fishnet networked objects, behaviours, prefabs and all that to your scene.

There is one example in `Assets/VRTAssets/Prefabs`:

- `FNOBJ_GrabbableSnowball` is a dirty snowball that is grabbable, and it can be used as a prefab for networked spawning.

There are two examples in `Packages/VR2Gather/Runtime/VRTFishnet/Prefabs`:

- `FNOBJ_Button` is a button. When pressed by anyone in the session it will make a "pling" sound for everyone. It uses the whole XRInteractable stuff.
- `FNOBJ_SnowballGenerator` will to a network-spawn of the snowball mentioned above.

The scripts in `Packages/VR2Gather/Runtime/VRTFishnet/Scripts` should be sufficiently generic that you can probably use them for your own purposes. Otherwise just add your own scripts to your project.

### How it works under the hood

On scenario start, if fishnet has been enabled in `Tool_SceneSetup`, the fishnet server is started on the machine that is the VR2Gather session master. 

> This will make all your fishnet objects disappear temporarily from your scene.

After a short delay (a few seconds) all machines will start the fishnet client, which all connect to the fishnet server using a Fishnet transport protocol implementation that uses VR2Gather orchestrator transport as the underlying mechanism.

> This will make all the fishnet objects re-appear in your scene, and they will now be synchronized.

All the VRT-Fishnet objects have a `debug` property. this will make them print verbose messages about what is happening to the log file.
