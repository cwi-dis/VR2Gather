# VR2Gather — Avatar representations

This document explains how a rigged avatar follows the player (head, hands, height, feet), and how to make your own avatar with the *Setup Humanoid Avatar* tool.

## Overview

Each player prefab (`P_Player` for other participants, `P_Self_Player` for yourself) has two slots for avatar representations, `altRepOne` and `altRepTwo`. They are selected with the representations `AppDefinedRepresentationOne` and `AppDefinedRepresentationTwo` (in `config-user.json`, or in the settings of the login scene), and switched by `PlayerControllerBase.SetRepresentation()`.

By default these hold the two grey mannequins:

- `P_Mannequin` (`altRepOne`): the full mannequin.
- `MannequinHeadless` (`altRepTwo`): the same without a head, so when you use it yourself in VR you don't look at the inside of your own head.

Both are Humanoid avatars built with the *Setup Humanoid Avatar* tool described below, so your own avatars have exactly the same structure.

## How an avatar follows the player

### The player side: `PlayerTrackingTargets`

`PlayerTrackingTargets` sits on the player prefab root and holds the transforms an avatar needs. They live in `P_Player` and are inherited by `P_Self_Player`, so your own avatar and the way others see you use exactly the same points.

| Field | Wired to | Offset from the camera (eyes) |
|---|---|---|
| `head` | `CameraPositionOrientation/RiggingAttachPointHead` | y −0.09, z −0.10 |
| `neck` | `CameraPositionOrientation/RiggingAttachPointNeck` | y −0.16, z −0.12 |
| `headTop` | `CameraPositionOrientation/RiggingAttachPointHeadTop` | y +0.12, z −0.07 |
| `leftHand` | `HandsPositionOrientation/LeftHandPositionOrientation/RiggingAttachPointLeftHand` | maps the controller pose to the wrist |
| `rightHand` | `HandsPositionOrientation/RightHandPositionOrientation/RiggingAttachPointRightHand` | same, right side |
| `handModels` | the VR2Gather hand models under `HandsPositionOrientation` | hidden while an avatar (with hands of its own) is active |

The head and neck offsets come from the mannequin's proportions: its eyes are at the camera, its neck bone 16 cm lower and 12 cm further back. Because the attach points are children of the camera, they move along with the head: when you bend your head, the neck point stays roughly where your real neck is.

At runtime, `CameraPositionOrientation` and the `…HandPositionOrientation` objects are driven by the network for other participants (`PlayerNetworkControllerOther`), and for yourself by copying the XR camera and controllers (`PlayerNetworkControllerSelf`). That is also what gets sent to the others, so what you see of yourself is what they see of you.

### The avatar side: `PlayerRepresentationWirer`

`PlayerRepresentationWirer` sits on the avatar's root. In `OnEnable()` it finds `PlayerTrackingTargets` in its parents and:

- fills the `vrTarget` fields of `SyncSkeletonToVRRig` (head, neck, hands);
- sets `SizeAdjust.SourceTop` to the neck attach point and `SizeAdjust.SourceBottom` to the player root;
- subscribes `SizeAdjust.AdjustHeight` to `PlayerTrackingTargets.ViewAdjusted`, so a view adjust or reset measures the player again;
- hides the player's VR2Gather hand models (unless you switch off `hideHandModels`, which is handy to compare both sets of hands when checking hand alignment).

In `OnDisable()` it undoes the subscription and shows the hand models again. So the player prefab needs no references into its representations, and adding or swapping an avatar needs no changes to the player prefab.

Subclass `PlayerRepresentationWirer` and override `OnApply(PlayerTrackingTargets targets)` for app-specific setup, such as skin or hair colour. An avatar selection UI can call `Apply()` to apply it again after a change.

### Placement: `SyncSkeletonToVRRig`

`SyncSkeletonToVRRig` (in `LateUpdate`) positions the avatar:

