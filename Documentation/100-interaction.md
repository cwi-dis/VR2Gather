# VR2Gather - Interaction System

This document describes how users interact with objects in VR2Gather: what actions are supported, how the player rig implements them across different input modes, and how interactable objects are structured.

The **TechnicalPlayground** sample scene (`Samples~/VRTAssets/Scenes/TechnicalPlayground/`) is the canonical reference for all interaction patterns. It contains working examples of every interactable type described here and is the best place to test interaction behaviour.

## Design philosophy: two primary actions

VR2Gather is designed around human-to-human interaction in XR. Given that focus, the set of meaningful physical actions a user can perform is deliberately narrow. We consider two primary actions:

- **Pressing a button** — a momentary activation, like pushing a physical switch
- **Holding an object** — picking something up, carrying it, and putting it down

Everything else (locomotion, teleport, UI navigation) is secondary infrastructure. This two-action philosophy is reflected directly in the two base interactable prefabs: `PFB_Trigger` (button) and `PFB_Grabbable` (held object).

The original implementation used VR controllers to emulate hands: poke with the index finger to press a button, squeeze the grip button to grab an object. That physical metaphor has guided the design ever since.

---

## Interactors: how the player performs actions

All interactors live inside the `P_Self_Player` prefab. VR2Gather supports several input modes, described below.

### XR controllers — direct (near) interaction

The primary use case. Each hand has a **NearFarInteractor** (from XRIT 3.x) operating in near mode. Grabbing an object works by reaching out and squeezing; the object attaches to the hand. Each hand also has a **PokeInteractor** for pressing buttons — physically poking the button collider with the fingertip of the controller.

### XR controllers — ray (far) interaction

The same NearFarInteractor, now operating in far mode. A ray extends from the controller into the scene. Grabbing a distant object pulls it to the hand. Pressing a button with the ray requires pointing at it and pressing the trigger — this works but the feel is less polished than near/poke interaction.

### Hand tracking

Hand tracking in XR should work via the same NearFarInteractor and PokeInteractor as controllers, since XRIT 3.x handles both transparently. In practice the implementation is incomplete and not well tested.

### Desktop — keyboard and mouse

Originally added as a debug mode so development could proceed without a headset; now a supported (if secondary) input path. Implemented by the **`P_Handsfree`** prefab, which is a child of `P_Self_Player`.

`P_Handsfree` uses a plain **`XRRayInteractor`** (not NearFarInteractor). The ray originates from a virtual "hand" at the lower-right of the screen and points toward the mouse cursor. Interaction is activated by holding the **Alt key**:

| Input | Action |
|-------|--------|
| Alt + left click | Press button |
| Alt + right click (hold) | Grab object; release to drop |

### Gamepad (non-functional)

A skeleton for controlling the interaction ray with a joystick (no keyboard/mouse) exists inside `P_Handsfree`. The ray direction would be steered with a thumbstick ("Sweeping" input action). This mode is not currently functional end-to-end.

### Unused XRIT sample components

Two components were imported as part of the XRIT sample rig and have never been used in VR2Gather:

- **`XRGazeAssistance`** — disabled (`m_Enabled: 0`), all ray interactor slots empty; completely inert.
- **`XRClimbTeleportInteractor`** — enabled, but requires `XRClimbInteractable` objects in the scene, which VR2Gather does not have; inert.

Both can be ignored.

---

## Interactables: how objects respond to actions

_This section will be expanded. The four categories described below are the current taxonomy._

### Grabbable objects — `PFB_Grabbable`

Objects the user can pick up, carry and put down. The mudball (`OBJ_GrabbableMudball`) is the canonical example. All grabbable objects should derive from the `PFB_Grabbable` base prefab.

Key components beyond the standard Unity `XRGrabInteractable` + `Rigidbody`:

- **`VRTGrabbableController`** — synchronises the held object's position and orientation to all other participants while it is being carried.
- **`RigidBodyNetworkController`** — synchronises physics state (position, velocity) when the object is not held.

### Static buttons — `PFB_Trigger` / `OBJ_NetworkButton`

Buttons the user can press to trigger a networked action. `OBJ_NetworkButton` is the standard prefab for a standalone button; the interaction logic lives in `PFB_Trigger` inside it.

Key component: **`NetworkTrigger`** — when the button is pressed locally, `NetworkTrigger.Trigger()` is called. This broadcasts the event to all participants, and all instances fire their `OnTrigger` UnityEvent callback. See also [Differences from standard Unity XR](11-differences.md).

### Grabbable objects with a button — `PFB_Grabbable` + `PFB_Trigger`

Objects that can be both picked up and activated. Examples: the clicker objects on the tables in TechnicalPlayground. These combine a `PFB_Grabbable` body with a `PFB_Trigger` child for the pressable part.

> Not all objects in this category currently derive cleanly from `PFB_Grabbable`; this is a known inconsistency.

### Factory objects — `NetworkInstantiator`

Objects whose button creates a new networked object rather than triggering a one-shot event. The mudball generator (table 3) is a pure factory. The camera (table 2) is a more complex example: it is grabbable, has a trigger button, and is also a factory — pressing its button produces a photograph object.

When the button is pressed, the request is routed to the **session master**, which instantiates the prefab, assigns it a unique network ID, and broadcasts the creation to all participants. This ensures every participant ends up with an identical, consistently-identified copy of the new object.

Key component: **`NetworkInstantiator`** (shares a base class with `NetworkTrigger`). The spawned prefab needs special setup so that its network ID is assigned by the master at instantiation time rather than auto-generated.

---

## Summary tables

### Interaction modes

| Mode | Grab action | Button action | Status |
|------|-------------|---------------|--------|
| XR controllers, near | NearFarInteractor (near mode) | PokeInteractor | Primary, fully supported |
| XR controllers, far | NearFarInteractor (far mode) → pulls to hand | Ray + trigger | Works, feel needs polish |
| Hand tracking | Same as controllers | Same | Incomplete |
| Desktop (keyboard+mouse) | Alt + right-click (hold) | Alt + left-click | Supported |
| Gamepad | Joystick-steered ray | Ray + button | Not functional |

### Interactable object types

| Object type | Base prefab | Key VR2Gather component | Example |
|-------------|-------------|------------------------|---------|
| Grabbable | `PFB_Grabbable` | `VRTGrabbableController` | OBJ_GrabbableMudball |
| Static button | `PFB_Trigger` / `OBJ_NetworkButton` | `NetworkTrigger` | Blue button, table 4 |
| Grabbable + button | `PFB_Grabbable` + `PFB_Trigger` child | Both of the above | Clickers, camera |
| Factory | `PFB_Trigger` + `NetworkInstantiator` | `NetworkInstantiator` | Mudball generator |
| Grabbable + button + factory | All three combined | All three | Camera (produces photographs) |

---

## Next steps

Read [Creating a new experience](10-createnew.md) for how to wire these patterns into a new scene, or go back to the [Developer Overview](01-overview.md).
