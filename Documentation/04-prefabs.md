# VR2Gather - Important prefabs and scripts

It is suggested you read the [Walkthrough](03-walkthrough.md) section before this section, to get an idea of the overall structure of a VR2Gather experience.

Most of the functionality described in this section is part of the `Assets/Pilots/Common` assembly. We will describe it roughly in order of complexity, simplest first.

## Virtual Objects

The VR2Gather virtual objects are more complex than "normal" Unity virtual objects, because they need to behave consistently for the different participants in an experience: if one participant moves an object the other partiipants should see that move.

> We are investigating using some standard Unity shared game package like `netcode` for this, but most seem to be server-based. But this section may change in the future.

### PFB_Button

This is a button. A participant can press the button and it will make a sound (for everyone).

The button has an `AudioSource` that is the "business logic component" of it: it makes a sound.

It has a VR2Gather  `NetworkTrigger` component that ensures it behaves consistently (each participant's button makes a sound): in a normal Unity scene you would call `AudioSource.PlayOneShot()` directly from the interaction or collision handler but for VRTogether you call `NetworkTrigger.Trigger()`. **Note the spelling**. This will communicate with the other instances, and all instances will call the `OnTrigger()` UnityEvent. This is where you link your AudioSource.PlayOneShot, so now all participants hear the sound.

There is one extra bit of VR2Gather-specific code here: the `Trigger` component (yes, I know, third time we use this word:-). The `XRSimpleInteractable` component handles ray-based interaction, but we want participants to be able to use their finger for direct interaction, and for this we need the collider callback methods. These are in `Trigger`.

Some things to note about this prefab:

- It is in layer `TouchableObject` (but it may be that this is for historic reasons nowadays)
- The collider has `IsTrigger` set
- The XRSimpleInteractable has Interaction Layer Mask `Pointable`, references the collider mentiond above and ints interactable event Activate goes to `Trigger.OnActivate`.
- The XRSimpleInterctable should have Interaction Manager `null`, it will be set correctly after the self-player has been instantiated.

### Teleportable areas

Pilot0 has examples of how to create teleportable areas (the floor) and a teleportable anchor. The latter is an easter egg: one of the tables can be teleported on to.

There is no special VR2Gather code needed.

### MudBall

> Note: grabbable objects (below) that are thrown don't currently behave as they should. When thrown, only the person that did the throwing will see the object behave logically. Other session participants will simply see it dropping on the floor.

This is an object that behaves consistently among participants. The idea is that the master instance will control the behaviour and the other instances will simply follow.

The `RigidBody` and `RigidBodyNetworkController` take care of this.

### GrabbableMudball

This is a variant of MudBall that can be grabbed. While grabbed all participants will see it move with the person holding it, and when dropped everyone will see it in its new location.

The Rigidbody, Collider and XRGrabInteractable follow the standard Unity idiom. The only VR2Gather-specific component is `Grabbable`, which ensures the object shares its position and orientation while it is grabbed.

### MudBallGenerator

This object creates mudballs. The structure of the button is the same as for PFB_Button, but it doesn't have a `NetworkTrigger`.

In stead there is an object MudballGenerator with a `NetworkInstantiator` component, and pressing the button calls its `Trigger()` method. This class shares a base class with NetworkTrigger. The NetworkInstantiator will always send the instantiation request to the _master_ participant, which will instantiate the prefab (a mudball, in this case), assign it a unique identifier, and then lets all other participants create the object with the same unique identifier. This way, when you create multiple mudballs they share the correct state.

The prefab `GaneratableMudball` is a variant of `GrabbableMudball` with one difference: it has the `NoAutoCreateNetworkId` field set (because its network ID will be created by script, in the master).

There is also a `MudballLocation` which is where new mudballs appear.

### PFB_Pilot0ButtonObject

An example of how to create an object that is both grabbable and clickable/pointable.

## Avatars for participants

The `P_Player` prefab is what is instantiated for every _other_ player. Note the emphasis on _other_: these are created for all players except for the current user.

The `PlayerControllerOther` component controls which of the possible representations for this participant is currently in use. The standard options are:

- _Avatar_: a very simple avatar with only a red body and a grey head. The head direction follows the HMD of the participant that this player represents.
- _WebCam_: similar, but the head is replaced with a "tv screen" that shows a feed from the webcam of the participant that this player represents.
- _PointCloud_: a 3D volumetric representation, streamed from RGBD cameras around the participant that this player represents.
- _Voice_: in addition to the previous avatars this "representation" transmits audio from the participant microphone, to be played out as a 3D audio source.

The `PlayerNetworkControllerOther` synchronises participant position and orientation. It receives messages from the corresponding `PlayerNetworkControllerSelf` for this participant, and ensure that the avatar is in the same location and has the same orientation.

The `OtherPlayerHands` gameobject has a left and right hand with a `HandNetworkControllerOther` component that does the same for the hands of the participant. In addition it manages what the hand looks like (neutral, pointing, grabbing) and whether it holds an object currently.

The `Synchronizer` and `TileSelector` we won't document right now.

## Camera, XRRig, XROrigin, self-representation

The `P_Self_player` prefab is the VR2Gather equivalent of a normal Unity Camera+interaction manager (for non-VR applications) or XRRig+XROrigin (for VR applications).

Because it also contains the self-representation, and because this self-representation is very similar to the representation of other participants this is a variant of the `P_Player` prefab, but with the `OtherPlayerHands` disabled, some of the components replaced and a lot of interaction stuff added.

The toplevel object has a `PlayerControllerSelf` and `PlayerNetworkControllerSelf` which do the "other side" of the things explained in `P_Player`, for example _transmitting_ player position and HMD orientation in stead of receiving it.

The `XROrigin` is also on the toplevel object.

The `CameraReference` GameObject replaces that standard Unity XRRig. A few differences are worth mentioning:

- Each hand has a `HandNetworkControllerSelf` that sends position and object grabbed to the other instances.
- Each hand has a _Ray Based Interaction_ and _Direct Interaction_, and the `HandVisualController` allows switching between them. This changes the representation (controller or hand) as well as the interactions (ray-based or point-and-grab based).
- The `P_Handsfree` prefab handles (limited) control over a VR2Gather experience with keyboard and mouse.

The `LocomotionSystem` GameObject is fairly standard but has the component `FixInteractables` which ensures that interactables and the interatcion manager and teleport provider are linked correctly (because our XRRig is created after the scene has already started).

The `P_HeadsUpDisplay` handles the popup that shows error messages or praticipant commands.

## Next

I _think_ that that covers all important functionality, insofar as it is important to VR2Gather experience developers. Let me know if things are missing.

Next read the section on [Creating a new experience](10-createnew.md) or go back to the [Developer Overview](01-overview.md).
