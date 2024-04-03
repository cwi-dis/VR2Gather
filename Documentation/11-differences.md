# VR2Gather - Comparison to standard Unity XR practices

In this section we will try to give a very quick overview in how using VR2Gather for development differs to standard Unity practices for VR experiences. It is intended to be read in parallel to [Prefabs](04-prefabs.md), or before, or after.

VR2Gather is used to create shared experiences, and the intention is that all users share the same experience. So, if user _Jack_ presses a switch that makes a light turn on, a user _Jill_ in the same session should also see the light turn on.

And if user _Jill_ walks or teleports to a different location in the scene then user _Jack_ should see the representation of user _Jill_ moving. No matter whether they go down a hill or not.

The low-level implementation of this is passing messages between the currently running instances within the session, and is handled by the orchestrator code, but there are a few things that you need to implement differently from standard Unity.

## VRRig and P\_Player\_self

In a normal Unity XR scene you will have a `VRRig` game object that contains your camera, your XR controllers, your character controller, input action manager, event system and all the other objects that represent "the player".

In VR2Gather all of this and more is contained in a prefab `P_Player_self`. The "and more" consists mainly of two things:

- The code to ensure that when your player does something (moving, teleporting, grabbing objects) this is transmitted to the other VR2Gather instances in the session, so the remote representation does the same thing.
- The code to transmit your audio-visual self representation (point cloud, video, but also conversational audio) to the other participants.

There is one important difference between `P_Player_self` and the usual way of how `VRRig` is used: the `P_Player_self` is _generally not created statically_ in the scene. It is added dynamically by the `SessionPlayersManager` script. 

This means that if you would usually create a static reference to, say, the `Camera` in some other scene game object you will now have to dynamically look for the object. The `SessionPlayersManager` can help you with this.

## P_Player

The `P_Player` prefab (which is a prefab on which `P_Player_self` is based) is the representation of another user in the scene.

Each `P_Player` will listen for messages from its corresponding `P_Player_self` in another instance of VR2Gather, and follow its movements and actions, and it will display its representation and play the incoming voice audio streams.

## Implementing user actions

Normally in Unity, if you have something like a button that can be pressed it will have an `XRSimpleInteractable` or so which will do a callback to the component that has to implement the action.

In VR2Gather there is an extra layer between this. A component `NetworkTrigger` is added to the game object, and `XRSimpleInteractable` should call its `Trigger` method. The `NetworkTrigger` will send a message to all instances of itself in all the other players in the scene. Then all the `NetworkTrigger` instances will call their `OnTrigger` action. So this is where you should add your implementation callback.

The effect of all this is that all participants will see the effect of the user interaction.

The prefab `OBJ_NetworkButton` is the simplest implementation of this.

## Grabbable objects

Objects that the user can grab also need an extra component (alongside the standard `XRGrabInteractable` and `RigidBody` and all that): a `VRTGrabbableController`. Again, this sends messages to other instances to ensure all participants have a consiste view of which player is holding which object in which hand.

## Scene changes

To change scenes, in stead of calling `SceneManager.LoadScene()` you should find your `PilotController` and call its `LoadNewScene`.

Moreover, you will probably want all participants to change scene at the same time, so you should use a `NetworkTrigger` to ensure this.

## Next steps

The [Prefabs](04-prefabs.md) section is a good continuation point, or go back to the [Developer Overview](01-overview.md)

