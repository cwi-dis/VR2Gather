using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BakeryLightmappedPrefab))]
[CanEditMultipleObjects]
public class ftLightmappedPrefabInspector : UnityEditor.Editor
{
    bool allPrefabsGood = true;
    SerializedProperty isEnabled;

    void Refresh(BakeryLightmappedPrefab selected)
    {
        allPrefabsGood = selected.IsValid() && allPrefabsGood;
    }

    void OnPrefabInstanceUpdate(GameObject go)
    {
        allPrefabsGood = true;
        foreach(BakeryLightmappedPrefab selected in targets)
        {
            //if (go != selected.gameObject) continue;
            Refresh(selected);
        }
    }

    void OnEnable()
    {
        allPrefabsGood = true;
        foreach(BakeryLightmappedPrefab selected in targets)
        {
            Refresh(selected);
        }
        PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdate;
        isEnabled = serializedObject.FindProperty("enableBaking");
    }

    void OnDisable()
    {
        PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdate;
    }

    public ftLightmapsStorage FindPrefabStorage(BakeryLightmappedPrefab pref)
    {
        var p = pref.gameObject;
        var bdataName = "BakeryPrefabLightmapData";
        var pstoreT = p.transform.Find(bdataName);
        if (pstoreT == null)
        {
            var pstoreG = new GameObject();
            pstoreG.name = bdataName;
            pstoreT = pstoreG.transform;
            pstoreT.parent = p.transform;
        }
        var pstore = pstoreT.gameObject.GetComponent<ftLightmapsStorage>();
        if (pstore == null) pstore = pstoreT.gameObject.AddComponent<ftLightmapsStorage>();
        return pstore;
    }

    public void CopySettings(ftLightmapsStorage src, ftLightmapsStorage dest)
    {
        dest.renderSettingsBounces = src.renderSettingsBounces;
        dest.renderSettingsGISamples = src.renderSettingsGISamples;
        dest.renderSettingsGIBackFaceWeight = src.renderSettingsGIBackFaceWeight;
        dest.renderSettingsTileSize = src.renderSettingsTileSize;
        dest.renderSettingsPriority = src.renderSettingsPriority;
        dest.renderSettingsTexelsPerUnit = src.renderSettingsTexelsPerUnit;
        dest.renderSettingsForceRefresh = src.renderSettingsForceRefresh;
        dest.renderSettingsForceRebuildGeometry = src.renderSettingsForceRebuildGeometry;
        dest.renderSettingsPerformRendering = src.renderSettingsPerformRendering;
        dest.renderSettingsUserRenderMode = src.renderSettingsUserRenderMode;
        dest.renderSettingsDistanceShadowmask = src.renderSettingsDistanceShadowmask;
        dest.renderSettingsSettingsMode = src.renderSettingsSettingsMode;
        dest.renderSettingsFixSeams = src.renderSettingsFixSeams;
        dest.renderSettingsDenoise = src.renderSettingsDenoise;
        dest.renderSettingsEncode = src.renderSettingsEncode;
        dest.renderSettingsEncodeMode = src.renderSettingsEncodeMode;
        dest.renderSettingsOverwriteWarning = src.renderSettingsOverwriteWarning;
        dest.renderSettingsAutoAtlas = src.renderSettingsAutoAtlas;
        dest.renderSettingsUnwrapUVs = src.renderSettingsUnwrapUVs;
        dest.renderSettingsMaxAutoResolution = src.renderSettingsMaxAutoResolution;
        dest.renderSettingsMinAutoResolution = src.renderSettingsMinAutoResolution;
        dest.renderSettingsUnloadScenes = src.renderSettingsUnloadScenes;
        dest.renderSettingsGILODMode = src.renderSettingsGILODMode;
        dest.renderSettingsGILODModeEnabled = src.renderSettingsGILODModeEnabled;
        dest.renderSettingsCheckOverlaps = src.renderSettingsCheckOverlaps;
        dest.renderSettingsSkipOutOfBoundsUVs = src.renderSettingsSkipOutOfBoundsUVs;
        dest.renderSettingsHackEmissiveBoost = src.renderSettingsHackEmissiveBoost;
        dest.renderSettingsHackIndirectBoost = src.renderSettingsHackIndirectBoost;
        dest.renderSettingsTempPath = src.renderSettingsTempPath;
        dest.renderSettingsOutPath = src.renderSettingsOutPath;
        dest.renderSettingsHackAOIntensity = src.renderSettingsHackAOIntensity;
        dest.renderSettingsHackAOSamples = src.renderSettingsHackAOSamples;
        dest.renderSettingsHackAORadius = src.renderSettingsHackAORadius;
        dest.renderSettingsShowAOSettings = src.renderSettingsShowAOSettings;
        dest.renderSettingsShowTasks = src.renderSettingsShowTasks;
        dest.renderSettingsShowTasks2 = src.renderSettingsShowTasks2;
        dest.renderSettingsShowPaths = src.renderSettingsShowPaths;
        dest.renderSettingsOcclusionProbes = src.renderSettingsOcclusionProbes;
        dest.renderSettingsTexelsPerMap = src.renderSettingsTexelsPerMap;
        dest.renderSettingsTexelsColor = src.renderSettingsTexelsColor;
        dest.renderSettingsTexelsMask = src.renderSettingsTexelsMask;
        dest.renderSettingsTexelsDir = src.renderSettingsTexelsDir;
        dest.renderSettingsShowDirWarning = src.renderSettingsShowDirWarning;
        dest.renderSettingsRenderDirMode = src.renderSettingsRenderDirMode;
        dest.renderSettingsShowCheckerSettings = src.renderSettingsShowCheckerSettings;
        dest.renderSettingsSamplesWarning = src.renderSettingsSamplesWarning;
        dest.renderSettingsPrefabWarning = src.renderSettingsPrefabWarning;
        dest.renderSettingsSplitByScene = src.renderSettingsSplitByScene;
        dest.renderSettingsUVPaddingMax = src.renderSettingsUVPaddingMax;
        dest.renderSettingsBeepOnFinish = src.renderSettingsBeepOnFinish;
    }

