# Port an existing VRTApp to the package

If you have created a VR2Gather application before April 2024 you have likely created it in the "old" structure, with your app as a submodule of VR2Gather.

This needs to be changed.

First step is to get your application completely up to date with the framework version of VR2Gather. Merge the current framework branch `historic/framework` (which is the old `master`) into your (development) branch. Check that everything works.

If at all possibly you should try to ensure that you have no **modifications** on your branch of the VR2Gather repo. Additions is fine, but modifications are going to cause a problem later.

There are going to be problems with the `Tool_scenarioRegistry`. Try the following:

- Rename to `Tool_scenarionRegistry_myApplication`. Move into the `Prefabs` of the submodule. 
- commit both repos. An example is at SHA `a5adb6f10`.
- Edit the `.meta` file, change the GUID.
- Now also change all occurrences of the old GUID, probably only in the `LoginManager` scene.
- commit both repos again. An example is at SHA `8296b0`.

It is probably a good idea to move all changed files from the toplevel repo (your branch) to the submodule repo.
 
You can then do a diff between the checked-out version of your branch and the master branch with a recursive diff program.
If the only differences are the GUIDs in things like the `LoginManager` scene you're going to be fine later on.

Tag your submodule with something like `historic/framework`. Push your main module branch to `historic/yourappname-framework`. Now everything has been saved so you can still run the old version.

Checkout a separate copy of your app repo (which was - until now - a submodule).


Now you need to ensure your `.gitattributes` and `.gitignore` are up-to-date. Compare with the one frmo `VR2Gather`.

Create an empty toplevel app _with a different name than your current folder name_. Maybe something like `VRTApp-MyWonderfulApp`. You can probably simply copy `VRTApp-TestGitPackage` from the VR2Gather repo.

Now you need to **move** everything from your old subfolder into your new app `Assets` folder. It is important that you move the `.meta` files too. Moving the whole second-level folder `MyWonderfulApp` into `VRTApp-MyWonderfulApp` may work.

Next you need to **copy** everything from your branch in the old VR2Gather repo that has been **added** there. Again, also copy the `.meta` files.

For the **modified** files I'm not sure what you should do.

Finally, you can try to open the new project in Unity.
You need to replace your ScenarioRegistry in the LoginManager scene by the correct one. You also need to add all needed scenes to the build settings. Now you can try to run your experience.


## Next steps

Go back to the [Developer Overview](01-overview.md)

