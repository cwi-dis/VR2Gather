# VR2Gather — Replacing the Rigged Avatar in P_Mannequin

This document explains how to swap the default `P_Mannequin` placeholder avatar for a custom rigged humanoid avatar, and how the wiring system (`PlayerTrackingTargets` + `PlayerRepresentationWirer`) works so the avatar tracks the player's head and hands at runtime.

## Overview

Each player prefab (`P_Player`, `P_Self_Player`) has two avatar slots — `altRepOne` and `altRepTwo` — controlled by `PlayerControllerBase.SetRepresentation()`. By default both slots hold `P_Mannequin` (a Generic-rig placeholder). To use a custom avatar, you replace one or both slots with a prefab built around your rigged model.

The wiring between the player's VR tracking transforms and the avatar's IK targets is handled by two components:

- **`PlayerTrackingTargets`** — sits on the player prefab root; holds the five tracking transforms the avatar needs (`head`, `neck`, `headTop`, `leftHand`, `rightHand`).
- **`PlayerRepresentationWirer`** — sits on the avatar prefab root; on `OnEnable()` it walks up the parent hierarchy to find `PlayerTrackingTargets` and wires `SyncSkeletonToVRRig` and `SizeAdjust` automatically. No cross-prefab Inspector overrides are needed.

## Preparing the rigged avatar prefab

1. **Rig type**: Humanoid Mixamo rigs (e.g. Remy, Megan) work. Generic rigs require manual bone mapping in `SyncSkeletonToVRRig`.

2. **Required components on the avatar prefab root**:
   - `SyncSkeletonToVRRig` — maps VR tracking positions to IK targets inside the skeleton. Wire its `rigTarget` fields to bones inside the skeleton (head bone, neck bone, left/right wrist bones). Leave the `vrTarget` fields empty — `PlayerRepresentationWirer` fills them at runtime.
   - `SizeAdjust` — scales the avatar to match the player's real height. Wire `Destination`, `DestinationTop`, `DestinationBottom` to the corresponding bones/root inside the skeleton. Leave `SourceTop` and `SourceBottom` empty — `PlayerRepresentationWirer` fills them.
   - `PlayerRepresentationWirer` (or a subclass) — the self-wiring component. No Inspector fields to fill; it finds `PlayerTrackingTargets` via `GetComponentInParent` at runtime.

3. **Subclassing `PlayerRepresentationWirer`**: Override `OnApply(PlayerTrackingTargets targets)` to add app-specific setup such as skin tone or hair colour changes after the tracking wiring is done. The avatar selection UI can call `Apply()` to re-apply after a user change.

## `PlayerTrackingTargets` — what each field means

| Field | What to wire | Notes |
|---|---|---|
| `head` | `RiggingAttachPointHead` | Child of `HeadPositionOrientation`; y:-0.12, z:-0.07 offset from HMD |
| `neck` | `RiggingAttachPointNeck` | Child of `HeadPositionOrientation`; y:-0.23, z:-0.17 offset |
| `headTop` | `RiggingAttachPointHeadTop` | Child of `HeadPositionOrientation`; y:+0.12 — used for height measurement by `SizeAdjust` |
| `leftHand` | `RiggingAttachPointLeftHand` | Child of `Left Controller` (P_Self_Player) or child of `LeftHandPositionOrientation` (P_Player). Has a pre-baked rotation offset that maps controller space to wrist space. |
| `rightHand` | `RiggingAttachPointRightHand` | Same as leftHand, right side. |

**Important**: for `P_Self_Player`, `leftHand`/`rightHand` must point to the `RiggingAttachPoint*Hand` that is a child of the **XR controller GO** (`Left Controller` / `Right Controller`), not the one inherited from P_Player's `LeftHandPositionOrientation`. The XR-controller attach points have the correct pre-baked rotation for mapping controller orientation to avatar wrist orientation.

## How tracking flows at runtime

### Other players (P_Player)
```
Network data → PlayerNetworkControllerBase
  → drives HeadPositionOrientation.position/rotation
      → RiggingAttachPointHead (child, fixed offset)  ← PlayerTrackingTargets.head
      → RiggingAttachPointNeck (child, fixed offset)  ← PlayerTrackingTargets.neck
      → RiggingAttachPointHeadTop (child)             ← PlayerTrackingTargets.headTop
  → drives LeftHandPositionOrientation.position/rotation
      → RiggingAttachPointLeftHand (child)            ← PlayerTrackingTargets.leftHand
```

### Self player (P_Self_Player)
```
XR camera → Main Camera (auto-tracked)
  → RiggingAttachPointHead (child)    ← PlayerTrackingTargets.head
  → RiggingAttachPointNeck (child)    ← PlayerTrackingTargets.neck
  → RiggingAttachPointHeadTop (child) ← PlayerTrackingTargets.headTop

XR controller → Left Controller (auto-tracked)
  → RiggingAttachPointLeftHand (child) ← PlayerTrackingTargets.leftHand

PlayerNetworkControllerSelf.Update() also copies:
  camTransform → HeadPositionOrientation  (so P_Player-inherited avatars track)
  LeftHandTransform → LeftHandPositionOrientation  (same reason)
```

## Wiring checklist for a new avatar prefab

- [ ] `SyncSkeletonToVRRig`: all `rigTarget` fields wired to skeleton bones; `vrTarget` fields left empty
- [ ] `SizeAdjust`: `Destination`, `DestinationTop`, `DestinationBottom` wired; `SourceTop`/`SourceBottom` left empty
- [ ] `PlayerRepresentationWirer` (or subclass) added to avatar root
- [ ] Avatar prefab placed as `altRepOne` or `altRepTwo` in the player prefab
- [ ] `PlayerTrackingTargets` on player root has all five fields wired (already done in `P_Player` and `P_Self_Player`)

## Known gotchas

- **Hand rotation 90° off**: if `leftHand`/`rightHand` in `PlayerTrackingTargets` point to the wrong `RiggingAttachPointLeftHand` (e.g. the one inherited from P_Player rather than the one under `Left Controller`), the pre-baked rotation offset will be wrong and the avatar hands will appear twisted.
- **`PlayerRepresentationWirer` fires on first enable**: if the avatar GO is active when the player prefab is instantiated, `OnEnable` fires before `PlayerTrackingTargets` is fully set up. Ensure the player prefab has `PlayerTrackingTargets` wired before the avatar GO is activated (it always is in the shipped prefabs).
- **Generic vs Humanoid rig**: P_Mannequin uses a Generic rig; Remy/Megan use Humanoid Mixamo rigs. There is no automatic retargeting — `SyncSkeletonToVRRig` must be wired to the correct bones for each rig.
