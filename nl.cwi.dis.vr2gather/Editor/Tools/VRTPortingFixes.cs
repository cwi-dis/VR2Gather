using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace VRT.Tools
{
    /// <summary>
    /// Fixers for the mechanical problems the Porting Check finds (#383).
    ///
    /// Each fixer works on a scene, changes instances in place (so prefab instances get overrides),
    /// records Undo and marks the scene dirty, but does not save it. With dryRun it only reports
    /// what it would change. Anything that doesn't match a known old pattern is reported, not changed.
    ///
    /// This file only uses Unity and (through reflection) XRI types, so it can also be run
    /// standalone against an application project:
    ///   unity command run_script --file .../VRTPortingFixes.cs --entry VRT.Tools.VRTPortingFixes.FixPhysicsLayersInActiveScene --args '[true]'
    /// </summary>
    public static class VRTPortingFixes
    {
        const int DefaultPhysicsLayer = 0;
        const uint DefaultInteractionBit = 1u;
        const uint TeleportInteractionBit = 1u << 31;
        const uint OldInteractableBits = 2u | 4u;
        const uint OldTeleportBit = 8u;

        // ── Physics layers ──────────────────────────────────────────────────────────

        public static string FixPhysicsLayersInActiveScene(bool dryRun) => FixPhysicsLayers(SceneManager.GetActiveScene(), dryRun);

        public static string FixPhysicsLayers(Scene scene, bool dryRun)
        {
            var report = new Report("Physics layers", scene, dryRun);
            foreach (var go in AllObjects(scene))
            {
                if (go.layer != 28 && go.layer != 29) continue;
                report.Change(go, $"layer {go.layer} → {DefaultPhysicsLayer}");
                if (dryRun) continue;
                Undo.RecordObject(go, "Fix physics layer");
                go.layer = DefaultPhysicsLayer;
            }
            return report.Finish();
        }

        // ── Interaction layers (including teleportation areas) ──────────────────────

        public static string FixInteractionLayersInActiveScene(bool dryRun) => FixInteractionLayers(SceneManager.GetActiveScene(), dryRun);

        public static string FixInteractionLayers(Scene scene, bool dryRun)
        {
            var report = new Report("Interaction layers", scene, dryRun);
            var interactableType = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable, Unity.XR.Interaction.Toolkit");
            var teleportType = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.BaseTeleportationInteractable, Unity.XR.Interaction.Toolkit");
            if (interactableType == null || teleportType == null)
                return report.Skip("XRI not installed");

            foreach (var go in AllObjects(scene))
            {
                foreach (var comp in go.GetComponents(interactableType))
                {
                    var so = new SerializedObject(comp);
                    var bits = so.FindProperty("m_InteractionLayers.m_Bits");
                    if (bits == null) continue;
                    uint oldBits = (uint)bits.longValue;
                    bool isTeleport = teleportType.IsInstanceOfType(comp);
                    uint newBits;

                    if (isTeleport)
                    {
                        if ((oldBits & TeleportInteractionBit) != 0 && (oldBits & (OldTeleportBit | OldInteractableBits)) == 0)
                            continue;
                        if ((oldBits & OldInteractableBits) != 0 || (oldBits & OldTeleportBit) == 0)
                        {
                            report.Unknown(go, $"{comp.GetType().Name} has interaction bits {oldBits}");
                            continue;
                        }
                        newBits = (oldBits & ~OldTeleportBit) | TeleportInteractionBit;
                    }
                    else
                    {
                        if ((oldBits & (OldInteractableBits | OldTeleportBit)) == 0)
                            continue;
                        if ((oldBits & OldTeleportBit) != 0)
                        {
                            report.Unknown(go, $"{comp.GetType().Name} has interaction bits {oldBits}");
                            continue;
                        }
                        newBits = (oldBits & ~OldInteractableBits) | DefaultInteractionBit;
                    }

                    report.Change(go, $"{comp.GetType().Name} interaction bits {oldBits} → {newBits}");
                    if (dryRun) continue;
                    bits.longValue = newBits;
                    so.ApplyModifiedProperties();
                }
            }
            return report.Finish();
        }

        // ── PilotController prefab references ───────────────────────────────────────

        const string ConfigurationPrefabPath = "Packages/nl.cwi.dis.vr2gather/Runtime/VRTCore/Prefabs/Tool_VRTInitializer.prefab";
        const string OrchestratorPrefabPath = "Packages/nl.cwi.dis.vr2gather/Runtime/VRTOrchestrator/Prefabs/Tool_StandaloneOrchestratorController.prefab";

        public static string FixPilotControllerPrefabsInActiveScene(bool dryRun) => FixPilotControllerPrefabs(SceneManager.GetActiveScene(), dryRun);

        /// <summary>
        /// Fill empty configurationPrefab/orchestratorPrefab on PilotControllers with the package
        /// defaults (the same ones Tool_scenesetup uses), so the scene is playable in the editor.
        /// </summary>
        public static string FixPilotControllerPrefabs(Scene scene, bool dryRun)
        {
            var report = new Report("PilotController prefabs", scene, dryRun);
            var configurationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConfigurationPrefabPath);
            var orchestratorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OrchestratorPrefabPath);
            if (configurationPrefab == null || orchestratorPrefab == null)
                return report.Skip("package default prefabs not found");

            foreach (var go in AllObjects(scene))
            {
                foreach (var comp in go.GetComponents<MonoBehaviour>())
                {
                    if (comp == null) continue;
                    var so = new SerializedObject(comp);
                    var config = so.FindProperty("configurationPrefab");
                    var orch = so.FindProperty("orchestratorPrefab");
                    if (config == null || orch == null) continue;
                    if (config.objectReferenceValue != null && orch.objectReferenceValue != null) continue;

                    var what = new List<string>();
                    if (config.objectReferenceValue == null)
                    {
                        what.Add($"configurationPrefab → {configurationPrefab.name}");
                        config.objectReferenceValue = configurationPrefab;
                    }
                    if (orch.objectReferenceValue == null)
                    {
                        what.Add($"orchestratorPrefab → {orchestratorPrefab.name}");
                        orch.objectReferenceValue = orchestratorPrefab;
                    }
                    report.Change(go, $"{comp.GetType().Name} {string.Join(", ", what)}");
                    if (!dryRun)
                        so.ApplyModifiedProperties();
                }
            }
            return report.Finish();
        }

        // ── Trigger colliders on interactables ──────────────────────────────────────

        // The VR controller casters in P_Self_Player ignore trigger colliders (QueryTriggerInteraction.Ignore),
        // while the P_Handsfree ray does hit them. An interactable whose colliders are all triggers therefore
        // works with keyboard/mouse but is invisible to the VR controllers (ray, near and poke).

        public static string FixTriggerInteractablesInActiveScene(bool dryRun) => FixTriggerInteractables(SceneManager.GetActiveScene(), dryRun);

        public static string FixTriggerInteractables(Scene scene, bool dryRun)
        {
            var report = new Report("Trigger-only interactables", scene, dryRun);
            FixTriggerInteractables(AllObjects(scene), report, dryRun);
            return report.Finish();
        }

        /// <summary>
        /// Same, for a prefab asset. Unlike the scene fixers this saves the prefab.
        /// </summary>
        public static string FixTriggerInteractablesInPrefab(string prefabPath, bool dryRun)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var report = new Report("Trigger-only interactables", prefabPath, dryRun);
            try
            {
                FixTriggerInteractables(root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject), report, dryRun);
                if (!dryRun && report.Changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool saved);
                    if (!saved)
                        report.SaveFailed(prefabPath, "see console");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return report.Finish();
        }

        static void FixTriggerInteractables(IEnumerable<GameObject> objects, Report report, bool dryRun)
        {
            var interactableType = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable, Unity.XR.Interaction.Toolkit");
            var teleportType = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.BaseTeleportationInteractable, Unity.XR.Interaction.Toolkit");
            if (interactableType == null || teleportType == null)
            {
                report.Skip("XRI not installed");
                return;
            }

            foreach (var go in objects)
            {
                foreach (var comp in go.GetComponents(interactableType))
                {
                    var colliders = InteractableColliders(comp);
                    if (colliders.Count == 0 || colliders.Any(c => !c.isTrigger))
                        continue;

                    var names = string.Join(", ", colliders.Select(c => $"{HierarchyPath(c.gameObject)} ({c.GetType().Name})"));
                    if (teleportType.IsInstanceOfType(comp))
                    {
                        report.Unknown(go, $"teleportation area with only trigger colliders: {names}");
                        continue;
                    }
                    var body = colliders.Select(c => c.attachedRigidbody).FirstOrDefault(b => b != null && !b.isKinematic);
                    if (body != null)
                    {
                        report.Unknown(go, $"{comp.GetType().Name}: non-kinematic Rigidbody on {body.name}, making {names} solid would change physics");
                        continue;
                    }
                    var listener = colliders.SelectMany(c => c.GetComponents<MonoBehaviour>()).FirstOrDefault(HasTriggerCallback);
                    if (listener != null)
                    {
                        report.Unknown(go, $"{comp.GetType().Name}: {listener.GetType().Name} on {listener.name} handles trigger events, trigger probably deliberate");
                        continue;
                    }

                    report.Change(go, $"{comp.GetType().Name}: isTrigger off on {names}");
                    if (dryRun) continue;
                    foreach (var c in colliders)
                    {
                        Undo.RecordObject(c, "Fix trigger collider on interactable");
                        c.isTrigger = false;
                    }
                }
            }
        }

        // The colliders XRI uses for an interactable: the Colliders list, or else all colliders in its children.
        static List<Collider> InteractableColliders(Component interactable)
        {
            var so = new SerializedObject(interactable);
            var list = so.FindProperty("m_Colliders");
            var result = new List<Collider>();
            if (list != null)
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue is Collider c && c != null)
                        result.Add(c);
            if (result.Count == 0)
                result.AddRange(interactable.GetComponentsInChildren<Collider>(true));
            return result;
        }

        static bool HasTriggerCallback(MonoBehaviour mb)
        {
            if (mb == null) return false;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var t = mb.GetType();
            return t.GetMethod("OnTriggerEnter", flags) != null
                || t.GetMethod("OnTriggerStay", flags) != null
                || t.GetMethod("OnTriggerExit", flags) != null;
        }

        // ── Grabbable event wiring ──────────────────────────────────────────────────

        // Old VRTGrabbableController method names, renamed long ago (d11419a2b). Unity silently skips
        // persistent calls to methods that don't exist, so grabbing works locally but isn't synchronized.
        // OnSelectEnter takes the SelectEnterEventArgs, so the call mode changes from Void to EventDefined.
        static readonly (string oldName, string newName, PersistentListenerMode mode, string eventMustContain)[] GrabbableRenames =
        {
            ("OnGrab", "OnSelectEnter", PersistentListenerMode.EventDefined, "SelectEntered"),
            ("OnRelease", "OnSelectExit", PersistentListenerMode.Void, null),
        };

        public static string FixGrabbableEventsInActiveScene(bool dryRun) => FixGrabbableEvents(SceneManager.GetActiveScene(), dryRun);

        public static string FixGrabbableEvents(Scene scene, bool dryRun)
        {
            var report = new Report("Grabbable events", scene, dryRun);
            FixGrabbableEvents(AllObjects(scene), report, dryRun);
            return report.Finish();
        }

        /// <summary>
        /// Same, for a prefab asset. Unlike the scene fixers this saves the prefab.
        /// </summary>
        public static string FixGrabbableEventsInPrefab(string prefabPath, bool dryRun)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var report = new Report("Grabbable events", prefabPath, dryRun);
            try
            {
                FixGrabbableEvents(root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject), report, dryRun);
                if (!dryRun && report.Changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool saved);
                    if (!saved)
                        report.SaveFailed(prefabPath, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root) > 0
                            || root.GetComponentsInChildren<Transform>(true).Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                            ? "prefab has missing scripts, Unity refuses to save it" : "see console");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return report.Finish();
        }

        static void FixGrabbableEvents(IEnumerable<GameObject> objects, Report report, bool dryRun)
        {
            foreach (var go in objects)
            {
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    var so = new SerializedObject(comp);
                    var it = so.GetIterator();
                    bool changed = false;
                    while (it.Next(true))
                    {
                        if (!it.propertyPath.EndsWith(".m_MethodName") || !it.propertyPath.Contains("m_PersistentCalls")) continue;
                        var rename = GrabbableRenames.FirstOrDefault(r => r.oldName == it.stringValue);
                        if (rename.oldName == null) continue;
                        var callPath = it.propertyPath.Substring(0, it.propertyPath.Length - ".m_MethodName".Length);
                        var target = so.FindProperty(callPath + ".m_Target")?.objectReferenceValue;
                        if (target == null || target.GetType().Name != "VRTGrabbableController") continue;
                        var eventName = callPath.Split('.')[0];
                        if (target.GetType().GetMethod(rename.oldName) != null) continue;
                        if (rename.eventMustContain != null && !eventName.Contains(rename.eventMustContain))
                        {
                            report.Unknown(go, $"{comp.GetType().Name}.{eventName} calls {rename.oldName}");
                            continue;
                        }
                        report.Change(go, $"{comp.GetType().Name}.{eventName}: {rename.oldName} → {rename.newName}");
                        if (dryRun) continue;
                        it.stringValue = rename.newName;
                        so.FindProperty(callPath + ".m_Mode").enumValueIndex = (int)rename.mode;
                        changed = true;
                    }
                    if (changed)
                        so.ApplyModifiedProperties();
                }
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        static IEnumerable<GameObject> AllObjects(Scene scene) =>
            scene.GetRootGameObjects()
                 .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                 .Select(t => t.gameObject);

        static string HierarchyPath(GameObject go)
        {
            var path = go.name;
            for (var t = go.transform.parent; t != null; t = t.parent)
                path = t.name + "/" + path;
            return path;
        }

        class Report
        {
            readonly StringBuilder _sb = new StringBuilder();
            readonly Scene? _scene;
            readonly bool _dryRun;
            int _changed, _unknown;
            public int Changed => _changed;

            public Report(string what, Scene scene, bool dryRun) : this(what, scene.path, dryRun)
            {
                _scene = scene;
            }

            public Report(string what, string where, bool dryRun)
            {
                _dryRun = dryRun;
                _sb.AppendLine($"{what} in {where}{(dryRun ? " (dry run)" : "")}");
            }

            public void Change(GameObject go, string what)
            {
                _changed++;
                _sb.AppendLine($"  {(_dryRun ? "would fix" : "fixed")}  {HierarchyPath(go)}: {what}");
            }

            public void Unknown(GameObject go, string what)
            {
                _unknown++;
                _sb.AppendLine($"  NOT FIXED  {HierarchyPath(go)}: {what} (unknown pattern)");
            }

            public void SaveFailed(string path, string why)
            {
                _unknown += _changed;
                _changed = 0;
                _sb.AppendLine($"  NOT SAVED  {path}: {why}; none of the changes above were kept");
            }

            public string Skip(string why)
            {
                _sb.AppendLine($"  skipped: {why}");
                return _sb.ToString();
            }

            public string Finish()
            {
                if (!_dryRun && _changed > 0 && _scene.HasValue)
                    EditorSceneManager.MarkSceneDirty(_scene.Value);
                _sb.AppendLine($"{_changed} {(_dryRun ? "to fix" : "fixed")}, {_unknown} not fixed");
                return _sb.ToString();
            }
        }
    }
}
