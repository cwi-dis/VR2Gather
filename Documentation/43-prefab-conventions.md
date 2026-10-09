# VR2Gather - Prefab conventions and editing workflow

In VR2Gather much of the application logic lives in the wiring between GameObjects: UnityEvents calling methods on other components, prefab variants, overrides. Unlike C# code there is no compiler and no structure to keep that wiring tidy, so this document collects the conventions we follow, and the workflow we use when prefabs are edited by a script or a coding agent (through the Unity CLI `unity` and the `com.unity.pipeline` package).

The conventions apply to everyone editing prefabs, by hand or otherwise. The list grows: when a review turns up a rule that wasn't written down yet, add it here.

## Structuring conventions

- **Reuse through variants and nested prefabs.** Never copy a prefab and never unpack one to change it. A variant or a nested instance keeps receiving fixes from its base.
- **Generic in the prefab, scene-specific in the scene.** Anything every use of a prefab needs belongs in the prefab (or variant). Anything that depends on the scene stays an override on the scene instance: references to scene objects (such as the `PilotController`), labels, colours, positions.
- **Wire local sources to network entry points.** A local source (an interactable's `SelectEntered`/`Activated`, your own code, a non-networked UI button) calls `NetworkTrigger.Trigger()`. A `NetworkTrigger`'s `OnTrigger` fires on *every* participant, so it must never call another `NetworkTrigger.Trigger()`: one press would become one event per participant.
- **Remove components that become dead in a variant.** An unused component invites someone to wire to it. For example, `OBJ_BarrierNetworkButton` removes the button's own `NetworkTrigger`, because the press goes to the barrier's Ready trigger instead.
- **NetworkIds are empty in prefab assets.** Only scene instances have them; `NetworkIdBehaviour` fills them in. A NetworkId in a prefab asset means every instance shares it.

## Workflow for building a prefab

1. **Create the skeleton asset-first.** Make the new prefab or variant without placing it in a scene, so it starts with a clean root transform, no scene references and no NetworkIds. The skeleton contains only the name.
2. **Place an instance in a scene** where it can be tested.
3. **Iterate on the instance**, and decide for each change whether it is generic or scene-specific. Apply the generic ones to the prefab (Overrides → Apply to the *variant*, not to its base); keep the scene-specific ones as overrides.
4. **Test, then save the scene.**

When a coding agent does the editing, the division of labour is:

- The agent describes the change structurally before making it: which objects, components and events change, and where (base, variant, or scene override).
- The agent makes the changes on the instance and proposes a classification (generic or scene-specific) for each override.
- The developer applies the generic overrides through the Inspector's Overrides dropdown. Applying *is* the structural decision, so this is also the review.
- The agent reads the result back from the Editor (wiring, overrides, NetworkIds), cleans up what Unity leaves behind (see below), and reports.
- Nothing is committed that the developer hasn't inspected.

## Pitfalls when editing prefabs from scripts

These surfaced while building `OBJ_BarrierNetworkButton` (#319, #351). Unity gives no error for any of them, so always read the result back.

- **Values set from a script on a prefab instance aren't automatically overrides.** Change instances through `SerializedObject`, or call `PrefabUtility.RecordPrefabInstancePropertyModifications()` afterwards, or the change may be lost on save.
- **Applying references into a freshly applied object fails silently.** After applying an added GameObject to a prefab, references from other overrides into that object arrive in the prefab as `null`. Apply added objects first, in a separate step, then the rest.
- **Removing a component leaves its overrides behind.** They remain as modifications with a `null` target. Remove them with `PrefabUtility.GetPropertyModifications()` / `SetPropertyModifications()`.
- **Watch for NetworkIds in prefab assets.** Before 1.4.4, `NetworkIdBehaviour` invented NetworkIds in preview scenes (`PrefabUtility.LoadPrefabContents`) and recorded them as overrides. That is fixed, but check the prefab file for non-empty `NetworkId` values after every edit anyway.

Back to the [Developer Overview](01-overview.md).