- **Neck:** moves the *whole model* (the Animator's transform) so that its Neck bone lands on the neck attach point, and turns it to the head's direction (yaw only). This anchors the avatar.
- **Head:** a proxy object ("Head Constraint") that follows the head's direction, used to turn the body. It's deliberately not the head bone, because the neck mapping already moves the head along with the rest of the model.
- **Hands:** the Two Bone IK targets of the arms get the position and rotation of the hand attach points.

A Humanoid Animator resets the pose every frame, but relative to the model's own transform, so moving the model takes the whole skeleton along. Root motion is switched off, so the Animator doesn't move the model itself.

### Feet: `KeepFeetAboveGround`

The legs are Two Bone IK constraints whose targets (the ankles) are pinned by `KeepFeetAboveGround` to their rest height above the representation root, which sits at the player root, the floor. Horizontally the foot targets move along with the model. So when you crouch, the model moves down, the feet stay on the floor, and the IK bends the knees. No physics is involved.

### Size: `SizeAdjust`

`SizeAdjust` scales the representation root so that the avatar's neck height matches the player's:

```
scale = (neck attach point − player root) / (model Neck bone − model root, measured at Awake)
```

Because the size is measured at the same point that anchors the avatar (the neck), the feet land on the floor by construction.

When it measures:

- **On a view adjust or reset**, through `ViewAdjusted` (see above).
- **When the HMD starts tracking**, which also happens at the start of every scene. Only the self player does this: `P_Self_Player` overrides `setHeightOnHMDTracking` and the HMD tracking action ("XRI Head/Is Tracked") on its mannequins. The other participants receive your scale over the network (`BodySize` in `PlayerNetworkControllerSelf`/`Other`).
- **Not in desktop mode:** without an HMD, the avatar keeps scale 1.

The measurement reflects your posture at that moment: a reset while sitting gives a small avatar. If you stand up taller than you were at the measurement, the legs are fully stretched and the feet float a little.

## Making a new avatar

1. **Import the model as Humanoid.** In the model's import settings: *Rig > Animation Type: Humanoid*, *Avatar Definition: Create From This Model*, then *Apply*. Mixamo rigs map automatically; for other rigs check *Configure…*. Required bones: Hips, Neck, Head, and upper arm, lower arm, hand, upper leg, lower leg and foot on both sides.
2. **Make a wrapper.** Create an empty GameObject and put the model in it as a child, at the wrapper's origin (feet at y=0, facing +Z).
3. **Run the tool.** Select the wrapper and choose *Tools > VR2Gather Setup Humanoid Avatar*. Running it again first removes the previous setup, so you can run it again after changes.
4. **Save the wrapper as a prefab.**
5. **Put it in a player.** In a variant of `P_Player` (and/or `P_Self_Player`): add the avatar prefab as a child of the player root (inactive), and assign it to `altRepOne` or `altRepTwo` of `PlayerControllerOther`/`PlayerControllerSelf`.
6. **In a self player**, also set the avatar's `SizeAdjust` overrides as `P_Self_Player` does for the mannequins: tick `setHeightOnHMDTracking`, and set the HMD tracking action to the reference "XRI Head/Is Tracked". (Issue #360 is about moving this to the player, so it won't be needed any more.)
7. **For your own view in VR**, consider a headless variant of the model, like `MannequinHeadless`.

What the tool builds, under the wrapper:

- on the model: a `RigBuilder`, and `applyRootMotion` switched off;
- a "VR Constraints" child of the model, with a `Rig`, a "Head Constraint" proxy, and Two Bone IK for both arms and both legs (with hints for elbows and knees);
- `KeepFeetAboveGround` on both leg targets;
- a "SyncSkeletonToVRRig" child of the wrapper, wired to the model, the Neck bone, the head proxy and the arm targets;
- on the wrapper: `SizeAdjust` (measuring from the model root to the Neck bone) and `PlayerRepresentationWirer`.

All bones are found through the Humanoid mapping, so bone names don't matter, and all references end up in the prefab, visible in the Inspector. The `vrTarget` fields and the `SizeAdjust` sources stay empty: `PlayerRepresentationWirer` fills them at runtime. The setup code itself (`HumanoidAvatarSetup`) has no Editor dependencies, so it could also be used for avatars that are loaded at runtime (see issue #358).

## Known limitations and gotchas

- **Proportions:** the attach points are calibrated to the mannequin. An avatar with a different distance from neck to eyes is sized slightly off (about 1% per cm of difference), but its feet still land on the floor.
- **Generic rigs** are not supported by the tool. A Generic avatar can be built by hand with the same structure (see the list above), with the bones wired by hand.
- **Hand rotation 90° off:** `PlayerTrackingTargets.leftHand`/`rightHand` must be the `RiggingAttachPoint…Hand` objects, not the raw controller pose (`LeftHandPositionOrientation` itself, or `Left Controller`), otherwise the wrist offset is missing.
- **The animation stream only sees transforms that a constraint references directly.** `SyncSkeletonToVRRig` must move exactly the IK target objects, not a parent of them.
