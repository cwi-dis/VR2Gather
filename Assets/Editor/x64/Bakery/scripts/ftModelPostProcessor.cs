using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class ftModelPostProcessorInternal : AssetPostprocessor
{
    public virtual void UnwrapXatlas(Mesh m, UnwrapParam param)
    {
    }
}

public partial class ftModelPostProcessor : ftModelPostProcessorInternal
{
    static ftGlobalStorage storage;
    UnwrapParam uparams;
    const int res = 1024;
    static Material mat;
    public static RenderTexture rt;
    public static Texture2D tex;

    static Dictionary<string, bool> assetHasPaddingAdjustment = new Dictionary<string, bool>();

#if UNITY_2017_1_OR_NEWER
    bool deserializedSuccess = false;
    ftGlobalStorage.AdjustedMesh deserialized;
#endif

    public static double GetTime()
    {
        return (System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond) / 1000.0;
    }

    public static void Init()
    {
        storage = ftLightmaps.GetGlobalStorage();

        //ftLightmaps.AddTag("BakeryProcessed");
    }

    void OnPreprocessModel()
    {
        assetHasPaddingAdjustment[assetPath] = false;

        if (storage == null) return;
        if (storage.modifiedAssetPathList.IndexOf(assetPath) < 0) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        assetHasPaddingAdjustment[assetPath] = importer.generateSecondaryUV;
        importer.generateSecondaryUV = false; // disable built-in unwrapping for models with padding adjustment
    }

    void OnPostprocessModel(GameObject g)
    {
        Init();

        ModelImporter importer = (ModelImporter)assetImporter;
        if (importer.generateSecondaryUV || assetHasPaddingAdjustment[assetPath])
        {
            if (!importer.generateSecondaryUV)
            {
                importer.generateSecondaryUV = true; // set "generate lightmap UVs" checkbox back
                EditorUtility.SetDirty(importer);
            }

            // Auto UVs: Adjust UV padding per mesh
            //if (!storage.modifiedAssetPathList.Contains(assetPath) && g.tag == "BakeryProcessed") return;
            //if (ftLightmaps.IsModelProcessed(assetPath)) return;

            //g.tag = "BakeryProcessed";
            Debug.Log("Bakery: processing auto-unwrapped asset " + assetPath);
            if (storage != null) ftLightmaps.MarkModelProcessed(assetPath, true);

            uparams = new UnwrapParam();
            UnwrapParam.SetDefaults(out uparams);
            uparams.angleError = importer.secondaryUVAngleDistortion * 0.01f;
            uparams.areaError = importer.secondaryUVAreaDistortion * 0.01f;
            uparams.hardAngle = importer.secondaryUVHardAngle;

#if UNITY_2017_1_OR_NEWER
            deserializedSuccess = false;
            var props = importer.extraUserProperties;
            for(int p=0; p<props.Length; p++)
            {
                if (props[p].Substring(0,7) == "#BAKERY")
                {
                    var json = props[p].Substring(7);
                    deserialized = JsonUtility.FromJson<ftGlobalStorage.AdjustedMesh>(json);
                    deserializedSuccess = true;
                    break;
                }
            }
#endif
            if (storage != null) storage.InitModifiedMeshMap(assetPath);

            var tt = GetTime();
            AdjustUV(g.transform);
            Debug.Log("UV adjustment time: " + (GetTime() - tt));
        }
        else
        {
            if (storage == null) return;

            Debug.Log("Bakery: checking for UV overlaps in " + assetPath);

            //if (g.tag == "BakeryProcessed") g.tag = "";
            ftLightmaps.MarkModelProcessed(assetPath, true);//false);

            // Manual UVs: check if overlapping
            CheckUVOverlap(g, assetPath);
        }

        if (g.tag == "BakeryProcessed") g.tag = ""; // remove legacy mark
    }

