# Port an existing VRTApp to the package

If you have created a VR2Gather application before April 2024 you have likely created it in the "old" structure, with your app as a submodule of VR2Gather.

This needs to be changed.

First step is to get your application completely up to date with the framework version of VR2Gather. Merge the current framework branch (either `historic/framework` or if that doesn't exist yet `master`) into your (development) branch. Check that everything works.

If at all possibly you should try to ensure that you have no **modifications** on your branch of the VR2Gather repo. Additions is fine, but modifications are going to cause a problem later.

There are going to be problems with the `Tool_scenarioRegistry`. Try the following:

- Rename to `Tool_scenarionRegistry_myApplication`.
- commit.
- Edit the `.meta` file, change the GUID.
- Now also change all occurrences of the old GUID.

Tag your submodule with something like `historic/framework`. Push your main module branch to `historic/yourappname-framework`. Now everything has been saved so you can still run the old version.

Checkout a separate copy of your app repo (which is now a submodule).

Create an empty toplevel app _with a different name than your current folder name_. Maybe something like `VRTApp-MyWonderfulApp`. You can probably simply copy `VRTApp-TestGitPackage` from the VR2Gather repo.

Now you need to **move** everything from your old subfolder into your new app `Assets` folder. It is important that you move the `.meta` files too.

Next you need to **copy** everything from your branch in the old VR2Gather repo that has been **added** there. Again, also copy the `.meta` files.

For the **modified** files I'm not sure what you should do.