    public override void OnInspectorGUI() {

        serializedObject.Update();
        var prev = isEnabled.boolValue;
        EditorGUILayout.PropertyField(isEnabled, new GUIContent("Enable baking", "Prefab contents will be patched after baking if this checkbox is on. Patched prefab will be lightmapped when instantiated in any scene."));
        serializedObject.ApplyModifiedProperties();

        if (isEnabled.boolValue != prev)
        {
            allPrefabsGood = true;
            foreach(BakeryLightmappedPrefab selected in targets)
            {
                selected.enableBaking = isEnabled.boolValue;
                Refresh(selected);
            }
        }

        if (allPrefabsGood)
        {
            EditorGUILayout.LabelField("Prefab connection: OK");
        }
        else
        {
            foreach(BakeryLightmappedPrefab selected in targets)
            {
                if (selected.errorMessage.Length > 0) EditorGUILayout.LabelField("Error: " + selected.errorMessage);
            }
        }

        if (GUILayout.Button("Load render settings from prefab"))
        {
            if (EditorUtility.DisplayDialog("Bakery", "Change current render settings to prefab?", "OK", "Cancel"))
            {
                var storage = ftRenderLightmap.FindRenderSettingsStorage();
                foreach(BakeryLightmappedPrefab pref in targets)
                {
                    var prefabStorage = FindPrefabStorage(pref);
                    CopySettings(prefabStorage, storage);
                }
                var instance = (ftRenderLightmap)EditorWindow.GetWindow(typeof(ftRenderLightmap));
                if (instance != null) instance.LoadRenderSettings();
            }
        }

        if (GUILayout.Button("Save current render settings to prefab"))
        {
            if (EditorUtility.DisplayDialog("Bakery", "Save current render settings to prefab?", "OK", "Cancel"))
            {
                var storage = ftRenderLightmap.FindRenderSettingsStorage();
                foreach(BakeryLightmappedPrefab pref in targets)
                {
                    var prefabStorage = FindPrefabStorage(pref);
                    CopySettings(storage, prefabStorage);
                }
            }
        }
    }
}