    public static bool InitOverlapCheck()
    {
        rt = new RenderTexture(res, res, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        tex = new Texture2D(res, res, TextureFormat.ARGB32, false, true);
        var shdr = Shader.Find("Hidden/ftOverlapTest");
        if (shdr == null)
        {
            var bakeryRuntimePath = ftLightmaps.GetRuntimePath();
            shdr = AssetDatabase.LoadAssetAtPath(bakeryRuntimePath + "ftOverlapTest.shader", typeof(Shader)) as Shader;
            if (shdr == null)
            {
                Debug.Log("No overlap testing shader present");
                return false;
            }
        }
        mat = new Material(shdr);
        return true;
    }

    // -1 = No UVs
    // 0 = no overlaps
    // > 0 = overlapping pixels count
    public static int DoOverlapCheck(GameObject g, bool deep)
    {
        int overlap = -1;
        int overlapCounter = 0;

        Graphics.SetRenderTarget(rt);
        GL.Clear(false, true, new Color(0,0,0,0));
        mat.SetPass(0);

        bool hasUV1 = RenderMeshes(g.transform, deep);
        if (hasUV1)
        {
            tex.ReadPixels(new Rect(0,0,res,res), 0, 0, false);
            tex.Apply();

            var bytes = tex.GetRawTextureData();
            overlap = 0;
            for(int i=0; i<bytes.Length; i++)
            {
                if (bytes[i] > 1)
                {
                    overlapCounter++;
                    if (overlapCounter > 256) // TODO: better check
                    {
                        overlap = 1;
                        break;
                    }
                }
            }
        }

        Graphics.SetRenderTarget(null);

        return overlap == 1 ? overlapCounter : overlap;
    }

    public static void EndOverlapCheck()
    {
        if (rt != null) rt.Release();
        if (tex != null) Object.DestroyImmediate(tex);
    }

    public static void CheckUVOverlap(GameObject g, string assetPath)
    {
        bool canCheck = InitOverlapCheck();
        if (!canCheck) return;

        int overlap = DoOverlapCheck(g, true);
        EndOverlapCheck();

        if (overlap != 1 && overlap > 0)
        {
            Debug.LogWarning("[Bakery warning] " + overlap + " pixels overlap: " + assetPath);
        }

        //var index = storage.assetList.IndexOf(assetPath);
        var index = storage.assetList.IndexOf(assetPath);
        var prevOverlap = -100;
        if (index < 0)
        {
            //index = storage.assetList.Count;
            //storage.assetList.Add(assetPath);
            index = storage.assetList.Count;
            storage.assetList.Add(assetPath);
            storage.uvOverlapAssetList.Add(overlap);
        }
        else
        {
            prevOverlap = storage.uvOverlapAssetList[index];
            storage.assetList[index] = assetPath;
            storage.uvOverlapAssetList[index] = overlap;
        }

        if (prevOverlap != overlap)
        {
            EditorUtility.SetDirty(storage);
            EditorSceneManager.MarkAllScenesDirty();
        }
    }

    bool ValidateMesh(Mesh m, ftGlobalStorage.Unwrapper unwrapper)
    {
#if UNITY_2017_3_OR_NEWER
        if (m.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32 && unwrapper == ftGlobalStorage.Unwrapper.Default)
        {
            Debug.LogError("Can't adjust UV padding for " + m.name + " due to Unity bug. Please set Index Format to 16-bit on the asset or use xatlas.");
            return false;
        }
#endif
        return true;
    }

    void AdjustUV(Transform t)
    {
        var mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var m = mf.sharedMesh;
            var nm = m.name;

#if UNITY_2017_1_OR_NEWER
            if (deserializedSuccess && deserialized.meshName != null && deserialized.padding != null)
            {
                // Get padding from extraUserProperties (new)
                int mindex = deserialized.meshName.IndexOf(nm);
                if (mindex < 0)
                {
                    //Debug.LogError("Unable to find padding value for mesh " + nm);
                    // This is fine. Apparently caused by parts of models being lightmapped,
                    // while other parts are not baked, yet still a part of the model.
                }
                else
                {
                    var padding = deserialized.padding[mindex];

                    ftGlobalStorage.Unwrapper unwrapper = ftGlobalStorage.Unwrapper.Default;
                    if (deserialized.unwrapper != null && deserialized.unwrapper.Count > mindex)
                        unwrapper = (ftGlobalStorage.Unwrapper)deserialized.unwrapper[mindex];

                    if (!ValidateMesh(m, unwrapper)) return;

                    uparams.packMargin = padding/1024.0f;
                    Unwrap(m, uparams, unwrapper);
                }
            }
            else
            {
                // Get padding from GlobalStorage (old)
                int modifiedMeshID;
                if (storage != null && storage.modifiedMeshMap.TryGetValue(nm, out modifiedMeshID))
                {
                    var padding = storage.modifiedMeshPaddingArray[modifiedMeshID];

                    ftGlobalStorage.Unwrapper unwrapper = ftGlobalStorage.Unwrapper.Default;
                    if (storage.modifiedMeshUnwrapperArray != null && storage.modifiedMeshUnwrapperArray.Count > modifiedMeshID)
                        unwrapper = (ftGlobalStorage.Unwrapper)storage.modifiedMeshUnwrapperArray[modifiedMeshID];

                    if (!ValidateMesh(m, unwrapper)) return;

                    uparams.packMargin = padding/1024.0f;
                    Unwrap(m, uparams, unwrapper);
                }
            }
#else
            int modifiedMeshID;
            if (storage != null && storage.modifiedMeshMap.TryGetValue(nm, out modifiedMeshID))
            {
                var padding = storage.modifiedMeshPaddingArray[modifiedMeshID];

                ftGlobalStorage.Unwrapper unwrapper = ftGlobalStorage.Unwrapper.Default;
                if (storage.modifiedMeshUnwrapperArray != null && storage.modifiedMeshUnwrapperArray.Count > modifiedMeshID)
                    unwrapper = (ftGlobalStorage.Unwrapper)storage.modifiedMeshUnwrapperArray[modifiedMeshID];

                if (!ValidateMesh(m, unwrapper)) return;

                uparams.packMargin = padding/1024.0f;
                Unwrap(m, uparams, unwrapper);
            }
#endif
        }

        // Recurse
        foreach (Transform child in t)
            AdjustUV(child);
    }

    static bool RenderMeshes(Transform t, bool deep)
    {
        var mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var m = mf.sharedMesh;
            //var nm = m.name;

            bool noUV2 = (m.uv2 == null || (m.uv2.Length == 0 && m.vertexCount != 0));
            bool noUV1 = (m.uv == null || (m.uv.Length == 0 && m.vertexCount != 0));

            if (noUV1 && noUV2) return false;

            mat.SetFloat("uvSet", noUV2 ? 0.0f : 1.0f);
            mat.SetPass(0);

            Graphics.DrawMeshNow(m, Vector3.zero, Quaternion.identity);
        }

        if (!deep) return true;

        // Recurse
        foreach (Transform child in t)
            RenderMeshes(child, deep);

        return true;
    }

    void Unwrap(Mesh m, UnwrapParam uparams, ftGlobalStorage.Unwrapper unwrapper)
    {
        if (unwrapper == ftGlobalStorage.Unwrapper.xatlas)
        {
            UnwrapXatlas(m, uparams);
        }
        else
        {
            var tt = GetTime();
            Unwrapping.GenerateSecondaryUVSet(m, uparams);
            Debug.Log("Unity unwrap time: " + (GetTime() - tt));
        }
    }
}

