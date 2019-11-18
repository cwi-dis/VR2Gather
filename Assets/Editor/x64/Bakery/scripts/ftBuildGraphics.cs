#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Reflection;

public class ftBuildGraphics : ScriptableWizard
{
    const int UVGBFLAG_NORMAL = 1;
    const int UVGBFLAG_FACENORMAL = 2;
    const int UVGBFLAG_ALBEDO = 4;
    const int UVGBFLAG_EMISSIVE = 8;
    const int UVGBFLAG_POS = 16;
    const int UVGBFLAG_SMOOTHPOS = 32;
    const int UVGBFLAG_TANGENT = 64;
    const int UVGBFLAG_TERRAIN = 128;
    const int UVGBFLAG_RESERVED = 256;

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern void InitShaders();

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern void LoadScene(string path);

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    private static extern void SetAlbedos(int count, IntPtr[] tex);

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    private static extern int CopyAlbedos();

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern void FreeAlbedoCopies();

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    private static extern void SetAlphas(int count, IntPtr[] tex, float[] alphaRefs, int numLODs);

    [DllImport ("unityLib11")]
    private static extern void SaveTexture(IntPtr tex, string path);

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern void SaveSky(IntPtr tex, float rx, float ry, float rz, float ux, float uy, float uz, float fx, float fy, float fz, string path, bool isLinear, bool doubleLDR);

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern void SaveCookie(IntPtr tex, string path);

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern int ftRenderUVGBuffer();

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern void SetUVGBFlags(int flags);

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern void SetFixPos(bool enabled);

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern void SetCompression(bool enabled);

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern int ftGenerateAlphaBuffer();

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern int SaveGBufferMap(IntPtr tex, string path, bool compressed);

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern int SaveGBufferMapFromRAM(byte[] tex, int size, string path, bool compressed);

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern int GetABGErrorCode();

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern int GetUVGBErrorCode();

    [DllImport ("frender", CallingConvention=CallingConvention.Cdecl)]
    public static extern void ResetBlendState();

    [DllImport ("uvrepack", CallingConvention=CallingConvention.Cdecl)]
    public static extern int uvrLoad(float[] inputUVs, int numVerts, int[] inputIndices, int numIndices);

    [DllImport ("uvrepack", CallingConvention=CallingConvention.Cdecl)]
    public static extern int uvrRepack(float padding, int resolution);

    [DllImport ("uvrepack", CallingConvention=CallingConvention.Cdecl)]
    public static extern int uvrUnload();

    static int voffset, soffset, ioffset;
    static List<BakeryLightmapGroup> groupList;

    static public string scenePath = "";

    static BinaryWriter fscene, fmesh, flmid, fseamfix, fsurf, fmatid, fmatide, fmatidh, fmatideb, falphaid, fvbfull, fvbtrace, fvbtraceTex, fvbtraceUV0, fib, fib32, fhmaps;
    static BinaryWriter[] fib32lod;
    static BinaryWriter[] falphaidlod;

    public static ftLightmapsStorage storage;
    public static ftLightmapsStorage.ImplicitLightmapData tempStorage = new ftLightmapsStorage.ImplicitLightmapData();

    public static float texelsPerUnit = 20;
    public static int minAutoResolution = 16;
    public static int maxAutoResolution = 4096;
    public static bool mustBePOT = true;
    public static bool autoAtlas = true;
    public static bool unwrapUVs = true;
    public static bool exportShaderColors = true;
    public static int atlasPaddingPixels = 3;
    public static bool atlasCountPriority = false;
    public static bool splitByScene = false;
    public static bool uvPaddingMax = false;
    public static bool exportTerrainAsHeightmap = true;
    public static bool exportTerrainTrees = false;
    public static bool uvgbHeightmap = true;

    public static bool texelsPerUnitPerMap = false;
    public static float mainLightmapScale = 1;
    public static float maskLightmapScale = 1;
    public static float dirLightmapScale = 1;

    const float atlasScaleUpValue = 1.01f;
    const int atlasMaxTries = 100;
    const float alphaInstanceThreshold = 5.0f / 255.0f;

    public static string overwriteExtensionCheck = ".hdr";
    public static bool overwriteWarning = false;
    public static bool overwriteWarningSelectedOnly = false;
    public static bool memoryWarning = false;
    public static bool modifyLightmapStorage = true;
    //public static int unityVersionMajor = 0;
    //public static int unityVersionMinor = 0;

    static int areaLightCounter = -2;
    public static int sceneLodsUsed = 0;

    static GameObject lmgroupHolder;
    static List<BakeryLightmapGroup> autoAtlasGroups = null;
    static List<AtlasNode> autoAtlasGroupRootNodes = null;
    static BakeryLightmapGroup autoVertexGroup = null;
    static BakeryLightmapGroup lightProbeLMGroup = null;

    static List<GameObject> terrainObjectList;
    static List<Terrain> terrainObjectToActual;
    static List<Texture> terrainObjectToHeightMap;
    static IntPtr[] terrainObjectToHeightMapPtr;
    static List<float> terrainObjectToBounds;
    static List<int> terrainObjectToLMID;
    static List<float> terrainObjectToBoundsUV;
    static List<List<float[]>> terrainObjectToHeightMips;
    //static List<List<Vector3[]>> terrainObjectToNormalMips;
    //static List<Vector3[]> terrainObjectToNormalMip0;
    static List<GameObject> temporaryAreaLightMeshList;
    static List<BakeryLightMesh> temporaryAreaLightMeshList2;
    static List<GameObject> treeObjectList;

    public static List<float> vbtraceTexPosNormalArray; // global vbTraceTex.bin positions/normals
    public static List<float> vbtraceTexUVArray; // global vbTraceTex.bin UVs
    public static float[] vbtraceTexUVArrayLOD; // global vbTraceTex.bin LOD UVs

    public static List<MeshRenderer> atlasOnlyObj;
    public static List<Vector4> atlasOnlyScaleOffset;
    public static List<int> atlasOnlySize;
    public static List<int> atlasOnlyID;

    static ftGlobalStorage gstorage;

    static public void DebugLogError(string text)
    {
        ftRenderLightmap.DebugLogError(text);
    }

    class AtlasNode
    {
        public AtlasNode child0, child1;
        public Rect rc;
        public GameObject obj;
        bool leaf = true;

        public AtlasNode Insert(GameObject o, Rect rect)
        {
            if (!leaf)
            {
                var newNode = child0.Insert(o, rect);
                if (newNode != null) return newNode;

                return child1.Insert(o, rect);
            }
            else
            {
                if (obj != null) return null;

                var fits = (rect.width <= rc.width && rect.height <= rc.height);
                if (!fits) return null;

                var fitsExactly = (rect.width == rc.width && rect.height == rc.height);
                if (fitsExactly)
                {
                    obj = o;
                    return this;
                }

                child0 = new AtlasNode();
                child1 = new AtlasNode();

                var dw = rc.width - rect.width;
                var dh = rc.height - rect.height;

                if (dw > dh)
                {
                    child0.rc = new Rect(rc.x, rc.y, rect.width, rc.height);
                    child1.rc = new Rect(rc.x + rect.width, rc.y, rc.width - rect.width, rc.height);
                }
                else
                {
                    child0.rc = new Rect(rc.x, rc.y, rc.width, rect.height);
                    child1.rc = new Rect(rc.x, rc.y + rect.height, rc.width, rc.height - rect.height);
                }
                leaf = false;

                return child0.Insert(o, rect);
            }
        }

        public void GetMax(ref float maxx, ref float maxy)
        {
            if (obj != null)
            {
                if ((rc.x + rc.width) > maxx) maxx = rc.x + rc.width;
                if ((rc.y + rc.height) > maxy) maxy = rc.y + rc.height;
            }
            if (child0 != null) child0.GetMax(ref maxx, ref maxy);
            if (child1 != null) child1.GetMax(ref maxx, ref maxy);
        }

        public void Transform(float offsetx, float offsety, float scalex, float scaley)
        {
            rc.x *= scalex;
            rc.y *= scaley;
            rc.x += offsetx;
            rc.y += offsety;
            rc.width *= scalex;
            rc.height *= scaley;
            if (child0 != null) child0.Transform(offsetx, offsety, scalex, scaley);
            if (child1 != null) child1.Transform(offsetx, offsety, scalex, scaley);
        }
    };

    static ftBuildGraphics()
    {
        //ftRenderLightmap.PatchPath();
        //var unityVer = Application.unityVersion.Split('.');
        //unityVersionMajor = Int32.Parse(unityVer[0]);
        //unityVersionMinor = Int32.Parse(unityVer[1]);
    }

    static void exportVBPos(BinaryWriter f, Transform t, Mesh m, Vector3[] vertices)
    {
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 pos = vertices[i];//t.TransformPoint(vertices[i]);
            f.Write(pos.x);
            f.Write(pos.y);
            f.Write(pos.z);
        }
    }

    static void exportVBTrace(BinaryWriter f, Mesh m, Vector3[] vertices, Vector3[] normals)
    {
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 pos = vertices[i];//t.TransformPoint(vertices[i]);
            f.Write(pos.x);
            f.Write(pos.y);
            f.Write(pos.z);

            Vector3 normal = normals[i];//t.TransformDirection(normals[i]);
            f.Write(normal.x);
            f.Write(normal.y);
            f.Write(normal.z);
        }
    }

    public static BakeryLightmapGroup GetLMGroupFromObjectExplicit(GameObject obj)
    {
        lmgroupHolder = null;
        var lmgroupSelector = obj.GetComponent<BakeryLightmapGroupSelector>(); // if object has explicit lmgroup
        if (lmgroupSelector == null)
        {
            // if parents have explicit lmgroup
            var t2 = obj.transform.parent;
            while(lmgroupSelector == null && t2 != null)
            {
                lmgroupSelector = t2.GetComponent<BakeryLightmapGroupSelector>();
                t2 = t2.parent;
            }
        }
        BakeryLightmapGroup lmgroup = null;
        if (lmgroupSelector != null)
        {
            lmgroup = lmgroupSelector.lmgroupAsset as BakeryLightmapGroup;
            lmgroupHolder = lmgroupSelector.gameObject;

            var so = new SerializedObject(obj.GetComponent<Renderer>());
            var scaleInLm = so.FindProperty("m_ScaleInLightmap").floatValue;
            if (scaleInLm == 0.0f) lmgroup = autoVertexGroup;
                //null; // ignore lightmaps when scaleInLightmap == 0
        }
        return lmgroup;
    }

    public static BakeryLightmapGroup GetLMGroupFromObject(GameObject obj)
    {
        UnityEngine.Object lmgroupObj = null;
        BakeryLightmapGroup lmgroup = null;
        lmgroupHolder = null;

        var lmgroupSelector = obj.GetComponent<BakeryLightmapGroupSelector>(); // if object has explicit lmgroup
        tempStorage.implicitGroupMap.TryGetValue(obj, out lmgroupObj); // or implicit
        lmgroup = (BakeryLightmapGroup)lmgroupObj;
        lmgroupHolder = obj;
        if (lmgroupSelector == null && lmgroup == null)
        {
            // if parents have explicit/implicit lmgroup
            var t2 = obj.transform.parent;
            while(lmgroupSelector == null && lmgroup == null && t2 != null)
            {
                lmgroupSelector = t2.GetComponent<BakeryLightmapGroupSelector>();
                tempStorage.implicitGroupMap.TryGetValue(t2.gameObject, out lmgroupObj);
                lmgroup = (BakeryLightmapGroup)lmgroupObj;
                lmgroupHolder = t2.gameObject;
                t2 = t2.parent;
            }
        }
        if (lmgroupSelector != null)
        {
            lmgroup = lmgroupSelector.lmgroupAsset as BakeryLightmapGroup;
        }

        if (lmgroup != null)
        {
            var r = obj.GetComponent<Renderer>();
            if (r)
            {
                var so = new SerializedObject(r);
                var scaleInLm = so.FindProperty("m_ScaleInLightmap").floatValue;
                if (scaleInLm == 0.0f) lmgroup = autoVertexGroup;
                // null; // ignore lightmaps when scaleInLightmap == 0
            }
        }
        else
        {
            lmgroupHolder = null;
        }

        return lmgroup;
    }

    public static void exportVBTraceTex(BinaryWriter f, Vector3[] vertices, Vector3[] normals, Vector2[] uv2, int lmid)
    {
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 pos = vertices[i];//t.TransformPoint(vertices[i]);
            f.Write(pos.x);
            f.Write(pos.y);
            f.Write(pos.z);

            Vector3 normal = normals[i];//t.TransformDirection(normals[i]);
            f.Write(normal.x);
            f.Write(normal.y);
            f.Write(normal.z);

            float u = 0;
            float v = 0;

            if (lmid < 0)
            {
                if (uv2.Length>0)
                {
                    u = Mathf.Clamp(uv2[i].x, 0, 0.99999f);
                    v = Mathf.Clamp(1.0f - uv2[i].y, 0, 0.99999f);
                }
            }
            else
            {
                if (uv2.Length>0)
                {
                    u = Mathf.Clamp(uv2[i].x, 0, 0.99999f);
                    v = Mathf.Clamp(uv2[i].y, 0, 0.99999f);
                }
            }

            u += lmid * 10;
            if (lmid >= 0 && (int)u > lmid*10)
            {
                Debug.LogError("Float overflow");
            }

            f.Write(u);
            f.Write(v);
        }
    }

    public static void exportVBTraceTexAttribs(List<float> arrPosNormal, List<float> arrUV,
     Vector3[] vertices, Vector3[] normals, Vector2[] uv2, int lmid, bool vertexBake)
    {
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 pos = vertices[i];//t.TransformPoint(vertices[i]);
            arrPosNormal.Add(pos.x);
            arrPosNormal.Add(pos.y);
            arrPosNormal.Add(pos.z);

            Vector3 normal = normals[i];//t.TransformDirection(normals[i]);
            arrPosNormal.Add(normal.x);
            arrPosNormal.Add(normal.y);
            arrPosNormal.Add(normal.z);

            float u = 0;
            float v = 0;

            if (lmid < 0)
            {
                //u = lmid * 10;
                if (uv2.Length>0)
                {
                    u = Mathf.Clamp(uv2[i].x, 0, 0.99999f);
                    v = Mathf.Clamp(1.0f - uv2[i].y, 0, 0.99999f);
                }
            }
            else
            {
                if (uv2.Length>0 && !vertexBake)
                {
                    u = Mathf.Clamp(uv2[i].x, 0, 0.99999f);
                    v = Mathf.Clamp(uv2[i].y, 0, 0.99999f);
                }
                else if (vertexBake)
                {
                    u = uv2[i].x;
                    v = uv2[i].y - 1.0f;
                }
            }

            u += lmid * 10;
            if (lmid >= 0 && (int)u > lmid*10)
            {
                Debug.LogError("Float overflow");
            }

            arrUV.Add(u);
            arrUV.Add(v);
        }
    }

    static void exportVBTraceUV0(BinaryWriter f, Vector2[] uvs, int vertCount)
    {
        if (uvs.Length == 0)
        {
            for(int i=0;i<vertCount;i++)
            {
                f.Write(0.0f);
                f.Write(0.0f);
            }
        }
        else
        {
            for(int i=0;i<vertCount;i++)
            {
                f.Write(uvs[i].x);
                f.Write(100000.0f - uvs[i].y);
            }
        }
    }

    static void exportVBBasic(BinaryWriter f, Transform t, Mesh m, Vector3[] vertices, Vector3[] normals, Vector2[] uv2)
    {
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 pos = vertices[i];//t.TransformPoint(vertices[i]);
            f.Write(pos.x);
            f.Write(pos.y);
            f.Write(pos.z);

            Vector3 normal = normals[i];//t.TransformDirection(normals[i]);
            f.Write(normal.x);
            f.Write(normal.y);
            f.Write(normal.z);

            if (uv2.Length>0)
            {
                f.Write(uv2[i].x);
                f.Write(uv2[i].y);
            }
            else
            {
                f.Write(0.0f);
                f.Write(0.0f);
            }
        }
    }

    static void exportVBFull(BinaryWriter f, Vector3[] vertices, Vector3[] normals, Vector4[] tangents, Vector2[] uv, Vector2[] uv2)
    {
        bool hasUV = uv.Length > 0;
        bool hasUV2 = uv2.Length > 0;

        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 pos = vertices[i];//t.TransformPoint(vertices[i]);
            f.Write(pos.x);
            f.Write(pos.y);
            f.Write(pos.z);

            Vector3 normal = normals[i];//t.TransformDirection(normals[i]);
            f.Write(normal.x);
            f.Write(normal.y);
            f.Write(normal.z);

            if (tangents == null)
            {
                f.Write(0.0f);
                f.Write(0.0f);
                f.Write(0.0f);
                f.Write(0.0f);
            }
            else
            {
                Vector4 tangent = tangents[i];
                f.Write(tangent.x);
                f.Write(tangent.y);
                f.Write(tangent.z);
                f.Write(tangent.w);
            }

            if (hasUV)
            {
                f.Write(uv[i].x);
                f.Write(uv[i].y);
            }
            else
            {
                f.Write(0.0f);
                f.Write(0.0f);
            }

            if (hasUV2)
            {
                f.Write(uv2[i].x);
                f.Write(uv2[i].y);
            }
            else
            {
                f.Write(0.0f);
                f.Write(0.0f);
            }
        }
    }

    static int exportIB(BinaryWriter f, int[] indices, bool isFlipped, bool is32Bit, int offset, BinaryWriter falphaid, ushort alphaID)
    {
        //var indices = m.GetTriangles(i);
        for(int j=0;j<indices.Length;j+=3)
        {
            if (!isFlipped)
            {
                if (is32Bit)
                {
                    f.Write(indices[j] + offset);
                    f.Write(indices[j+1] + offset);
                    f.Write(indices[j+2] + offset);
                }
                else
                {
                    f.Write(indices[j]);
                    f.Write(indices[j+1]);
                    f.Write(indices[j+2]);
                }
            }
            else
            {
                if (is32Bit)
                {
                    f.Write(indices[j+2] + offset);
                    f.Write(indices[j+1] + offset);
                    f.Write(indices[j] + offset);
                }
                else
                {
                    f.Write(indices[j+2]);
                    f.Write(indices[j+1]);
                    f.Write(indices[j]);
                }
            }

            if (falphaid!=null) falphaid.Write(alphaID);
        }
        return indices.Length;
    }

    // returns mesh area if requested
    static void exportIB32(List<int> indicesOpaque, List<int> indicesTransparent, List<int> indicesLMID,
        int[] indices, bool isFlipped, int offset, int indexOffsetLMID, BinaryWriter falphaid,
        ushort alphaID)
    {
        //var indices = m.GetTriangles(i);
        var indicesOut = alphaID == 0xFFFF ? indicesOpaque : indicesTransparent;

        int indexA, indexB, indexC;

        for(int j=0;j<indices.Length;j+=3)
        {
            if (!isFlipped)
            {
                indexA = indices[j];
                indexB = indices[j + 1];
                indexC = indices[j + 2];
            }
            else
            {
                indexA = indices[j + 2];
                indexB = indices[j + 1];
                indexC = indices[j];
            }

            indicesOut.Add(indexA + offset);
            indicesOut.Add(indexB + offset);
            indicesOut.Add(indexC + offset);

            if (indicesLMID != null)
            {
                indicesLMID.Add(indexA + indexOffsetLMID);
                indicesLMID.Add(indexB + indexOffsetLMID);
                indicesLMID.Add(indexC + indexOffsetLMID);
            }

            if (alphaID != 0xFFFF) falphaid.Write(alphaID);
        }
    }


    static void exportSurfs(BinaryWriter f, int[][] indices, int subMeshCount)// Mesh m)
    {
        int offset = ioffset;
        for(int i=0;i<subMeshCount;i++) {
            int size = indices[i].Length;//m.GetTriangles(i).Length;
            f.Write(offset);
            f.Write(size);
            offset += size;// * 2;
        }
        soffset += subMeshCount;
    }

    static void exportMesh(BinaryWriter f, Mesh m)
    {
        f.Write(voffset);
        f.Write(soffset);
        f.Write((ushort)m.subMeshCount);
        f.Write((ushort)0);
    }

    static int exportLMID(BinaryWriter f, GameObject obj, BakeryLightmapGroup lmgroup)
    {
        if (lmgroup!=null)
        {
            f.Write(lmgroup.id);
            return lmgroup.id;
        }
        else
        {
            var areaLight =  obj.GetComponent<BakeryLightMesh>();
            if (areaLight == null)
            {
                int index = temporaryAreaLightMeshList.IndexOf(obj);
                if (index >= 0)
                {
                    areaLight = temporaryAreaLightMeshList2[index];
                }
            }
            if (areaLight != null)
            {
                f.Write(areaLightCounter);
                areaLight.lmid = areaLightCounter;
                areaLightCounter--;
                return areaLightCounter;
            }
            else
            {
                f.Write(0xFFFFFFFF);
                return -1;
            }
        }
    }

    static Vector2[] GenerateVertexBakeUVs(int voffset, int vlength, int totalVertexCount)
    {
        int atlasTexSize = (int)Mathf.Ceil(Mathf.Sqrt((float)totalVertexCount));
        atlasTexSize = (int)Mathf.Ceil(atlasTexSize / (float)ftRenderLightmap.tileSize) * ftRenderLightmap.tileSize;
        var uvs = new Vector2[vlength];
        float mul = 1.0f / atlasTexSize;
        float add = mul * 0.5f;
        for(int i=0; i<vlength; i++)
        {
            int x = (i + voffset) % atlasTexSize;
            int y = (i + voffset) / atlasTexSize;
            uvs[i] = new Vector2(x * mul + add, y * mul + add);// - 1.0f);
        }
        return uvs;
    }

    static bool ModelUVsOverlap(ModelImporter importer, ftGlobalStorage store)
    {
        if (importer.generateSecondaryUV) return true;

        var path = importer.assetPath;
        /*for(int i=0; i<storages.Length; i++)
        {
            var store = storages[i];
            if (store == null) continue;
            var index = store.assetList.IndexOf(path);
            if (index < 0) continue;

            if (store.uvOverlapAssetList[index] == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }*/
        var index = store.assetList.IndexOf(path);
        if (index >= 0)
        {
            if (store.uvOverlapAssetList[index] == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        var newAsset = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
        ftModelPostProcessor.CheckUVOverlap(newAsset, path);

        /*for(int i=0; i<storages.Length; i++)
        {
            var store = storages[i];
            var index = store.assetList.IndexOf(path);
            if (index < 0) continue;

            if (store.uvOverlapAssetList[index] == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }*/
        index = store.assetList.IndexOf(path);
        if (index >= 0)
        {
            if (store.uvOverlapAssetList[index] == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        return true;
    }

    static bool NeedsTangents(BakeryLightmapGroup lmgroup, bool tangentSHLights)
    {
        // RNM requires tangents
        if (ftRenderLightmap.renderDirMode == ftRenderLightmap.RenderDirMode.RNM ||
            (lmgroup!=null && lmgroup.renderDirMode == BakeryLightmapGroup.RenderDirMode.RNM)) return true;

        // SH requires tangents only if there is a SH skylight
        if (!tangentSHLights) return false;

        if (ftRenderLightmap.renderDirMode == ftRenderLightmap.RenderDirMode.SH ||
            (lmgroup!=null && lmgroup.renderDirMode == BakeryLightmapGroup.RenderDirMode.SH)) return true;

        return false;
    }

    static long GetTime()
    {
        return System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
    }

    static public string progressBarText;
    static public float progressBarPercent = 0;
    //static bool progressBarEnabled = false;
    static public bool userCanceled = false;
    //static IEnumerator progressFunc;
    static EditorWindow activeWindow;
    static void ProgressBarInit(string startText, EditorWindow window)
    {
        progressBarText = startText;
        //progressBarEnabled = true;
        ftRenderLightmap.simpleProgressBarShow("Bakery", progressBarText, progressBarPercent, 0);
    }
    static void ProgressBarShow(string text, float percent)
    {
        progressBarText = text;
        progressBarPercent = percent;
        ftRenderLightmap.simpleProgressBarShow("Bakery", progressBarText, progressBarPercent, 0);
        userCanceled = ftRenderLightmap.simpleProgressBarCancelled();
    }

    public static void FreeTemporaryAreaLightMeshes()
    {
        if (temporaryAreaLightMeshList != null)
        {
            for(int i=0; i<temporaryAreaLightMeshList.Count; i++)
            {
                if (temporaryAreaLightMeshList[i] != null)
                {
                    //var mr = temporaryAreaLightMeshList[i].GetComponent<MeshRenderer>();
                    //if (mr != null) DestroyImmediate(mr);
                    //var mf = temporaryAreaLightMeshList[i].GetComponent<MeshFilter>();
                    //if (mf != null) DestroyImmediate(mf);
                    DestroyImmediate(temporaryAreaLightMeshList[i]);
                }
            }
            temporaryAreaLightMeshList = null;
        }
    }

    public static void ProgressBarEnd(bool isError = true)
    {
        if (terrainObjectList != null)
        {
            for(int i=0; i<terrainObjectList.Count; i++)
            {
                if (terrainObjectList[i] != null) DestroyImmediate(terrainObjectList[i]);
            }
            terrainObjectList = null;
        }

        if (treeObjectList != null)
        {
            for(int i=0; i<treeObjectList.Count; i++)
            {
                if (treeObjectList[i] != null) DestroyImmediate(treeObjectList[i]);
            }
            treeObjectList = null;
        }

        if (isError)
        {
            FreeTemporaryAreaLightMeshes();
        }

        //progressBarEnabled = false;
        ftRenderLightmap.simpleProgressBarEnd();
    }
    void OnInspectorUpdate()
    {
        Repaint();
    }
    static void CloseAllFiles()
    {
        if (fscene != null) fscene.Close();
        if (fmesh != null) fmesh.Close();
        if (flmid != null) flmid.Close();
        if (fseamfix != null) fseamfix.Close();
        if (fsurf != null) fsurf.Close();
        if (fmatid != null) fmatid.Close();
        if (fmatide != null) fmatide.Close();
        if (fmatideb != null) fmatideb.Close();
        if (fmatidh != null) fmatidh.Close();
        if (falphaid != null) falphaid.Close();
        if (fvbfull != null) fvbfull.Close();
        if (fvbtrace != null) fvbtrace.Close();
        if (fvbtraceTex != null) fvbtraceTex.Close();
        if (fvbtraceUV0 != null) fvbtraceUV0.Close();
        if (fib != null) fib.Close();
        if (fib32 != null) fib32.Close();
        if (fhmaps != null) fhmaps.Close();
        if (fib32lod != null)
        {
            for(int i=0; i<fib32lod.Length; i++) fib32lod[i].Close();
        }
        if (falphaidlod != null)
        {
            for(int i=0; i<falphaidlod.Length; i++) falphaidlod[i].Close();
        }
        fscene = fmesh = flmid = fsurf = fmatid = fmatide = fmatideb = falphaid  = fvbfull  = fvbtrace = fvbtraceTex = fvbtraceUV0 = fib = fib32 = fseamfix = fmatidh = fhmaps = null;
        fib32lod = falphaidlod = null;
    }
    /*void OnGUI()
    {
        DrawWizardGUI();
        if (GUI.Button(new Rect(10, 70, 50, 30), "Build"))
        {
            progressFunc = ExportScene((ftBuildGraphics)EditorWindow.GetWindow(typeof(ftBuildGraphics)), true, false);
            EditorApplication.update += ExportSceneUpdate;
        }
        if (progressBarEnabled)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Bakery", progressBarText, progressBarPercent))
            {
                CloseAllFiles();
                progressBarEnabled = false;
                userCanceled = true;
                EditorUtility.ClearProgressBar();
            }
        }
    }*/

    static Mesh BuildAreaLightMesh(Light areaLight)
    {
        var mesh = new Mesh();

        var verts = new Vector3[4];

        verts[0] = new Vector3(-0.5f * areaLight.areaSize.x, -0.5f * areaLight.areaSize.y, 0);
        verts[1] = new Vector3(0.5f * areaLight.areaSize.x, -0.5f * areaLight.areaSize.y, 0);
        verts[2] = new Vector3(0.5f * areaLight.areaSize.x, 0.5f * areaLight.areaSize.y, 0);
        verts[3] = new Vector3(-0.5f * areaLight.areaSize.x, 0.5f * areaLight.areaSize.y, 0);

        var uvs = new Vector2[4];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(1, 1);
        uvs[3] = new Vector2(0, 1);

        var indices = new int[6];

        indices[0] = 0;
        indices[1] = 1;
        indices[2] = 2;

        indices[3] = 0;
        indices[4] = 2;
        indices[5] = 3;

        var normals = new Vector3[4];
        var n = -areaLight.transform.forward;
        for(int i=0; i<4; i++) normals[i] = n;

        mesh.vertices = verts;
        mesh.triangles = indices;
        mesh.normals = normals;
        mesh.uv = uvs;

        return mesh;
    }

    /*void ExportSceneUpdate()
    {
        if (!progressFunc.MoveNext())
        {
            EditorApplication.update -= ExportSceneUpdate;
        }
    }*/

    static public IEnumerator ExportScene(EditorWindow window, bool renderTextures = true, bool atlasOnly = false)
    {
        userCanceled = false;
        ProgressBarInit("Exporting scene - preparing...", window);
        yield return null;

        var bakeryRuntimePath = ftLightmaps.GetRuntimePath();
        gstorage = AssetDatabase.LoadAssetAtPath(bakeryRuntimePath + "ftGlobalStorage.asset", typeof(ftGlobalStorage)) as ftGlobalStorage;

        var objsToWrite = new List<GameObject>();
        var objsToWriteLightmapped = new List<bool>();
        var objsToWriteGroup = new List<BakeryLightmapGroup>();
        var objsToWriteHolder = new List<GameObject>();
        var objsToWriteScaleOffset = new List<Vector4>();
        var objsToWriteUVOverride = new List<Vector2[]>();
        var holderRect = new Dictionary<GameObject, Rect>();
        var time = GetTime();
        var ms = time;
        var startMsU = ms;
        double totalTime = GetTime();
        double vbTimeRead = 0;
        double vbTimeWrite = 0;
        double vbTimeWriteFull = 0;
        double vbTimeWriteT = 0;
        double vbTimeWriteT2 = 0;
        double vbTimeWriteT3 = 0;
        double ibTime = 0;
        var objsToWriteVerticesPosW = new List<Vector3[]>();
        var objsToWriteVerticesNormalW = new List<Vector3[]>();
        var objsToWriteVerticesTangentW = new List<Vector4[]>();
        var objsToWriteVerticesUV = new List<Vector2[]>();
        var objsToWriteVerticesUV2 = new List<Vector2[]>();
        var objsToWriteIndices = new List<int[][]>();
        var sceneCount = SceneManager.sceneCount;
        var storages = new ftLightmapsStorage[sceneCount];
        var sceneToID = new Dictionary<Scene, int>();
        var indicesOpaque = new List<int>();
        var indicesTransparent = new List<int>();
        List<int>[] indicesOpaqueLOD = null;
        List<int>[] indicesTransparentLOD = null;

        bool tangentSHLights = false;
        var All2 = FindObjectsOfType(typeof(BakerySkyLight));
        for(int i=0; i<All2.Length; i++)
        {
            var obj = All2[i] as BakerySkyLight;
            if (!obj.enabled) continue;
            if (!obj.gameObject.activeInHierarchy) continue;
            if (obj.tangentSH)
            {
                tangentSHLights = true;
                break;
            }
        }

        //var heightmapList = new List<IntPtr>(); // global list of heightmap textures
        //var heightmapListTex = new List<Texture>();
        //var heightmapListBounds = new List<float>(); // minBounds, maxBounds (multiples of 6)
        //int heightmapCounter = 0;

        // Per-LMGroup data
        var lmAlbedoList = new List<IntPtr>(); // list of albedo texture for UV GBuffer rendering
        var lmAlbedoListTex = new List<Texture>();
        var lmAlphaList = new List<IntPtr>(); // list of alpha textures for alpha buffer generation
        var lmAlphaListTex = new List<Texture>();
        var lmAlphaRefList = new List<float>(); // list of alpha texture refs
        var lmBounds = new List<Bounds>(); // list of bounding boxes around LMGroups for testing lights
        // lod-related
        var lmVOffset = new List<int>();
        var lmUVArrays = new List<List<float>>();
        var lmUVArrays2 = new List<float[]>();
        var lmUVArrays3 = new List<float[]>();
        var lmIndexArrays = new List<List<int>>();
        var lmIndexArrays2 = new List<int[]>();
        var lmLocalToGlobalIndices = new List<List<int>>();

        vbtraceTexPosNormalArray = new List<float>();
        vbtraceTexUVArray = new List<float>();

        sceneLodsUsed = 0;

        float smallestMapScale = 1;
        float colorScale = 1.0f / (1 << (int)((1.0f - ftBuildGraphics.mainLightmapScale) * 6));
        float maskScale = 1.0f / (1 << (int)((1.0f - ftBuildGraphics.maskLightmapScale) * 6));
        float dirScale = 1.0f / (1 << (int)((1.0f - ftBuildGraphics.dirLightmapScale) * 6));
        smallestMapScale = Mathf.Min(colorScale, maskScale);
        smallestMapScale = Mathf.Min(smallestMapScale, dirScale);

        int lmid = 0; // LMID counter

        try
        {
            if (scenePath == "" || !Directory.Exists(scenePath))
            {
                // Default scene path is TEMP/frender
                var tempDir = System.Environment.GetEnvironmentVariable("TEMP", System.EnvironmentVariableTarget.Process);
                scenePath = tempDir + "\\frender";
                if (!Directory.Exists(scenePath)) Directory.CreateDirectory(scenePath);
            }

            //bakedUV1BufferList = new List<Vector2>();
            for(int i=0; i<sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                var gg = ftLightmaps.FindInScene("!ftraceLightmaps", scene);
                storages[i] = gg.GetComponent<ftLightmapsStorage>();
                if (modifyLightmapStorage)
                {
                    storages[i].bakedRenderers = new List<MeshRenderer>();
                    storages[i].bakedIDs = new List<int>();
                    storages[i].bakedScaleOffset = new List<Vector4>();
                    storages[i].bakedVertexOffset = new List<int>();
                    storages[i].bakedVertexColorMesh = new List<Mesh>();
                    storages[i].bakedRenderersTerrain = new List<Terrain>();
                    storages[i].bakedIDsTerrain = new List<int>();
                    storages[i].bakedScaleOffsetTerrain = new List<Vector4>();
                    storages[i].hasEmissive = new List<bool>();
                    storages[i].lmGroupLODResFlags = null;
                    storages[i].lmGroupMinLOD = null;
                    storages[i].lmGroupLODMatrix = null;
                    storages[i].nonBakedRenderers = new List<MeshRenderer>();
                }
                storages[i].implicitGroups = new List<UnityEngine.Object>();
                storages[i].implicitGroupedObjects = new List<GameObject>();
                sceneToID[scene] = i;
            }

            var go = GameObject.Find("!ftraceLightmaps");
            storage = go.GetComponent<ftLightmapsStorage>();

            groupList = new List<BakeryLightmapGroup>(); // LMGroup list
        }
        catch
        {
            DebugLogError("Error exporting scene - see console for details");
            CloseAllFiles();
            userCanceled = true;
            ProgressBarEnd();
            throw;
        }

        // Create LMGroup for light probes
        if (ftRenderLightmap.lightProbeMode == ftRenderLightmap.LightProbeMode.L1 && renderTextures && !atlasOnly)
        {
            var probes = LightmapSettings.lightProbes;
            if (probes == null)
            {
                DebugLogError("No probes in LightingDataAsset");
                yield break;
            }
            var positions = probes.positions;

            int atlasTexSize = (int)Mathf.Ceil(Mathf.Sqrt((float)probes.count));
            atlasTexSize = (int)Mathf.Ceil(atlasTexSize / (float)ftRenderLightmap.tileSize) * ftRenderLightmap.tileSize;
            var uvpos = new float[atlasTexSize * atlasTexSize * 4];
            var uvnormal = new byte[atlasTexSize * atlasTexSize * 4];

            for(int i=0; i<probes.count; i++)
            {
                int x = i % atlasTexSize;
                int y = i / atlasTexSize;
                int index = y * atlasTexSize + x;
                uvpos[index * 4] =     positions[i].x;
                uvpos[index * 4 + 1] = positions[i].y;
                uvpos[index * 4 + 2] = positions[i].z;
                uvpos[index * 4 + 3] = 1.0f;
                uvnormal[index * 4 + 1] = 255;
                uvnormal[index * 4 + 3] = 255;
            }

            var posFile = new byte[128 + uvpos.Length * 4];
            System.Buffer.BlockCopy(ftDDS.ddsHeaderFloat4, 0, posFile, 0, 128);
            System.Buffer.BlockCopy(BitConverter.GetBytes(atlasTexSize), 0, posFile, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(atlasTexSize), 0, posFile, 16, 4);
            System.Buffer.BlockCopy(uvpos, 0, posFile, 128, uvpos.Length * 4);
            SaveGBufferMapFromRAM(posFile, posFile.Length, scenePath + "/uvpos_probes" + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"), ftRenderLightmap.compressedGBuffer);
            GL.IssuePluginEvent(8);
            yield return null;

            var posNormal = new byte[128 + uvnormal.Length];
            System.Buffer.BlockCopy(ftDDS.ddsHeaderRGBA8, 0, posNormal, 0, 128);
            System.Buffer.BlockCopy(BitConverter.GetBytes(atlasTexSize), 0, posNormal, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(atlasTexSize), 0, posNormal, 16, 4);
            System.Buffer.BlockCopy(uvnormal, 0, posNormal, 128, uvnormal.Length);
            SaveGBufferMapFromRAM(posNormal, posNormal.Length, scenePath + "/uvnormal_probes" + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"), ftRenderLightmap.compressedGBuffer);
            GL.IssuePluginEvent(8);
            yield return null;

            lightProbeLMGroup = ScriptableObject.CreateInstance<BakeryLightmapGroup>();
            lightProbeLMGroup.name = "probes";
            lightProbeLMGroup.probes = true;
            lightProbeLMGroup.isImplicit = true;
            lightProbeLMGroup.resolution = 256;
            lightProbeLMGroup.bitmask = 1;
            lightProbeLMGroup.mode = BakeryLightmapGroup.ftLMGroupMode.Vertex;
            lightProbeLMGroup.id = lmid;
            lightProbeLMGroup.totalVertexCount = probes.count;
            lightProbeLMGroup.vertexCounter = 0;
            lightProbeLMGroup.renderDirMode = BakeryLightmapGroup.RenderDirMode.ProbeSH;
            groupList.Add(lightProbeLMGroup);
            lmBounds.Add(new Bounds(new Vector3(0,0,0), new Vector3(10000,10000,10000)));
            lmid++;

            storages[sceneToID[EditorSceneManager.GetActiveScene()]].implicitGroups.Add(lightProbeLMGroup);
            storages[sceneToID[EditorSceneManager.GetActiveScene()]].implicitGroupedObjects.Add(null);
        }

        try
        {
            ms = GetTime();

            //if (!onlyUVdata)
            //{
                time = ms;

                //var objects = UnityEngine.Object.FindObjectsOfTypeAll(typeof(GameObject));
                var objects = Resources.FindObjectsOfTypeAll(typeof(GameObject));

                // Find explicit LMGroups
                // (Also init lmBounds and LMID)
                var groupSelectors = new List<BakeryLightmapGroupSelector>(FindObjectsOfType(typeof(BakeryLightmapGroupSelector)) as BakeryLightmapGroupSelector[]);
                for(int i=0; i<groupSelectors.Count; i++)
                {
                    var lmgroup = groupSelectors[i].lmgroupAsset as BakeryLightmapGroup;
                    if (lmgroup == null) continue;
                    if (!groupList.Contains(lmgroup))
                    {
                        lmgroup.id = lmid;
                        lmgroup.sceneLodLevel = -1;
                        lmgroup.sceneName = "";
                        groupList.Add(lmgroup);
                        lmBounds.Add(new Bounds(new Vector3(0,0,0), new Vector3(0,0,0)));
                        lmid++;
                    }
                    EditorUtility.SetDirty(lmgroup);
                }


                // Convert terrains to meshes
                terrainObjectList = new List<GameObject>();
                terrainObjectToActual = new List<Terrain>();
                terrainObjectToHeightMap = new List<Texture>();
                terrainObjectToBounds = new List<float>();
                terrainObjectToBoundsUV = new List<float>();
                terrainObjectToLMID = new List<int>();
                terrainObjectToHeightMips = new List<List<float[]>>();
                //terrainObjectToNormalMips = new List<List<Vector3[]>>();
                //terrainObjectToNormalMip0 = new List<Vector3[]>();
                temporaryAreaLightMeshList = new List<GameObject>();
                temporaryAreaLightMeshList2 = new List<BakeryLightMesh>();
                treeObjectList = new List<GameObject>();
                //Material tempAreaLightMaterial = null;
                var sceneHasStorage = new Dictionary<Scene,bool>();
                foreach(GameObject obj in objects)
                {
                    if (obj == null) continue;

                    // Also check for storage count in each scene
                    if (obj.name == "!ftraceLightmaps")
                    {
                        if (!sceneHasStorage.ContainsKey(obj.scene))
                        {
                            sceneHasStorage[obj.scene] = true;
                        }
                        else
                        {
                            ProgressBarEnd(false);
                            if (!EditorUtility.DisplayDialog("Bakery", "Scene " + obj.scene.name + " has multiple lightmap storage objects. This is not currently supported. Make sure you don't bake a scene with already baked prefabs.", "Continue anyway", "Cancel"))
                            {
                                CloseAllFiles();
                                userCanceled = true;
                                ProgressBarEnd();
                                yield break;
                            }
                            ProgressBarInit("Exporting scene - preparing...", window);
                        }
                    }


                    // Also add temporary meshes to area lights
                    var areaLightMesh = obj.GetComponent<BakeryLightMesh>();
                    if (areaLightMesh != null)
                    {
                        var areaLight = obj.GetComponent<Light>();
                        var mr = obj.GetComponent<MeshRenderer>();
                        var mf = obj.GetComponent<MeshFilter>();
                        //var prevMR = mr;
                        //var prevMF = mf;
                        //Mesh prevMesh = null;
                        //if (prevMF != null) prevMesh = prevMF.sharedMesh;
                        //Material[] prevMats = null;
                        //if (prevMR != null) prevMats = prevMR.sharedMaterials;
                        //int prevFlags = (mr!=null? 1 : 0) | (mf!=null? 2 : 0);
                        if (!areaLightMesh.selfShadow) continue;
                        if (areaLight != null && areaLight.type == LightType.Area && (mr == null || mf == null))
                        {
                            var areaObj = new GameObject();
                            mf = areaObj.AddComponent<MeshFilter>();
                            mf.sharedMesh = BuildAreaLightMesh(areaLight);
                            mr = areaObj.AddComponent<MeshRenderer>();
                            //if (tempAreaLightMaterial == null) tempAreaLightMaterial = new Material(Shader.Find("Unlit/Bakery Light"));
                            //mr.material = tempAreaLightMaterial;
                            var props = new MaterialPropertyBlock();
                            props.SetColor("_Color", areaLightMesh.color);
                            props.SetFloat("intensity", areaLightMesh.intensity);
                            if (areaLightMesh.texture != null) props.SetTexture("_MainTex", areaLightMesh.texture);
                            mr.SetPropertyBlock(props);
                            GameObjectUtility.SetStaticEditorFlags(areaObj, StaticEditorFlags.LightmapStatic);
                            temporaryAreaLightMeshList.Add(areaObj);
                            temporaryAreaLightMeshList2.Add(areaLightMesh);

                            var xformSrc = obj.transform;
                            var xformDest = areaObj.transform;
                            xformDest.position = xformSrc.position;
                            xformDest.rotation = xformSrc.rotation;
                            var srcMtx = xformSrc.localToWorldMatrix;
                            xformDest.localScale = new Vector3(srcMtx.GetColumn(0).magnitude, srcMtx.GetColumn(1).magnitude, srcMtx.GetColumn(2).magnitude);

                            //temporaryAreaLightPrevComponents.Add(prevFlags);
                            //temporaryAreaLightPrevMesh.Add(prevMesh);
                            //temporaryAreaLightPrevMat.Add(prevMR==null? new Material[0] : prevMats);
                            continue;
                        }
                    }


                    var terr = obj.GetComponent<Terrain>();
                    if (terr == null) continue;

                    if (!obj.activeInHierarchy) continue;
                    if ((obj.hideFlags & (HideFlags.DontSave|HideFlags.HideAndDontSave)) != 0) continue; // skip temp objects
                    if (obj.tag == "EditorOnly") continue; // skip temp objects
                    if ((GameObjectUtility.GetStaticEditorFlags(obj) & StaticEditorFlags.LightmapStatic) == 0) continue; // skip dynamic

                    var so = new SerializedObject(terr);
                    var scaleInLmTerr = so.FindProperty("m_ScaleInLightmap").floatValue;

                    var terrParent = new GameObject();
                    SceneManager.MoveGameObjectToScene(terrParent, obj.scene);
                    terrParent.transform.parent = obj.transform.parent;
                    var expGroup = obj.GetComponent<BakeryLightmapGroupSelector>();
                    if (expGroup != null)
                    {
                        var expGroup2 = terrParent.AddComponent<BakeryLightmapGroupSelector>();
                        expGroup2.lmgroupAsset = expGroup.lmgroupAsset;
                        expGroup2.instanceResolutionOverride = expGroup.instanceResolutionOverride;
                        expGroup2.instanceResolution = expGroup.instanceResolution;
                    }
                    terrParent.name = "__ExportTerrainParent";
                    terrainObjectList.Add(terrParent);
                    terrainObjectToActual.Add(terr);

                    var data = terr.terrainData;
                    int res = data.heightmapResolution;
                    var heightmap = data.GetHeights(0, 0, res, res);
                    var uvscale = new Vector2(1,1) / (res-1);
                    var uvoffset = new Vector2(0,0);
                    var gposOffset = obj.transform.position;
                    float scaleX = data.size.x / (res-1);
                    float scaleY = data.size.y;
                    float scaleZ = data.size.z / (res-1);

                    int patchRes = res;
                    while(patchRes > 254) patchRes = 254;//patchRes /= 2;
                    int numVerts = patchRes * patchRes;
                    int numPatches = (int)Mathf.Ceil(res / (float)patchRes);

                    // Gen terrain texture
                    var oldMat = terr.materialTemplate;
                    var oldMatType = terr.materialType;
                    var oldPos = obj.transform.position;
                    var unlitTerrainMat = new Material(Shader.Find("Hidden/ftUnlitTerrain"));
                        //unlitTerrainMat = AssetDatabase.LoadAssetAtPath("Assets/Bakery/ftUnlitTerrain.mat", typeof(Material)) as Material;
                    terr.materialTemplate = unlitTerrainMat;
                    terr.materialType = Terrain.MaterialType.Custom;
                    obj.transform.position = new Vector3(-10000, -10000, -10000); // let's hope it's not the worst idea
                    var tempCamGO = new GameObject();
                    tempCamGO.transform.parent = obj.transform;
                    tempCamGO.transform.localPosition = new Vector3(data.size.x * 0.5f, scaleY + 1, data.size.z * 0.5f);
                    tempCamGO.transform.eulerAngles = new Vector3(90,0,0);
                    var tempCam = tempCamGO.AddComponent<Camera>();
                    tempCam.orthographic = true;
                    tempCam.orthographicSize = Mathf.Max(data.size.x, data.size.z) * 0.5f;
                    tempCam.aspect = Mathf.Max(data.size.x, data.size.z) / Mathf.Min(data.size.x, data.size.z);
                    tempCam.enabled = false;
                    tempCam.targetTexture =
                        new RenderTexture(data.baseMapResolution, data.baseMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    var tex = new Texture2D(data.baseMapResolution, data.baseMapResolution, TextureFormat.ARGB32, true, false);
                    RenderTexture.active = tempCam.targetTexture;
                    tempCam.Render();
                    terr.materialTemplate = oldMat;
                    terr.materialType = oldMatType;
                    obj.transform.position = oldPos;
                    RenderTexture.active = tempCam.targetTexture;
                    tex.ReadPixels(new Rect(0,0,data.baseMapResolution, data.baseMapResolution), 0, 0, true);
                    tex.Apply();
                    unlitTerrainMat.mainTexture = tex;
                    Graphics.SetRenderTarget(null);
                    DestroyImmediate(tempCamGO);

                    if (exportTerrainAsHeightmap)
                    {
                        var hmap = new BinaryWriter(File.Open(scenePath + "/height" + terrainObjectToHeightMap.Count + ".dds", FileMode.Create));
                        if (ftRenderLightmap.clientMode) ftClient.serverFileList.Add("height" + terrainObjectToHeightMap.Count + ".dds");

                        hmap.Write(ftDDS.ddsHeaderR32F);
                        var bytes = new byte[res * res * 4];

                        // Normalize heights
                        float maxHeight = 0;
                        float height;
                        for(int y=0; y<res; y++)
                        {
                            for(int x=0; x<res; x++)
                            {
                                height = heightmap[x,y];
                                if (height > maxHeight) maxHeight = height;
                            }
                        }
                        maxHeight = Mathf.Max(maxHeight, 0.0001f);
                        float invMaxHeight = 1.0f / maxHeight;
                        for(int y=0; y<res; y++)
                        {
                            for(int x=0; x<res; x++)
                            {
                                heightmap[x,y] *= invMaxHeight;
                            }
                        }
                        float aabbHeight = maxHeight * scaleY;
                        //Debug.Log("aabbHeight: " + aabbHeight);

                        // First mip is the real heightmap
                        System.Buffer.BlockCopy(heightmap, 0, bytes, 0, bytes.Length);
                        hmap.Write(bytes);

                        var htex = new Texture2D(res, res, TextureFormat.RFloat, false, true);
                        htex.LoadRawTextureData(bytes);
                        htex.Apply();
                        terrainObjectToHeightMap.Add(htex);

                        // min
                        terrainObjectToBounds.Add(obj.transform.position.x);
                        terrainObjectToBounds.Add(obj.transform.position.y);
                        terrainObjectToBounds.Add(obj.transform.position.z);
                        // max
                        terrainObjectToBounds.Add(obj.transform.position.x + data.size.x);
                        terrainObjectToBounds.Add(obj.transform.position.y + aabbHeight);
                        terrainObjectToBounds.Add(obj.transform.position.z + data.size.z);

                        // filled later
                        terrainObjectToLMID.Add(0);
                        terrainObjectToBoundsUV.Add(0);
                        terrainObjectToBoundsUV.Add(0);
                        terrainObjectToBoundsUV.Add(0);
                        terrainObjectToBoundsUV.Add(0);

                        // Second mip is max() of 3x3 corners
                        float[] floats = null;
                        float[] floatsPrev = null;
                        float[] floatsTmp;
                        int mipCount = 1;
                        int mipRes = res / 2;
                        //int prevRes = res;
                        float h00, h10, h01, h11;
                        /*Vector3 n00, n10, n01, n11;
                        Vector3[] normals = null;
                        Vector3[] normalsPrev = null;

                        var origNormals = new Vector3[res * res];
                        for(int y=0; y<res; y++)
                        {
                            for(int x=0; x<res; x++)
                            {
                                origNormals[y*res+x] = data.GetInterpolatedNormal(x / (float)res, y / (float)res);
                            }
                        }*/
                        int terrIndex = terrainObjectToHeightMap.Count - 1;//terrainObjectToNormalMip0.Count;
                        //terrainObjectToNormalMip0.Add(origNormals);
                        //normalsPrev = origNormals;
                        terrainObjectToHeightMips.Add(new List<float[]>());
                        //terrainObjectToNormalMips.Add(new List<Vector3[]>());

                        if (mipRes > 0)
                        {
                            floats = new float[mipRes * mipRes];
                            //normals = new Vector3[mipRes * mipRes];
                            for(int y=0; y<mipRes; y++)
                            {
                                for(int x=0; x<mipRes; x++)
                                {
                                    /*h00 = heightmap[y*2,x*2];
                                    h10 = heightmap[y*2,x*2+1];
                                    h01 = heightmap[y*2+1,x*2];
                                    h11 = heightmap[y*2+1,x*2+1];
                                    height = h00 > h10 ? h00 : h10;
                                    height = height > h01 ? height : h01;
                                    height = height > h11 ? height : h11;
                                    floats[y*mipRes+x] = height;*/

                                    float maxVal = 0;
                                    for(int yy=0; yy<3; yy++)
                                    {
                                        for(int xx=0; xx<3; xx++)
                                        {
                                            float val = heightmap[y*2+yy, x*2+xx];
                                            if (val > maxVal) maxVal = val;
                                        }
                                    }
                                    floats[y*mipRes+x] = maxVal;

                                    //n00 = normalsPrev[y*2 * res + x*2];
                                    //n10 = normalsPrev[y*2 * res + x*2+1];
                                    //n01 = normalsPrev[(y*2+1) * res + x*2];
                                    //n11 = normalsPrev[(y*2+1) * res + x*2+1];
                                    //normals[y*mipRes+x] = (n00 + n10 + n01 + n11);
                                }
                            }

                            System.Buffer.BlockCopy(floats, 0, bytes, 0, mipRes*mipRes*4);
                            hmap.Write(bytes, 0, mipRes*mipRes*4);

                            float[] storedMip = new float[mipRes*mipRes];
                            System.Buffer.BlockCopy(floats, 0, storedMip, 0, mipRes*mipRes*4);
                            terrainObjectToHeightMips[terrIndex].Add(storedMip);
                            //terrainObjectToNormalMips[terrIndex].Add(normals);

                            mipCount++;
                            mipRes /= 2;
                        }

                        // Next mips are regular max() of 4 texels
                        while(mipRes > 1)
                        {
                            if (floatsPrev == null)
                            {
                                floatsPrev = floats;
                                floats = new float[mipRes * mipRes];
                            }

                            //normalsPrev = normals;
                            //normals = new Vector3[mipRes * mipRes];

                            for(int y=0; y<mipRes; y++)
                            {
                                for(int x=0; x<mipRes; x++)
                                {
                                    h00 = floatsPrev[y*2 * mipRes*2 + x*2];
                                    h10 = floatsPrev[y*2 * mipRes*2 + x*2+1];
                                    h01 = floatsPrev[(y*2+1) * mipRes*2 + x*2];
                                    h11 = floatsPrev[(y*2+1) * mipRes*2 + x*2+1];
                                    height = h00 > h10 ? h00 : h10;
                                    height = height > h01 ? height : h01;
                                    height = height > h11 ? height : h11;
                                    floats[y*mipRes+x] = height;

                                    //n00 = normalsPrev[y*2 * mipRes*2 + x*2];
                                    //n10 = normalsPrev[y*2 * mipRes*2 + x*2+1];
                                    //n01 = normalsPrev[(y*2+1) * mipRes*2 + x*2];
                                    //n11 = normalsPrev[(y*2+1) * mipRes*2 + x*2+1];
                                    //normals[y*mipRes+x] = (n00 + n10 + n01 + n11);
                                }
                            }

                            System.Buffer.BlockCopy(floats, 0, bytes, 0, mipRes*mipRes*4);
                            hmap.Write(bytes, 0, mipRes*mipRes*4);

                            float[] storedMip = new float[mipRes*mipRes];
                            System.Buffer.BlockCopy(floats, 0, storedMip, 0, mipRes*mipRes*4);
                            terrainObjectToHeightMips[terrIndex].Add(storedMip);
                            //terrainObjectToNormalMips[terrIndex].Add(normals);

                            mipCount++;
                            mipRes /= 2;

                            floatsTmp = floatsPrev;
                            floatsPrev = floats;
                            floats = floatsTmp;
                        }

                        hmap.BaseStream.Seek(12, SeekOrigin.Begin);
                        hmap.Write(res);
                        hmap.Write(res);
                        hmap.BaseStream.Seek(28, SeekOrigin.Begin);
                        hmap.Write(mipCount);
                        hmap.Close();

                        // Create dummy plane for packing/albedo/emissive purposes
                        var mesh = new Mesh();
                        mesh.vertices = new Vector3[] { gposOffset,
                                                        gposOffset + new Vector3(data.size.x, 0, 0),
                                                        gposOffset + new Vector3(data.size.x, 0, data.size.z),
                                                        gposOffset + new Vector3(0, 0, data.size.z) };
                        mesh.triangles = new int[]{0,1,2, 2,3,0};
                        mesh.normals = new Vector3[]{Vector3.up, Vector3.up, Vector3.up, Vector3.up};
                        mesh.uv = new Vector2[]{new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)};
                        mesh.uv2 = mesh.uv;

                        var terrGO = new GameObject();
                        terrGO.name = "__ExportTerrain";
                        GameObjectUtility.SetStaticEditorFlags(terrGO, StaticEditorFlags.LightmapStatic);
                        terrGO.transform.parent = terrParent.transform;
                        var mf = terrGO.AddComponent<MeshFilter>();
                        var mr = terrGO.AddComponent<MeshRenderer>();
                        mf.sharedMesh = mesh;
                        mr.sharedMaterial = (terr.materialTemplate == null) ? unlitTerrainMat : terr.materialTemplate;

                        terrGO.transform.position = obj.transform.position;

                        var so2 = new SerializedObject(mr);
                        so2.FindProperty("m_ScaleInLightmap").floatValue = scaleInLmTerr;
                        so2.ApplyModifiedProperties();

                        //terrainObjectList.Add(terrGO);
                        //terrainObjectToActual.Add(terr);
                    }
                    else
                    {
                        for (int patchX=0; patchX<numPatches; patchX++)
                        {
                            for (int patchY=0; patchY<numPatches; patchY++)
                            {
                                int patchResX = patchX < numPatches-1 ? patchRes : (res - patchRes*(numPatches-1));
                                int patchResY = patchY < numPatches-1 ? patchRes : (res - patchRes*(numPatches-1));
                                if (patchX < numPatches-1) patchResX += 1;
                                if (patchY < numPatches-1) patchResY += 1;
                                numVerts = patchResX * patchResY;
                                var posOffset = gposOffset + new Vector3(patchX*patchRes*scaleX, 0, patchY*patchRes*scaleZ);

                                var positions = new Vector3[numVerts];
                                var uvs = new Vector2[numVerts];
                                var normals = new Vector3[numVerts];
                                var indices = new int[(patchResX-1) * (patchResY-1) * 2 * 3];
                                int vertOffset = 0;
                                int indexOffset = 0;

                                for (int x=0;x<patchResX;x++)
                                {
                                    for (int y=0;y<patchResY;y++)
                                    {
                                        int gx = x + patchX * patchRes;
                                        int gy = y + patchY * patchRes;

                                        int index = x * patchResY + y;
                                        float height = heightmap[gy,gx];

                                        positions[index] = new Vector3(x * scaleX, height * scaleY, y * scaleZ) + posOffset;
                                        uvs[index] = new Vector2(gx * uvscale.x + uvoffset.x, gy * uvscale.y + uvoffset.y);

                                        normals[index] = data.GetInterpolatedNormal(gx / (float)res, gy / (float)res);

                                        if (x < patchResX-1 && y < patchResY-1)
                                        {
                                            indices[indexOffset] = vertOffset + patchResY + 1;
                                            indices[indexOffset + 1] = vertOffset + patchResY;
                                            indices[indexOffset + 2] = vertOffset;

                                            indices[indexOffset + 3] = vertOffset + 1;
                                            indices[indexOffset + 4] = vertOffset + patchResY + 1;
                                            indices[indexOffset + 5] = vertOffset;

                                            indexOffset += 6;
                                        }

                                        vertOffset++;
                                    }
                                }

                                var mesh = new Mesh();
                                mesh.vertices = positions;
                                mesh.triangles = indices;
                                mesh.normals = normals;
                                mesh.uv = uvs;
                                mesh.uv2 = uvs;

                                var terrGO = new GameObject();
                                terrGO.name = "__ExportTerrain";
                                GameObjectUtility.SetStaticEditorFlags(terrGO, StaticEditorFlags.LightmapStatic);
                                terrGO.transform.parent = terrParent.transform;
                                var mf = terrGO.AddComponent<MeshFilter>();
                                var mr = terrGO.AddComponent<MeshRenderer>();
                                mf.sharedMesh = mesh;
                                mr.sharedMaterial = (terr.materialTemplate == null) ? unlitTerrainMat : terr.materialTemplate;

                                var so2 = new SerializedObject(mr);
                                so2.FindProperty("m_ScaleInLightmap").floatValue = scaleInLmTerr;
                                so2.ApplyModifiedProperties();

                                terrainObjectList.Add(terrGO);
                                terrainObjectToActual.Add(terr);
                            }
                        }
                    }

                    if (exportTerrainTrees && terr.drawTreesAndFoliage)
                    {
                        var trees = data.treeInstances;
                        for (int t = 0; t < trees.Length; t++)
                        {
                            Vector3 pos = Vector3.Scale(trees[t].position, data.size) + obj.transform.position;

                            var treeProt = data.treePrototypes[trees[t].prototypeIndex];
                            var prefab = treeProt.prefab;

                            var newObj = GameObject.Instantiate(prefab, pos, Quaternion.AngleAxis(trees[t].rotation, Vector3.up)) as GameObject;
                            newObj.name = "__Export" + newObj.name;
                            treeObjectList.Add(newObj);

                            var lodGroup = newObj.GetComponent<LODGroup>();
                            if (lodGroup == null)
                            {
                                var renderers = newObj.GetComponentsInChildren<MeshRenderer>();
                                for(int r=0; r<renderers.Length; r++)
                                {
                                    GameObjectUtility.SetStaticEditorFlags(renderers[r].gameObject, StaticEditorFlags.LightmapStatic);
                                    var s = new SerializedObject(renderers[r]);
                                    s.FindProperty("m_ScaleInLightmap").floatValue = 0;
                                    s.ApplyModifiedProperties();
                                }
                            }
                            else
                            {
                                var lods = lodGroup.GetLODs();
                                for (int tl = 0; tl < lods.Length; tl++)
                                {
                                    for (int h = 0; h < lods[tl].renderers.Length; h++)
                                    {
                                        GameObjectUtility.SetStaticEditorFlags(lods[tl].renderers[h].gameObject, tl==0 ? StaticEditorFlags.LightmapStatic : 0);
                                        if (tl == 0)
                                        {
                                            var s = new SerializedObject(lods[tl].renderers[h]);
                                            s.FindProperty("m_ScaleInLightmap").floatValue = 0;
                                            s.ApplyModifiedProperties();
                                        }
                                    }
                                }
                            }

                            var xform = newObj.transform;
                            xform.localScale = new Vector3(trees[t].widthScale, trees[t].heightScale, trees[t].widthScale);
                        }
                    }
                }

                if (terrainObjectList.Count > 0 || treeObjectList.Count > 0 || temporaryAreaLightMeshList.Count > 0)
                {
                    //objects = UnityEngine.Object.FindObjectsOfTypeAll(typeof(GameObject));
                    objects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
                }

                autoAtlasGroups = new List<BakeryLightmapGroup>();
                autoAtlasGroupRootNodes = new List<AtlasNode>();
                autoVertexGroup = null;
                tempStorage.implicitGroupMap = new Dictionary<GameObject, UnityEngine.Object>(); // implicit holder -> LMGroup map. used by GetLMGroupFromObject

                // Find LODGroups -> LODs -> scene-wide LOD distances
                // Map objects to scene-wide LOD levels
                const int maxSceneLodLevels = 100;
                var sceneLodUsed = new int[maxSceneLodLevels];
                for(int i=0; i<maxSceneLodLevels; i++) sceneLodUsed[i] = -1;
                var lodGroups = Resources.FindObjectsOfTypeAll(typeof(LODGroup));
                var objToLodLevel = new Dictionary<GameObject,int>(); // defines atlas LOD level
                var objToLodLevelVisible = new Dictionary<GameObject,List<int>>(); // defines LOD levels where this object is visible
                var lodLevelsInLodGroup = new List<int>[lodGroups.Length];
                var localLodLevelsInLodGroup = new List<int>[lodGroups.Length];
                int lcounter = -1;
                foreach(LODGroup lodgroup in lodGroups)
                {
                    lcounter++;
                    if (!lodgroup.enabled) continue;
                    var obj = lodgroup.gameObject;
                    if (obj == null) continue;
                    if (!obj.activeInHierarchy) continue;
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (path != "") continue; // must belond to scene
                    if ((obj.hideFlags & (HideFlags.DontSave|HideFlags.HideAndDontSave)) != 0) continue; // skip temp objects
                    if (obj.tag == "EditorOnly") continue; // skip temp objects

                    var lods = lodgroup.GetLODs();
                    if (lods.Length == 0) continue;

                    for(int i=0; i<lods.Length; i++)
                    {
                        var lodRenderers = lods[i].renderers;
                        if (lodRenderers.Length == 0) continue;

                        bool lightmappedLOD = false;
                        for(int j=0; j<lodRenderers.Length; j++)
                        {
                            var r = lodRenderers[j];
                            if (r == null) continue;
                            if (!r.enabled) continue;
                            if (!r.gameObject.activeInHierarchy) continue;
                            if ((r.gameObject.hideFlags & (HideFlags.DontSave|HideFlags.HideAndDontSave)) != 0) continue; // skip temp objects
                            if (r.gameObject.tag == "EditorOnly") continue; // skip temp objects
                            if ((GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.LightmapStatic) == 0) continue; // skip dynamic
                            var mr = r.gameObject.GetComponent<MeshRenderer>();
                            var mf = r.gameObject.GetComponent<MeshFilter>();
                            if (mr == null || mf == null) continue; // must have visible mesh
                            if (!mr.enabled) continue;
                            if (mf.sharedMesh == null) continue;

                            var so = new SerializedObject(mr);
                            var scaleInLm = so.FindProperty("m_ScaleInLightmap").floatValue;
                            if (scaleInLm == 0) continue;

                            lightmappedLOD = true;
                            break;
                        }
                        if (!lightmappedLOD) continue;

                        var lodDist = i == 0 ? 0 : (int)Mathf.Clamp((1.0f-lods[i-1].screenRelativeTransitionHeight) * (maxSceneLodLevels-1), 0, maxSceneLodLevels-1);
                        if (sceneLodUsed[lodDist] < 0)
                        {
                            sceneLodUsed[lodDist] = sceneLodsUsed;
                            sceneLodsUsed++;
                        }
                        int newLodLevel = sceneLodUsed[lodDist];

                        if (lodLevelsInLodGroup[lcounter] == null)
                        {
                            lodLevelsInLodGroup[lcounter] = new List<int>();
                            localLodLevelsInLodGroup[lcounter] = new List<int>();
                        }
                        if (lodLevelsInLodGroup[lcounter].IndexOf(newLodLevel) < 0)
                        {
                            lodLevelsInLodGroup[lcounter].Add(newLodLevel);
                            localLodLevelsInLodGroup[lcounter].Add(i);
                        }

                        for(int j=0; j<lodRenderers.Length; j++)
                        {
                            var r = lodRenderers[j];
                            if (r == null) continue;
                            int existingLodLevel = -1;
                            if (objToLodLevel.ContainsKey(r.gameObject)) existingLodLevel = objToLodLevel[r.gameObject];
                            if (existingLodLevel < newLodLevel)
                            {
                                objToLodLevel[r.gameObject] = newLodLevel;

                                // Collect LOD levels where this object is visible
                                List<int> visList;
                                if (!objToLodLevelVisible.TryGetValue(r.gameObject, out visList)) objToLodLevelVisible[r.gameObject] = visList = new List<int>();
                                visList.Add(newLodLevel);
                            }
                        }
                    }
                }

                // Sort scene LOD levels
                int counter = 0;
                var unsortedLodToSortedLod = new int[maxSceneLodLevels];
                for(int i=0; i<maxSceneLodLevels; i++)
                {
                    int unsorted = sceneLodUsed[i];
                    if (unsorted >= 0)
                    {
                        unsortedLodToSortedLod[unsorted] = counter;
                        sceneLodUsed[i] = counter;
                        counter++;
                    }
                }
                var keys = new GameObject[objToLodLevel.Count];
                counter = 0;
                foreach(var pair in objToLodLevel)
                {
                    keys[counter] = pair.Key;
                    counter++;
                }
                foreach(var key in keys)
                {
                    int unsorted = objToLodLevel[key];
                    objToLodLevel[key] = unsortedLodToSortedLod[unsorted];
                    var visList = objToLodLevelVisible[key];
                    for(int j=0; j<visList.Count; j++)
                    {
                        visList[j] = unsortedLodToSortedLod[visList[j]];
                    }
                }
                for(int i=0; i<lodLevelsInLodGroup.Length; i++)
                {
                    if (lodLevelsInLodGroup[i] == null) continue;
                    var levels = lodLevelsInLodGroup[i];
                    for(int j=0; j<levels.Count; j++)
                    {
                        levels[j] = unsortedLodToSortedLod[levels[j]];
                    }
                }

                // Fill LOD gaps
                for(int i=0; i<lodLevelsInLodGroup.Length; i++)
                {
                    if (lodLevelsInLodGroup[i] == null) continue;
                    var levels = lodLevelsInLodGroup[i];
                    var localLevels = localLodLevelsInLodGroup[i];
                    var lgroup = lodGroups[i] as LODGroup;
                    var lods = lgroup.GetLODs();
                    for(int j=0; j<levels.Count; j++)
                    {
                        int level = levels[j];
                        int localLevel = localLevels[j];
                        int nextLevel = (j == levels.Count-1) ? (sceneLodsUsed-1) : levels[j+1];
                        if (nextLevel - level > 1)
                        {
                            var lodRenderers = lods[localLevel].renderers;
                            for(int k=0; k<lodRenderers.Length; k++)
                            {
                                var r = lodRenderers[k];
                                if (r == null) continue;

                                var visList = objToLodLevelVisible[r.gameObject];
                                for(int l=level+1; l<nextLevel; l++)
                                {
                                    visList.Add(l);
                                }
                            }
                        }
                    }
                }

                // Find complete scene LOD range for each object
                /*foreach(var key in keys)
                {
                    var visList = objToLodLevelVisible[key];
                    var invisList = objToLodLevelInvisible[key];
                    var visList2 = new List<int>();
                    int prev = -1;
                    for(int j=0; j<visList.Count; j++)
                    {
                        int lod = visList[j];
                        if (prev >= 0 && (lod - prev > 1))
                        {
                            // Fill the gap
                            for(int k=prev+1; k<lod; k++)
                            {
                                if (invisList.IndexOf(k) >= 0) continue;
                                visList2.Add(k);
                            }
                        }
                        visList2.Add(lod);
                        prev = lod;
                    }
                    if (prev >= 0)
                    {
                        // Add more in the end
                        for(int j=prev+1; j<sceneLodsUsed; j++)
                        {
                            visList2.Add(j);
                        }
                    }

                    objToLodLevelVisible[key] = visList2;
                }*/

                /*//var sceneLodDistances = new float[sceneLodsUsed];
                //int sceneLodOffset = 0;
                for(int i=0; i<maxSceneLodLevels; i++)
                {
                    if (sceneLodUsed[i] >= 0)
                    {
                        Debug.LogError(sceneLodUsed[i]);
                        //sceneLodDistances[sceneLodOffset] = i / (float)maxSceneLodLevels-1);
                        //sceneLodOffset++;
                    }
                }*/
                Debug.Log("Scene LOD levels: " + sceneLodsUsed);

                // Init scene LOD index buffers
                indicesOpaqueLOD = new List<int>[sceneLodsUsed];
                indicesTransparentLOD = new List<int>[sceneLodsUsed];
                for(int i=0; i<sceneLodsUsed; i++)
                {
                    indicesOpaqueLOD[i] = new List<int>();
                    indicesTransparentLOD[i] = new List<int>();
                }

                // Sort objects by scene-wide LOD level
                if (sceneLodsUsed > 0)
                {
                    Array.Sort(objects, delegate(UnityEngine.Object a, UnityEngine.Object b)
                    {
                        if (a == null || b == null) return 0;
                        int lodLevelA = -1;
                        int lodLevelB = -1;
                        if (!objToLodLevel.TryGetValue((GameObject)a, out lodLevelA)) lodLevelA = -1;
                        if (!objToLodLevel.TryGetValue((GameObject)b, out lodLevelB)) lodLevelB = -1;
                        return lodLevelA.CompareTo(lodLevelB);
                    });
                }

                ftModelPostProcessor.Init();

                // Filter objects
                var objsToWriteNames = new List<string>();
                var prop = new MaterialPropertyBlock();
                foreach(GameObject obj in objects)
                {
                    if (obj == null) continue;
                    if (!obj.activeInHierarchy) continue;
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (path != "") continue; // must belond to scene
                    if ((obj.hideFlags & (HideFlags.DontSave|HideFlags.HideAndDontSave)) != 0) continue; // skip temp objects
                    if (obj.tag == "EditorOnly") continue; // skip temp objects

                    var areaLight = obj.GetComponent<BakeryLightMesh>();
                    if (areaLight == null)
                    {
                        int areaIndex = temporaryAreaLightMeshList.IndexOf(obj);
                        if (areaIndex >= 0) areaLight = temporaryAreaLightMeshList2[areaIndex];
                    }

                    if (areaLight != null && !areaLight.selfShadow) continue;
                    var mr = obj.GetComponent<MeshRenderer>();
                    var mf = obj.GetComponent<MeshFilter>();
                    if (mr == null || mf == null) continue; // must have visible mesh
#if UNITY_2018_1_OR_NEWER
                    if (mr.HasPropertyBlock())
                    {
                        // Reset shader props
                        mr.GetPropertyBlock(prop);
                        prop.SetFloat("bakeryLightmapMode", 0);
                        mr.SetPropertyBlock(prop);
                    }
#else
                    mr.GetPropertyBlock(prop);
                    if (!prop.isEmpty)
                    {
                        prop.SetFloat("bakeryLightmapMode", 0);
                        mr.SetPropertyBlock(prop);
                    }
#endif

                    if (((GameObjectUtility.GetStaticEditorFlags(obj) & StaticEditorFlags.LightmapStatic) == 0) && areaLight==null)
                    {
                        mr.lightmapIndex = 0xFFFF;
                        continue; // skip dynamic
                    }
                    if (!mr.enabled && areaLight == null) continue;
                    if (mf.sharedMesh == null) continue;
                    //if (mf.sharedMesh.uv.Length == 0 && mf.sharedMesh.uv2.Length == 0 && areaLight==null) continue; // must have UVs

                    var so = new SerializedObject(obj.GetComponent<Renderer>());
                    var scaleInLm = so.FindProperty("m_ScaleInLightmap").floatValue;

                    BakeryLightmapGroup group = null;
                    if (scaleInLm > 0)
                    {
                        group = GetLMGroupFromObjectExplicit(obj);
                        if (group != null)
                        {
                            // Set LOD level for explicit group
                            int lodLevel;
                            if (!objToLodLevel.TryGetValue(obj, out lodLevel)) lodLevel = -1;

                            if (group.sceneLodLevel == -1)
                            {
                                group.sceneLodLevel = lodLevel;
                            }
                            else
                            {
                                ProgressBarEnd(false);
                                if (!EditorUtility.DisplayDialog("Bakery", "Multiple LOD levels in " + group.name + ", this is not currently supported.", "Continue anyway", "Cancel"))
                                {
                                    CloseAllFiles();
                                    userCanceled = true;
                                    ProgressBarEnd();
                                    yield break;
                                }
                                ProgressBarInit("Exporting scene - preparing...", window);
                            }

                            if (splitByScene) group.sceneName = obj.scene.name;

                            // New explicit Pack Atlas holder selection
                            if (!group.isImplicit && group.mode == BakeryLightmapGroup.ftLMGroupMode.PackAtlas)
                            {
                                lmgroupHolder = obj; // by default pack each object
                                var prefabParent = PrefabUtility.GetPrefabParent(obj) as GameObject;
                                if (prefabParent != null)
                                {
                                    var ptype = PrefabUtility.GetPrefabType(prefabParent);
                                    if (ptype == PrefabType.ModelPrefab)
                                    {
                                        // but if object is a part of prefab/model
                                        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj.GetComponent<MeshFilter>().sharedMesh)) as ModelImporter;
                                        if (importer != null && !ModelUVsOverlap(importer, gstorage))
                                        {
                                            // or actually just non-overlapping model,
                                            // then pack it as a whole

                                            // find topmost asset parent
                                            var t = prefabParent.transform;
                                            while(t.parent != null) t = t.parent;
                                            var assetTopMost = t.gameObject;

                                            // find topmost scene instance parent
                                            var g = obj;
                                            while(PrefabUtility.GetPrefabParent(g) as GameObject != assetTopMost && g.transform.parent != null)
                                            {
                                                g = g.transform.parent.gameObject;
                                            }
                                            lmgroupHolder = g;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (autoVertexGroup == null)
                        {
                            autoVertexGroup = ScriptableObject.CreateInstance<BakeryLightmapGroup>();
                            autoVertexGroup.name = obj.scene.name + "_VLM";
                            autoVertexGroup.isImplicit = true;
                            autoVertexGroup.resolution = 256;
                            autoVertexGroup.bitmask = 1;
                            autoVertexGroup.mode = BakeryLightmapGroup.ftLMGroupMode.Vertex;
                            autoVertexGroup.id = lmid;
                            groupList.Add(autoVertexGroup);
                            lmBounds.Add(new Bounds(new Vector3(0,0,0), new Vector3(0,0,0)));
                            lmid++;
                        }
                        group = autoVertexGroup;

                        storages[sceneToID[obj.scene]].implicitGroupedObjects.Add(obj);
                        storages[sceneToID[obj.scene]].implicitGroups.Add(autoVertexGroup);
                        tempStorage.implicitGroupMap[obj] = autoVertexGroup;

                        storages[sceneToID[obj.scene]].nonBakedRenderers.Add(mr);
                    }

                    bool vertexBake = (group != null && group.mode == BakeryLightmapGroup.ftLMGroupMode.Vertex);
                    // must have UVs or be arealight or vertexbaked
                    var uv = mf.sharedMesh.uv;
                    var uv2 = mf.sharedMesh.uv2;
                    if (uv.Length == 0 && uv2.Length == 0 && areaLight==null && !vertexBake) continue;

                    var usedUVs = uv2.Length == 0 ? uv : uv2;
                    //bool validUVs = true;
                    for(int v=0; v<usedUVs.Length; v++)
                    {
                        if (usedUVs[v].x < -0.0001f || usedUVs[v].x > 1.0001f || usedUVs[v].y < -0.0001f || usedUVs[v].y > 1.0001f)
                        {
                            Debug.LogWarning("Mesh " + mf.sharedMesh.name + " on object " + obj.name + " possibly has incorrect UVs (UV2: " + (uv2.Length == 0 ? "no" : "yes")+", U: " + usedUVs[v].x + ", V: " + usedUVs[v].y + ")");
                            //validUVs = false;
                            break;
                        }
                    }
                    //if (!validUVs) continue;

                    if (vertexBake)
                    {
                        group.totalVertexCount = 0;
                        group.vertexCounter = 0;
                    }

                    objsToWrite.Add(obj);
                    objsToWriteNames.Add(obj.name);
                    objsToWriteLightmapped.Add((scaleInLm > 0 && areaLight == null) ? true : false);
                    objsToWriteGroup.Add(group);
                    objsToWriteHolder.Add(lmgroupHolder);

                    objsToWriteVerticesUV.Add(uv);
                    objsToWriteVerticesUV2.Add(uv2);
                    var inds = new int[mf.sharedMesh.subMeshCount][];
                    for(int n=0; n<inds.Length; n++) inds[n] = mf.sharedMesh.GetTriangles(n);
                    objsToWriteIndices.Add(inds);
                }

                // Calculate total vertex count for vertex-baked groups
                for(int i=0; i<objsToWrite.Count; i++)
                {
                    var lmgroup = objsToWriteGroup[i];
                    if (lmgroup == null || lmgroup.mode != BakeryLightmapGroup.ftLMGroupMode.Vertex) continue;
                    lmgroup.totalVertexCount += objsToWrite[i].GetComponent<MeshFilter>().sharedMesh.vertexCount;
                }

                // Create implicit temp LMGroups.
                // If object is a part of prefab, and if UVs are not generated in Unity, group is only addded to the topmost object (aka holder).

                // Implicit groups are added on every static object without ftLMGroupSelector.
                // (Also init lmBounds and LMID as well)
                // if autoAtlas == false: new group for every holder.
                // if autoAtlas == true: single group for all holders (will be split later).
                for(int i=0; i<objsToWrite.Count; i++)
                {
                    if (!objsToWriteLightmapped[i]) continue; // skip objects with scaleInLM == 0
                    if (objsToWriteGroup[i] != null) continue; // skip if already has lightmap assigned
                    var obj = objsToWrite[i];

                    var holder = obj; // holder is object itself (packed individually)
                    var prefabParent = PrefabUtility.GetPrefabParent(obj) as GameObject;
                    if (prefabParent != null) // object is part of prefab
                    {
                        // unity doesn't generate non-overlapping UVs for the whole model, only submeshes
                        // // if importer == null, it's an actual prefab, not model <-- not really; importer points to mesh's prefab, not real
                        // importer of a mesh is always model asset
                        // importers of components never exist
                        // at least check the prefab type
                        var ptype = PrefabUtility.GetPrefabType(prefabParent);
                        if (ptype == PrefabType.ModelPrefab)
                        {
                            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj.GetComponent<MeshFilter>().sharedMesh)) as ModelImporter;

                            if (importer != null && !ModelUVsOverlap(importer, gstorage))
                            {
                                // find topmost asset parent
                                var t = prefabParent.transform;
                                while(t.parent != null) t = t.parent;
                                var assetTopMost = t.gameObject;

                                // find topmost scene instance parent
                                var g = obj;
                                var assetG = PrefabUtility.GetPrefabParent(g) as GameObject;
                                while(assetG != assetTopMost && g.transform.parent != null && assetG.transform.parent != null)
                                {
                                    var g2 = g.transform.parent.gameObject;
                                    var assetG2 = assetG.transform.parent.gameObject;

                                    if (PrefabUtility.GetPrefabParent(g2) != assetG2) break; // avoid using parents which don't belong to this model

                                    g = g2;
                                    assetG = assetG2;
                                }
                                var sceneTopMost = g;
                                holder = sceneTopMost; // holder is topmost model object (non-overlapped UVs)

                                int lodLevel;
                                if (objToLodLevel.TryGetValue(obj, out lodLevel)) holder = obj; // separated if used in LOD
                            }
                        }
                    }
                    else if (obj.name == "__ExportTerrain")
                    {
                        holder = obj.transform.parent.gameObject; // holder is terrain parent

                        int lodLevel;
                        if (objToLodLevel.TryGetValue(obj, out lodLevel)) holder = obj; // separated if used in LOD
                    }

                    if (!storages[sceneToID[holder.scene]].implicitGroupedObjects.Contains(holder))
                    {
                        BakeryLightmapGroup newGroup;
                        if (autoAtlas && autoAtlasGroups.Count > 0)
                        {
                            newGroup = autoAtlasGroups[0];
                        }
                        else
                        {
                            newGroup = ScriptableObject.CreateInstance<BakeryLightmapGroup>();

                            // Make sure first lightmap is always LM0, not LM1, if probes are used
                            int lmNum = storages[sceneToID[holder.scene]].implicitGroups.Count;
                            if (ftRenderLightmap.lightProbeMode == ftRenderLightmap.LightProbeMode.L1 && renderTextures && !atlasOnly) lmNum--;

                            newGroup.name = holder.scene.name + "_LM" + lmNum;
                            newGroup.isImplicit = true;
                            newGroup.resolution = 256;
                            newGroup.bitmask = 1;
                            newGroup.area = 0;
                            newGroup.mode = autoAtlas ? BakeryLightmapGroup.ftLMGroupMode.PackAtlas : BakeryLightmapGroup.ftLMGroupMode.OriginalUV;

                            newGroup.id = lmid;
                            groupList.Add(newGroup);
                            lmBounds.Add(new Bounds(new Vector3(0,0,0), new Vector3(0,0,0)));
                            lmid++;

                            if (autoAtlas)
                            {
                                autoAtlasGroups.Add(newGroup);
                                var rootNode = new AtlasNode();
                                rootNode.rc = new Rect(0, 0, 1, 1);
                                autoAtlasGroupRootNodes.Add(rootNode);
                            }
                        }
                        storages[sceneToID[holder.scene]].implicitGroupedObjects.Add(holder);

                        storages[sceneToID[holder.scene]].implicitGroups.Add(newGroup);
                        //Debug.LogError("Add "+(storages[sceneToID[holder.scene]].implicitGroups.Count-1)+" "+newGroup.name);

                        tempStorage.implicitGroupMap[holder] = newGroup;
                        if (splitByScene) newGroup.sceneName = holder.scene.name;
                    }

                    if (!tempStorage.implicitGroupMap.ContainsKey(holder))
                    {
                        // happens with modifyLightmapStorage == false
                        var gholders = storages[sceneToID[holder.scene]].implicitGroupedObjects;
                        var grs = storages[sceneToID[holder.scene]].implicitGroups;
                        for(int g=0; g<gholders.Count; g++)
                        {
                            if (gholders[g] == holder)
                            {
                                tempStorage.implicitGroupMap[holder] = grs[g];
                                break;
                            }
                        }
                    }

                    objsToWriteGroup[i] = (BakeryLightmapGroup)tempStorage.implicitGroupMap[holder];
                    objsToWriteHolder[i] = holder;
                }


                // Transform vertices to world space
                for(int i=0; i<objsToWrite.Count; i++)
                {
                    var obj = objsToWrite[i];
                    var lmgroup = objsToWriteGroup[i];
                    var m = obj.GetComponent<MeshFilter>().sharedMesh;
                    var vertices = m.vertices;
                    var tform = obj.transform;
                    objsToWriteVerticesPosW.Add(new Vector3[vertices.Length]);
                    for(int t=0; t<vertices.Length; t++)
                    {
                        objsToWriteVerticesPosW[i][t] = tform.TransformPoint(vertices[t]);
                    }
                    var normals = m.normals;
                    objsToWriteVerticesNormalW.Add(new Vector3[vertices.Length]);
                    var nbuff = objsToWriteVerticesNormalW[i];
                    var localScale = obj.transform.localScale;
                    bool flipX = localScale.x < 0;
                    bool flipY = localScale.y < 0;
                    bool flipZ = localScale.z < 0;
                    for(int t=0; t<vertices.Length; t++)
                    {
                        if (normals.Length == 0)
                        {
                            nbuff[t] = Vector3.up;
                        }
                        else
                        {
                            nbuff[t] = normals[t];
                            if (flipX) nbuff[t].x *= -1;
                            if (flipY) nbuff[t].y *= -1;
                            if (flipZ) nbuff[t].z *= -1;
                            nbuff[t] = tform.TransformDirection(nbuff[t]);
                        }
                    }
                    if (NeedsTangents(lmgroup, tangentSHLights))
                    {
                        var tangents = m.tangents;
                        while(objsToWriteVerticesTangentW.Count < i) objsToWriteVerticesTangentW.Add(null);
                        objsToWriteVerticesTangentW.Add(new Vector4[vertices.Length]);
                        var tbuff = objsToWriteVerticesTangentW[i];
                        Vector3 tangent = Vector3.zero;
                        for(int t=0; t<vertices.Length; t++)
                        {
                            if (tangents.Length == 0)
                            {
                                tbuff[t] = Vector3.right;
                            }
                            else
                            {
                                tangent.Set(flipX ? -tangents[t].x : tangents[t].x,
                                            flipY ? -tangents[t].y : tangents[t].y,
                                            flipZ ? -tangents[t].z : tangents[t].z);
                                tangent = tform.TransformDirection(tangent);
                                tbuff[t] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[t].w);
                            }
                        }
                    }
                }


                if (unwrapUVs)
                {
                    // Calculate every implicit mesh area and convert to proper padding value
                    var dirtyAssetList = new List<string>();
                    var dirtyObjList = new List<int>();
                    var meshToPaddingMap = new Dictionary<Mesh, int>();
                    var meshToObjIDs = new Dictionary<Mesh, List<int>>();

                    var explicitGroupTotalArea = new Dictionary<int, float>();
                    var objsWithExplicitGroupPadding = new List<int>();
                    var objsWithExplicitGroupPaddingWidth = new List<float>();

                    for(int i=0; i<objsToWrite.Count; i++)
                    {
                        var lmgroup = objsToWriteGroup[i];
                        if (lmgroup == null) continue;
                        //if (!lmgroup.isImplicit) continue;
                        var prefabParent = PrefabUtility.GetPrefabParent(objsToWrite[i]) as GameObject;
                        if (prefabParent == null) continue;
                        var assetPath = AssetDatabase.GetAssetPath(objsToWrite[i].GetComponent<MeshFilter>().sharedMesh);
                        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                        if (importer == null || !importer.generateSecondaryUV) continue;
                        // user doesn't care much about UVs - adjust

                        var m = objsToWrite[i].GetComponent<MeshFilter>().sharedMesh;
                        var vpos = objsToWriteVerticesPosW[i];
                        float area = 0;
                        var inds = objsToWriteIndices[i];
                        for(int k=0;k<m.subMeshCount;k++) {
                            var indices = inds[k];// m.GetTriangles(k);
                            int indexA, indexB, indexC;
                            for(int j=0;j<indices.Length;j+=3)
                            {
                                indexA = indices[j];
                                indexB = indices[j + 1];
                                indexC = indices[j + 2];

                                var v1 = vpos[indexA];
                                var v2 = vpos[indexB];
                                var v3 = vpos[indexC];
                                area += Vector3.Cross(v2 - v1, v3 - v1).magnitude;
                            }
                        }
                        var so = new SerializedObject(objsToWrite[i].GetComponent<MeshRenderer>());
                        var scaleInLm = so.FindProperty("m_ScaleInLightmap").floatValue;
                        area *= scaleInLm;

                        float width = Mathf.Sqrt(area);
                        float twidth = 1;
                        if (lmgroup.isImplicit)
                        {
                            twidth = width * texelsPerUnit;
                        }
                        else
                        {
                            float currentArea;
                            if (!explicitGroupTotalArea.TryGetValue(lmgroup.id, out currentArea)) currentArea = 0;
                            explicitGroupTotalArea[lmgroup.id] = currentArea + area;

                            var holder = objsToWriteHolder[i];
                            BakeryLightmapGroupSelector comp = null;
                            if (holder != null) comp = holder.GetComponent<BakeryLightmapGroupSelector>();
                            if (comp != null && comp.instanceResolutionOverride)
                            {
                                // Explicit holder size
                                twidth = width * comp.instanceResolution;
                            }
                            else
                            {
                                // Texel size in atlas - can't calculate at this point
                                objsWithExplicitGroupPadding.Add(i);
                                objsWithExplicitGroupPaddingWidth.Add(width);
                                continue;
                            }
                        }
                        float requiredPadding = 4 * (1024.0f / (twidth * smallestMapScale));
                        int requiredPaddingClamped = (int)Mathf.Clamp(requiredPadding, 1, 256);

                        int existingPadding = 0;
                        meshToPaddingMap.TryGetValue(m, out existingPadding);
                        meshToPaddingMap[m] = Math.Max(requiredPaddingClamped, existingPadding); // select largest padding among instances

                        List<int> arr;
                        if (!meshToObjIDs.TryGetValue(m, out arr))
                        {
                            meshToObjIDs[m] = arr = new List<int>();
                        }
                        if (!arr.Contains(i)) arr.Add(i);
                    }

                    for(int j=0; j<objsWithExplicitGroupPadding.Count; j++)
                    {
                        int i = objsWithExplicitGroupPadding[j];
                        float width = objsWithExplicitGroupPaddingWidth[j];
                        var lmgroup = objsToWriteGroup[i];
                        float totalArea = explicitGroupTotalArea[lmgroup.id];
                        float twidth = (width / Mathf.Sqrt(totalArea)) * lmgroup.resolution;
                        var m = objsToWrite[i].GetComponent<MeshFilter>().sharedMesh;

                        // Following is copy-pasted from the loop above
                        float requiredPadding = 4 * (1024.0f / (twidth * smallestMapScale));
                        int requiredPaddingClamped = (int)Mathf.Clamp(requiredPadding, 1, 256);

                        int existingPadding = 0;
                        meshToPaddingMap.TryGetValue(m, out existingPadding);
                        meshToPaddingMap[m] = Math.Max(requiredPaddingClamped, existingPadding); // select largest padding among instances

                        List<int> arr;
                        if (!meshToObjIDs.TryGetValue(m, out arr))
                        {
                            meshToObjIDs[m] = arr = new List<int>();
                        }
                        if (!arr.Contains(i)) arr.Add(i);
                    }

                    // Reset scene padding backup
                    for(int s=0; s<sceneCount; s++)
                    {
                        var str = storages[s];
                        if (str == null) continue;
                        str.modifiedAssetPathList = new List<string>();
                        str.modifiedAssets = new List<ftGlobalStorage.AdjustedMesh>();
                    }

                    foreach(var pair in meshToPaddingMap)
                    {
                        var m = pair.Key;
                        var requiredPaddingClamped = pair.Value;
                        var assetPath = AssetDatabase.GetAssetPath(m);

                        var ids = meshToObjIDs[m];

                        //for(int s=0; s<sceneCount; s++)
                        {
                            var objStorage = gstorage;// == null ? storages[0] : gstorage;// storages[s];
                            int mstoreIndex = objStorage.modifiedAssetPathList.IndexOf(assetPath);
                            int ind = -1;
                            var mname = m.name;
                            if (mstoreIndex >= 0) ind = objStorage.modifiedAssets[mstoreIndex].meshName.IndexOf(mname);
                            if (ind < 0)
                            {
                                if (mstoreIndex < 0)
                                {
                                    // add new record to globalstorage
                                    objStorage.modifiedAssetPathList.Add(assetPath);
                                    var newStruct = new ftGlobalStorage.AdjustedMesh();
                                    newStruct.meshName = new List<string>();
                                    newStruct.padding = new List<int>();
                                    objStorage.modifiedAssets.Add(newStruct);
                                    mstoreIndex = objStorage.modifiedAssets.Count - 1;
                                }

                                var nameList = objStorage.modifiedAssets[mstoreIndex].meshName;
                                var paddingList = objStorage.modifiedAssets[mstoreIndex].padding;
                                var unwrapperList = objStorage.modifiedAssets[mstoreIndex].unwrapper;
                                if (unwrapperList == null)
                                {
                                    var s = objStorage.modifiedAssets[mstoreIndex];
                                    unwrapperList = s.unwrapper = new List<int>();
                                    objStorage.modifiedAssets[mstoreIndex] = s;
                                }
                                while(nameList.Count > unwrapperList.Count) unwrapperList.Add(0); // fix legacy

                                nameList.Add(mname);
                                paddingList.Add(requiredPaddingClamped);
                                unwrapperList.Add((int)ftRenderLightmap.unwrapper);

                                if (!dirtyAssetList.Contains(assetPath)) dirtyAssetList.Add(assetPath);
                                for(int xx=0; xx<ids.Count; xx++) dirtyObjList.Add(ids[xx]);
#if UNITY_2017_1_OR_NEWER
                                objStorage.SyncModifiedAsset(mstoreIndex);
#endif
                            }
                            else
                            {
                                var nameList = objStorage.modifiedAssets[mstoreIndex].meshName;
                                var paddingList = objStorage.modifiedAssets[mstoreIndex].padding;
                                var unwrapperList = objStorage.modifiedAssets[mstoreIndex].unwrapper;
                                if (unwrapperList == null)
                                {
                                    var s = objStorage.modifiedAssets[mstoreIndex];
                                    unwrapperList = s.unwrapper = new List<int>();
                                    objStorage.modifiedAssets[mstoreIndex] = s;
                                }
                                while(nameList.Count > unwrapperList.Count) unwrapperList.Add(0); // fix legacy

                                // modify existing record
                                var oldValue = paddingList[ind];
                                var oldUnwrapperValue = (ftGlobalStorage.Unwrapper)unwrapperList[ind];
                                bool shouldModify = oldValue != requiredPaddingClamped;
                                if (uvPaddingMax)
                                {
                                    shouldModify = oldValue < requiredPaddingClamped;
                                }
                                if (oldUnwrapperValue != ftRenderLightmap.unwrapper) shouldModify = true;
                                if (shouldModify)
                                {
                                    if (!dirtyAssetList.Contains(assetPath)) dirtyAssetList.Add(assetPath);
                                    for(int xx=0; xx<ids.Count; xx++) dirtyObjList.Add(ids[xx]);
                                    paddingList[ind] = requiredPaddingClamped;
                                    unwrapperList[ind] = (int)ftRenderLightmap.unwrapper;
#if UNITY_2017_1_OR_NEWER
                                    objStorage.SyncModifiedAsset(mstoreIndex);
#endif
                                }
                            }

                            // Backup padding storage to scene
                            for(int s=0; s<sceneCount; s++)
                            {
                                var str = storages[s];
                                if (str == null) continue;
                                var localIndex = str.modifiedAssetPathList.IndexOf(assetPath);
                                if (localIndex < 0)
                                {
                                    str.modifiedAssetPathList.Add(assetPath);
                                    str.modifiedAssets.Add(objStorage.modifiedAssets[mstoreIndex]);
                                }
                                else
                                {
                                    str.modifiedAssets[localIndex] = objStorage.modifiedAssets[mstoreIndex];
                                }
                            }
                        }
                    }

                    EditorUtility.SetDirty(gstorage);

                    // Reimport assets with adjusted padding
                    if (modifyLightmapStorage)
                    {
                        for(int i=0; i<dirtyAssetList.Count; i++)
                        {
                            var assetPath = dirtyAssetList[i];
                            Debug.Log("Reimport " + assetPath);
                            ProgressBarShow("Exporting scene - adjusting UV padding for " + assetPath + "...", 0);
                            //AssetDatabase.ImportAsset(assetPath);
                            (AssetImporter.GetAtPath(assetPath) as ModelImporter).SaveAndReimport();
                        }
                    }


                    // Transform modified vertices to world space again
                    for(int d=0; d<dirtyObjList.Count; d++)
                    {
                        int i = dirtyObjList[d];
                        var obj = objsToWrite[i];
                        var lmgroup = objsToWriteGroup[i];
                        var m = obj.GetComponent<MeshFilter>().sharedMesh;

                        // Refresh attributes and indices after reimport
                        objsToWriteVerticesUV[i] = m.uv;
                        objsToWriteVerticesUV2[i] = m.uv2;
                        var inds = new int[m.subMeshCount][];
                        for(int n=0; n<inds.Length; n++) inds[n] = m.GetTriangles(n);
                        objsToWriteIndices[i] = inds;

                        var vertices = m.vertices;
                        var tform = obj.transform;
                        objsToWriteVerticesPosW[i] = new Vector3[vertices.Length];
                        for(int t=0; t<vertices.Length; t++)
                        {
                            objsToWriteVerticesPosW[i][t] = tform.TransformPoint(vertices[t]);
                        }
                        var normals = m.normals;
                        var localScale = obj.transform.localScale;
                        bool flipX = localScale.x < 0;
                        bool flipY = localScale.y < 0;
                        bool flipZ = localScale.z < 0;
                        objsToWriteVerticesNormalW[i] = new Vector3[vertices.Length];
                        var nbuff = objsToWriteVerticesNormalW[i];
                        for(int t=0; t<vertices.Length; t++)
                        {
                            if (normals.Length == 0)
                            {
                                nbuff[t] = Vector3.up;
                            }
                            else
                            {
                                nbuff[t] = normals[t];
                                if (flipX) nbuff[t].x *= -1;
                                if (flipY) nbuff[t].y *= -1;
                                if (flipZ) nbuff[t].z *= -1;
                                nbuff[t] = tform.TransformDirection(nbuff[t]);
                            }
                        }
                        if (NeedsTangents(lmgroup, tangentSHLights))
                        {
                            var tangents = m.tangents;
                            objsToWriteVerticesTangentW[i] = new Vector4[vertices.Length];
                            var tbuff = objsToWriteVerticesTangentW[i];
                            Vector3 tangent = Vector3.zero;
                            for(int t=0; t<vertices.Length; t++)
                            {
                                if (tangents.Length == 0)
                                {
                                    tbuff[t] = Vector3.right;
                                }
                                else
                                {
                                    tangent.Set(flipX ? -tangents[t].x : tangents[t].x,
                                                flipY ? -tangents[t].y : tangents[t].y,
                                                flipZ ? -tangents[t].z : tangents[t].z);
                                    tangent = tform.TransformDirection(tangent);
                                    tbuff[t] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[t].w);
                                }
                            }
                        }
                    }

                }


                // Data to collect for atlas packing
                var holderObjArea = new Dictionary<GameObject, float>(); // LMGroup holder area, accumulated from all children
                var holderObjUVBounds = new Dictionary<GameObject, Vector4>(); // LMGroup holder 2D UV AABB
                var groupToHolderObjects = new Dictionary<BakeryLightmapGroup, List<GameObject>>(); // LMGroup -> holders map

                // Calculate implicit group / atlas packing data
                for(int i=0; i<objsToWrite.Count; i++)
                {
                    var obj = objsToWrite[i];
                    var lmgroup = objsToWriteGroup[i];
                    var calculateArea = lmgroup == null ? false : (lmgroup.isImplicit || lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.PackAtlas);
                    if (!calculateArea) continue;

                    var holderObj = objsToWriteHolder[i];
                    var m = obj.GetComponent<MeshFilter>().sharedMesh;
                    var mr = obj.GetComponent<MeshRenderer>();

                    var vpos = objsToWriteVerticesPosW[i];
                    var vuv = objsToWriteVerticesUV2[i];//m.uv2;
                    var inds = objsToWriteIndices[i];
                    //if (vuv.Length == 0 || obj.GetComponent<BakeryLightMesh>()!=null) vuv = objsToWriteVerticesUV[i];//m.uv; // area lights or objects without UV2 export UV1 instead
                    if (vuv.Length == 0 || obj.GetComponent<BakeryLightMesh>()!=null || temporaryAreaLightMeshList.Contains(obj)) vuv = objsToWriteVerticesUV[i];//m.uv; // area lights or objects without UV2 export UV1 instead
                    Vector2 uv1 = Vector2.zero;
                    Vector2 uv2 = Vector2.zero;
                    Vector2 uv3 = Vector2.zero;

                    int lodLevel;
                    if (!objToLodLevel.TryGetValue(obj, out lodLevel)) lodLevel = -1;

                    for(int k=0;k<m.subMeshCount;k++) {

                        var indices = inds[k];//m.GetTriangles(k);
                        int indexA, indexB, indexC;
                        float area = 0;
                        float areaUV = 0;
                        Vector4 uvBounds = new Vector4(1,1,0,0); // minx, miny, maxx, maxy

                        for(int j=0;j<indices.Length;j+=3)
                        {
                            indexA = indices[j];
                            indexB = indices[j + 1];
                            indexC = indices[j + 2];

                            var v1 = vpos[indexA];
                            var v2 = vpos[indexB];
                            var v3 = vpos[indexC];
                            area += Vector3.Cross(v2 - v1, v3 - v1).magnitude;

                            if (vuv.Length > 0)
                            {
                                uv1 = vuv[indexA];
                                uv2 = vuv[indexB];
                                uv3 = vuv[indexC];
                            }

                            var uv31 = new Vector3(uv1.x, uv1.y, 0);
                            var uv32 = new Vector3(uv2.x, uv2.y, 0);
                            var uv33 = new Vector3(uv3.x, uv3.y, 0);
                            areaUV += Vector3.Cross(uv32 - uv31, uv33 - uv31).magnitude;

                            if (uv1.x < uvBounds.x) uvBounds.x = uv1.x;
                            if (uv1.y < uvBounds.y) uvBounds.y = uv1.y;
                            if (uv1.x > uvBounds.z) uvBounds.z = uv1.x;
                            if (uv1.y > uvBounds.w) uvBounds.w = uv1.y;

                            if (uv2.x < uvBounds.x) uvBounds.x = uv2.x;
                            if (uv2.y < uvBounds.y) uvBounds.y = uv2.y;
                            if (uv2.x > uvBounds.z) uvBounds.z = uv2.x;
                            if (uv2.y > uvBounds.w) uvBounds.w = uv2.y;

                            if (uv3.x < uvBounds.x) uvBounds.x = uv3.x;
                            if (uv3.y < uvBounds.y) uvBounds.y = uv3.y;
                            if (uv3.x > uvBounds.z) uvBounds.z = uv3.x;
                            if (uv3.y > uvBounds.w) uvBounds.w = uv3.y;
                        }

                        // uv layouts always have empty spaces
                        //area /= areaUV;

                        var so = new SerializedObject(mr);
                        var scaleInLm = so.FindProperty("m_ScaleInLightmap").floatValue;

                        area *= scaleInLm;

                        if (lmgroup.isImplicit && lodLevel == -1)
                        {
                            lmgroup.area += area; // accumulate LMGroup area
                            // only use base scene values, no LODs, to properly initialize autoatlas size
                        }
                        if (lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.PackAtlas)
                        {
                            // Accumulate per-holder area and UV bounds
                            float existingArea;
                            Vector4 existingBounds;
                            holderObjUVBounds.TryGetValue(holderObj, out existingBounds);
                            if (!holderObjArea.TryGetValue(holderObj, out existingArea))
                            {
                                existingArea = 0;
                                existingBounds = uvBounds;
                                List<GameObject> holderList;
                                if (!groupToHolderObjects.TryGetValue(lmgroup, out holderList))
                                {
                                    groupToHolderObjects[lmgroup] = holderList = new List<GameObject>();
                                }
                                holderList.Add(holderObj);
                            }
                            holderObjArea[holderObj] = existingArea + area;

                            existingBounds.x = existingBounds.x < uvBounds.x ? existingBounds.x : uvBounds.x;
                            existingBounds.y = existingBounds.y < uvBounds.y ? existingBounds.y : uvBounds.y;
                            existingBounds.z = existingBounds.z > uvBounds.z ? existingBounds.z : uvBounds.z;
                            existingBounds.w = existingBounds.w > uvBounds.w ? existingBounds.w : uvBounds.w;
                            holderObjUVBounds[holderObj] = existingBounds;
                        }
                    }
                }

                // Calculate implicit lightmap resolution
                for(int i=0; i<groupList.Count; i++)
                {
                    var lmgroup = groupList[i];
                    if (lmgroup.isImplicit)
                    {
                        lmgroup.resolution = (int)(Mathf.Sqrt(lmgroup.area) * texelsPerUnit);
                        if (mustBePOT)
                        {
                            if (atlasCountPriority)
                            {
                                lmgroup.resolution = Mathf.NextPowerOfTwo(lmgroup.resolution);
                            }
                            else
                            {
                                lmgroup.resolution = Mathf.ClosestPowerOfTwo(lmgroup.resolution);
                            }
                        }
                        lmgroup.resolution = Math.Max(lmgroup.resolution, minAutoResolution);
                        lmgroup.resolution = Math.Min(lmgroup.resolution, maxAutoResolution);
                    }
                }

                // Pack atlases
                // Try to scale all objects to occupy all atlas space
                foreach(var pair in groupToHolderObjects)
                {
                    // For every LMGroup with PackAtlas mode
                    var lmgroup = pair.Key;
                    var holderObjs = pair.Value; // get all objects

                    // Divide holders area to get from world space to -> UV space
                    float areaMult = 1.0f;
                    //float lightmapMaxArea = ((lmgroup.resolution/texelsPerUnit) * (lmgroup.resolution/texelsPerUnit));
                    //float lightmapMaxLength = lmgroup.resolution/texelsPerUnit;
                    if (lmgroup.isImplicit && lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.PackAtlas)
                    {
                        // ...by maximum lightmap area given texel size (autoAtlas)
                        //areaMult = 1.0f / lightmapMaxArea;
                        // don't modify
                    }
                    else
                    {
                        // ... by maximum holder area (normalize)
                        float lmgroupArea = 0;
                        for(int i=0; i<holderObjs.Count; i++)
                        {
                            // space outside of UV bounds shouldn't affect area
                            var uvbounds = holderObjUVBounds[holderObjs[i]];
                            var width = uvbounds.z - uvbounds.x;
                            var height = uvbounds.w - uvbounds.y;
                            float uvboundsArea = width * height;

                            lmgroupArea += holderObjArea[holderObjs[i]] * uvboundsArea;
                        }
                        areaMult = 1.0f / lmgroupArea;
                    }

                    // Perform the division and sum up total UV area
                    var remainingAreaPerLodLevel = new Dictionary<int,float>();
                    for(int i=0; i<holderObjs.Count; i++)
                    {
                        holderObjArea[holderObjs[i]] *= areaMult;

                        int lodLevel = -1;
                        if (!objToLodLevel.TryGetValue(holderObjs[i], out lodLevel)) lodLevel = -1;

                        float lodArea = 0;
                        if (!remainingAreaPerLodLevel.TryGetValue(lodLevel, out lodArea)) lodArea = 0;

                        remainingAreaPerLodLevel[lodLevel] = lodArea + holderObjArea[holderObjs[i]];
                    }

                    // Sort objects by area and scene LOD level
                    // + optionally by scene
                    // + split by terrain
                    holderObjs.Sort(delegate(GameObject a, GameObject b)
                    {
                        if (splitByScene)
                        {
                            if (a.scene.name != b.scene.name) return a.scene.name.CompareTo(b.scene.name);
                        }

                        if (ftRenderLightmap.giLodMode != ftRenderLightmap.GILODMode.ForceOff && exportTerrainAsHeightmap)
                        {
                            bool ba = a.name != "__ExportTerrainParent";
                            bool bb = b.name != "__ExportTerrainParent";
                            if (ba != bb) return ba.CompareTo(bb);
                        }

                        int lodLevelA = -1;
                        int lodLevelB = -1;
                        if (!objToLodLevel.TryGetValue(a, out lodLevelA)) lodLevelA = -1;
                        if (!objToLodLevel.TryGetValue(b, out lodLevelB)) lodLevelB = -1;

                        if (lodLevelA != lodLevelB) return lodLevelA.CompareTo(lodLevelB);

                        return holderObjArea[b].CompareTo( holderObjArea[a] );
                    });

                    //for(int i=0; i<holderObjs.Count; i++) Debug.LogError(holderObjs[i].name);

                    // Perform recursive packing
                    bool repack = true;
                    bool repackStage2 = false;
                    bool finalRepack = false;
                    float repackScale = 1;
                    int repackTries = 0;
                    bool hasResOverrides = false;
                    while(repack)
                    {
                        //Debug.LogError("repack: "+repackScale);
                        repack = false;
                        AtlasNode rootNode;

                        if (lmgroup.isImplicit && lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.PackAtlas && autoAtlasGroupRootNodes != null && autoAtlasGroupRootNodes.Count > 0)
                        {
                            rootNode = autoAtlasGroupRootNodes[0];
                        }
                        else
                        {
                            rootNode = new AtlasNode();
                        }

                        rootNode.rc = new Rect(0, 0, 1, 1);
                        for(int i=0; i<holderObjs.Count; i++)
                        {
                            var area = holderObjArea[holderObjs[i]];
                            var uvbounds = holderObjUVBounds[holderObjs[i]];

                            // Calculate width and height of each holder in atlas UV space
                            float width, height;
                            var comp = holderObjs[i].GetComponent<BakeryLightmapGroupSelector>();
                            if (comp != null && comp.instanceResolutionOverride)
                            {
                                // Explicit holder size
                                hasResOverrides = true;
                                width = height = comp.instanceResolution / (float)lmgroup.resolution;
                            }
                            else
                            {
                                // Automatic: width and height = sqrt(area) transformed by UV AABB aspect ratio
                                width = height = Mathf.Sqrt(area);
                                float uwidth = uvbounds.z - uvbounds.x;
                                float uheight = uvbounds.w - uvbounds.y;
                                if (uwidth == 0 && uheight == 0)
                                {
                                    width = height = 0;
                                }
                                else
                                {
                                    float uvratio = uheight / uwidth;
                                    if (uvratio <= 1.0f)
                                    {
                                        width /= uvratio;
                                        //height *= uvratio;
                                    }
                                    else
                                    {
                                        height *= uvratio;
                                        //width /= uvratio;
                                    }
                                }
                            }
                            // Clamp to full lightmap size
                            float twidth = width;
                            float theight = height;
                            if (lmgroup.isImplicit && lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.PackAtlas)
                            {
                                twidth = (width * texelsPerUnit) / lmgroup.resolution;
                                theight = (height * texelsPerUnit) / lmgroup.resolution;
                            }
                            //float unclampedTwidth = twidth;
                            //float unclampedTheight = twidth;
                            if (comp != null && comp.instanceResolutionOverride)
                            {
                            }
                            else
                            {
                                twidth *= repackScale;
                                theight *= repackScale;
                            }
                            twidth = twidth > 1 ? 1 : twidth;
                            theight = theight > 1 ? 1 : theight;
                            twidth = Mathf.Max(twidth, 1.0f / lmgroup.resolution);
                            theight = Mathf.Max(theight, 1.0f / lmgroup.resolution);
                            var rect = new Rect(0, 0, twidth, theight);

                            if (float.IsNaN(twidth) || float.IsNaN(theight))
                            {
                                DebugLogError("NaN UVs detected for " + holderObjs[i].name+" "+rect.width+" "+rect.height+" "+width+" "+height+" "+lmgroup.resolution+" "+area+" "+(uvbounds.z - uvbounds.x)+" "+(uvbounds.w - uvbounds.y));
                                CloseAllFiles();
                                userCanceled = true;
                                ProgressBarEnd();
                                yield break;
                            }

                            // Try inserting this rect
                            // Break autoatlas if lod level changes
                            // Optionally break autoatlas if scene changes
                            AtlasNode node = null;
                            int lodLevel;
                            if (!objToLodLevel.TryGetValue(holderObjs[i], out lodLevel)) lodLevel = -1;
                            bool splitAtlas = false;
                            if (splitByScene)
                            {
                                if (holderObjs[i].scene.name != lmgroup.sceneName)
                                {
                                    splitAtlas = true;
                                }
                            }
                            if (ftRenderLightmap.giLodMode != ftRenderLightmap.GILODMode.ForceOff && exportTerrainAsHeightmap)
                            {
                                bool ba = holderObjs[i].name == "__ExportTerrainParent";
                                if (ba) lmgroup.containsTerrains = true;

                                if (i > 0)
                                {
                                    bool bb = holderObjs[i-1].name == "__ExportTerrainParent";
                                    if (ba != bb)
                                    {
                                        splitAtlas = true;
                                    }
                                }
                            }
                            if (!splitAtlas)
                            {
                                if (lmgroup.isImplicit && lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.PackAtlas)
                                {
                                    if (lodLevel == lmgroup.sceneLodLevel)
                                    {
                                        node = rootNode.Insert(holderObjs[i], rect);
                                    }
                                }
                                else
                                {
                                    node = rootNode.Insert(holderObjs[i], rect);
                                }
                            }

                            /*if (node!=null)
                            {
                                Debug.Log(holderObjs[i].name+" goes straight into "+lmgroup.name);
                            }*/

                            if (node == null)
                            {
                                if (lmgroup.isImplicit && lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.PackAtlas)
                                {
                                    // Can't fit - try other autoAtlas lightmaps
                                    BakeryLightmapGroup newGroup = null;
                                    var holder = holderObjs[i];
                                    int goodGroup = -1;
                                    for(int g=1; g<autoAtlasGroups.Count; g++)
                                    {
                                        if (splitByScene)
                                        {
                                            if (autoAtlasGroups[g].sceneName != holderObjs[i].scene.name) continue;
                                        }
                                        if (ftRenderLightmap.giLodMode != ftRenderLightmap.GILODMode.ForceOff && exportTerrainAsHeightmap)
                                        {
                                            bool ba = holderObjs[i].name != "__ExportTerrainParent";
                                            bool bb = !autoAtlasGroups[g].containsTerrains;
                                            if (ba != bb) continue;
                                        }
                                        if (autoAtlasGroups[g].sceneLodLevel != lodLevel) continue;
                                        twidth = (width * texelsPerUnit) / autoAtlasGroups[g].resolution;
                                        theight = (height * texelsPerUnit) / autoAtlasGroups[g].resolution;
                                        //unclampedTwidth = twidth;
                                        //unclampedTheight = twidth;
                                        twidth = twidth > 1 ? 1 : twidth;
                                        theight = theight > 1 ? 1 : theight;
                                        rect = new Rect(0, 0, twidth, theight);

                                        node = autoAtlasGroupRootNodes[g].Insert(holder, rect);
                                        if (node != null)
                                        {
                                            //Debug.Log(holder.name+" fits into "+autoAtlasGroups[g].name);
                                            newGroup = autoAtlasGroups[g];
                                            goodGroup = g;
                                            break;
                                        }
                                    }

                                    // Can't fit - create new lightmap (autoAtlas)
                                    if (goodGroup < 0)
                                    {
                                        newGroup = ScriptableObject.CreateInstance<BakeryLightmapGroup>();
                                        newGroup.name = holder.scene.name + "_LMA" + autoAtlasGroups.Count;
                                        newGroup.isImplicit = true;
                                        newGroup.sceneLodLevel = lodLevel;
                                        if (splitByScene) newGroup.sceneName = holderObjs[i].scene.name;
                                        //Debug.Log(holder.name+" creates "+newGroup.name);

                                        if (ftRenderLightmap.giLodMode != ftRenderLightmap.GILODMode.ForceOff && exportTerrainAsHeightmap)
                                        {
                                            newGroup.containsTerrains = holderObjs[i].name == "__ExportTerrainParent";
                                        }

                                        newGroup.resolution = (int)(Mathf.Sqrt(remainingAreaPerLodLevel[lodLevel]) * texelsPerUnit);
                                        if (mustBePOT)
                                        {
                                            if (atlasCountPriority)
                                            {
                                                newGroup.resolution = Mathf.NextPowerOfTwo(newGroup.resolution);
                                            }
                                            else
                                            {
                                                newGroup.resolution = Mathf.ClosestPowerOfTwo(newGroup.resolution);
                                            }
                                        }
                                        newGroup.resolution = Math.Max(newGroup.resolution, minAutoResolution);
                                        newGroup.resolution = Math.Min(newGroup.resolution, maxAutoResolution);

                                        newGroup.bitmask = 1;
                                        newGroup.area = 0;
                                        newGroup.mode = BakeryLightmapGroup.ftLMGroupMode.PackAtlas;

                                        newGroup.id = lmid;
                                        groupList.Add(newGroup);
                                        lmBounds.Add(new Bounds(new Vector3(0,0,0), new Vector3(0,0,0)));
                                        lmid++;

                                        autoAtlasGroups.Add(newGroup);
                                        var rootNode2 = new AtlasNode();
                                        rootNode2.rc = new Rect(0, 0, 1, 1);
                                        autoAtlasGroupRootNodes.Add(rootNode2);

                                        twidth = (width * texelsPerUnit) / newGroup.resolution;
                                        theight = (height * texelsPerUnit) / newGroup.resolution;
                                        //unclampedTwidth = twidth;
                                        //unclampedTheight = twidth;
                                        twidth = twidth > 1 ? 1 : twidth;
                                        theight = theight > 1 ? 1 : theight;

                                        rect = new Rect(0, 0, twidth, theight);

                                        node = rootNode2.Insert(holder, rect);
                                    }

                                    // Modify implicit group storage
                                    var scn = holder.scene;
                                    tempStorage.implicitGroupMap[holder] = newGroup;
                                    for(int k=0; k<storages[sceneToID[holder.scene]].implicitGroupedObjects.Count; k++)
                                    {
                                        if (storages[sceneToID[holder.scene]].implicitGroupedObjects[k] == holder)
                                        {
                                            storages[sceneToID[holder.scene]].implicitGroups[k] = newGroup;
                                            //Debug.LogError("Implicit set: " + k+" "+newGroup.name+" "+holder.name);
                                        }
                                    }
                                    //lmgroup = newGroup;
                                }
                                else
                                {
                                    /*
                                    // Can't fit - try shrinking
                                    int tries = 0;
                                    while(node == null && tries < 10)
                                    {
                                        rect.width *= 0.5f;
                                        rect.height *= 0.5f;
                                        node = rootNode.Insert(holderObjs[i], rect);
                                        tries++;
                                    }
                                    */
                                    if (!repackStage2)
                                    {
                                        // explicit packed atlas - can't fit - try shrinking the whole atlas
                                        repackTries++;
                                        if (repackTries < atlasMaxTries)
                                        {
                                            repack = true;
                                            repackScale *= 0.75f;
                                            //Debug.LogError("Can't fit, set " +repackScale);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        // explicit packed atlas - did fit, now trying to scale up, doesn't work - found optimal fit
                                        repack = true;
                                        repackScale /= atlasScaleUpValue;//*= 0.75f;
                                        //Debug.LogError("Final, set " +repackScale);
                                        finalRepack = true;
                                        repackTries = 0;
                                        break;
                                    }
                                }
                            }

                            if (node == null)
                            {
                                // No way to fit
                                DebugLogError("Can't fit " + holderObjs[i].name+" "+rect.width+" "+rect.height);
                                CloseAllFiles();
                                userCanceled = true;
                                ProgressBarEnd();
                                yield break;
                            }
                            else
                            {
                                // Generate final rectangle to transform local UV -> atlas UV
                                float padding = ((float)atlasPaddingPixels) / lmgroup.resolution;

                                var paddedRc = new Rect(node.rc.x + padding,
                                                        node.rc.y + padding,
                                                        node.rc.width - padding * 2,
                                                        node.rc.height - padding * 2);

                                paddedRc.x -= uvbounds.x * (paddedRc.width / (uvbounds.z - uvbounds.x));
                                paddedRc.y -= uvbounds.y * (paddedRc.height / (uvbounds.w - uvbounds.y));
                                paddedRc.width /= uvbounds.z - uvbounds.x;
                                paddedRc.height /= uvbounds.w - uvbounds.y;

                                holderRect[holderObjs[i]] = paddedRc;
                            }

                            //float areaReduction = (twidth*theight) / (unclampedTwidth*unclampedTheight);
                            remainingAreaPerLodLevel[lodLevel] -= area;// * areaReduction;
                        }

                        if (!lmgroup.isImplicit && lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.PackAtlas)
                        {
                            if (finalRepack && repack) continue;
                            if (finalRepack) break;

                            if (!repack && !repackStage2)
                            {
                                //if (repackTries > 0) break; // shrinked down just now - don't scale up

                                repackStage2 = true; // scale up now
                                repack = true;
                                repackScale *= atlasScaleUpValue;///= 0.75f;
                                repackTries = 0;
                                //Debug.LogError("Scale up, set " +repackScale);
                            }
                            else if (repackStage2)
                            {
                                repackTries++;
                                if (repackTries == atlasMaxTries) break;
                                repack = true;
                                repackScale *= atlasScaleUpValue;///= 0.75f;
                                //Debug.LogError("Scale up cont, set " +repackScale);
                            }
                        }
                    }

                    if (!lmgroup.isImplicit && lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.PackAtlas && !hasResOverrides)
                    {
                        // normalize atlas by largest axis
                        float maxx = 0;
                        float maxy = 0;
                        for(int i=0; i<holderObjs.Count; i++)
                        {
                            var rect = holderRect[holderObjs[i]];
                            if ((rect.x + rect.width) > maxx) maxx = rect.x + rect.width;
                            if ((rect.y + rect.height) > maxy) maxy = rect.y + rect.height;
                        }
                        float maxDimension = maxx > maxy ? maxx : maxy;
                        float normalizeScale = 1.0f / maxDimension;
                        for(int i=0; i<holderObjs.Count; i++)
                        {
                            var rect = holderRect[holderObjs[i]];
                            holderRect[holderObjs[i]] = new Rect(rect.x * normalizeScale, rect.y * normalizeScale, rect.width * normalizeScale, rect.height * normalizeScale);
                        }
                    }
                }

                // Normalize autoatlases
                var stack = new Stack<AtlasNode>();
                for(int g=0; g<autoAtlasGroups.Count; g++)
                {
                    var lmgroup = autoAtlasGroups[g];
                    var rootNode = autoAtlasGroupRootNodes[g];
                    float maxx = 0;
                    float maxy = 0;
                    rootNode.GetMax(ref maxx, ref maxy);
                    float maxDimension = maxx > maxy ? maxx : maxy;
                    float normalizeScale = 1.0f / maxDimension;
                    stack.Clear();
                    stack.Push(rootNode);
                    while(stack.Count > 0)
                    {
                        var node = stack.Pop();
                        if (node.obj != null)
                        {
                            var rect = holderRect[node.obj];
                            holderRect[node.obj] = new Rect(rect.x * normalizeScale, rect.y * normalizeScale, rect.width * normalizeScale, rect.height * normalizeScale);
                        }
                        if (node.child0 != null) stack.Push(node.child0);
                        if (node.child1 != null) stack.Push(node.child1);
                    }
                    if (maxDimension < 0.5f)
                    {
                        lmgroup.resolution /= 2; // shrink the lightmap after normalization if it was too empty
                        lmgroup.resolution = Math.Max(lmgroup.resolution, minAutoResolution);
                    }
                }

                // Join autoatlases
                var autoAtlasLodLevels = new List<int>();
                bool joined = false;
                //var oldLMGroupToNew = new Dictionary<BakeryLightmapGroup, BakeryLightmapGroup>();
                for(int g=0; g<autoAtlasGroups.Count; g++)
                {
                    if (!autoAtlasLodLevels.Contains(autoAtlasGroups[g].sceneLodLevel)) autoAtlasLodLevels.Add(autoAtlasGroups[g].sceneLodLevel);
                }
                for(int alod=0; alod<autoAtlasLodLevels.Count; alod++)
                {
                    int lodLevel = autoAtlasLodLevels[alod];
                    var autoAtlasSizes = new List<int>();
                    var atlasStack = new Stack<BakeryLightmapGroup>();
                    for(int g=0; g<autoAtlasGroups.Count; g++)
                    {
                        if (autoAtlasGroups[g].sceneLodLevel != lodLevel) continue;
                        if (autoAtlasGroups[g].resolution == maxAutoResolution) continue;
                        if (!autoAtlasSizes.Contains(autoAtlasGroups[g].resolution)) autoAtlasSizes.Add(autoAtlasGroups[g].resolution);
                    }
                    autoAtlasSizes.Sort();
                    for(int s=0; s<autoAtlasSizes.Count; s++)
                    {
                        int asize = autoAtlasSizes[s];
                        atlasStack.Clear();
                        for(int g=0; g<autoAtlasGroups.Count; g++)
                        {
                            if (autoAtlasGroups[g].sceneLodLevel != lodLevel) continue;
                            if (autoAtlasGroups[g].resolution != asize) continue;
                            atlasStack.Push(autoAtlasGroups[g]);
                            if (atlasStack.Count == 4)
                            {
                                var newGroup = ScriptableObject.CreateInstance<BakeryLightmapGroup>();
                                newGroup.name = autoAtlasGroups[g].name;
                                newGroup.isImplicit = true;
                                newGroup.sceneLodLevel = lodLevel;

                                newGroup.resolution = asize * 2;

                                newGroup.bitmask = autoAtlasGroups[g].bitmask;
                                newGroup.mode = BakeryLightmapGroup.ftLMGroupMode.PackAtlas;

                                newGroup.id = lmid;
                                groupList.Add(newGroup);
                                lmBounds.Add(new Bounds(new Vector3(0,0,0), new Vector3(0,0,0)));
                                lmid++;

                                autoAtlasGroups.Add(newGroup);
                                var rootNode2 = new AtlasNode();
                                rootNode2.rc = new Rect(0, 0, 1, 1);
                                autoAtlasGroupRootNodes.Add(rootNode2);

                                // Top
                                rootNode2.child0 = new AtlasNode();
                                rootNode2.child0.rc = new Rect(0, 0, 1, 0.5f);

                                // Bottom
                                rootNode2.child1 = new AtlasNode();
                                rootNode2.child1.rc = new Rect(0, 0.5f, 1, 0.5f);

                                for(int gg=0; gg<4; gg++)
                                {
                                    var subgroup = atlasStack.Pop();
                                    var id = autoAtlasGroups.IndexOf(subgroup);
                                    var subgroupRootNode = autoAtlasGroupRootNodes[id];
                                    float ox, oy, sx, sy;

                                    if (gg == 0)
                                    {
                                        // Left top
                                        rootNode2.child0.child0 = subgroupRootNode;
                                        //rootNode2.child0.child0.Transform(0, 0, 0.5f, 0.5f);
                                        //offsetScale = rootNode2.child0.child0.rc;
                                        ox = 0; oy = 0; sx = 0.5f; sy = 0.5f;
                                    }
                                    else if (gg == 1)
                                    {
                                        // Right top
                                        rootNode2.child0.child1 = subgroupRootNode;
                                        //rootNode2.child0.child1.Transform(0.5f, 0, 0.5f, 0.5f);
                                        //offsetScale = rootNode2.child0.child1.rc;
                                        ox = 0.5f; oy = 0; sx = 0.5f; sy = 0.5f;
                                    }
                                    else if (gg == 2)
                                    {
                                        // Left bottom
                                        rootNode2.child1.child0 = subgroupRootNode;
                                        //rootNode2.child1.child0.Transform(0, 0.5f, 0.5f, 0.5f);
                                        //offsetScale = rootNode2.child1.child0.rc;
                                        ox = 0; oy = 0.5f; sx = 0.5f; sy = 0.5f;
                                    }
                                    else
                                    {
                                        // Right bottom
                                        rootNode2.child1.child1 = subgroupRootNode;
                                        //rootNode2.child1.child1.Transform(0.5f, 0.5f, 0.5f, 0.5f);
                                        //offsetScale = rootNode2.child1.child1.rc;
                                        ox = 0.5f; oy = 0.5f; sx = 0.5f; sy = 0.5f;
                                    }

                                    autoAtlasGroups.RemoveAt(id);
                                    autoAtlasGroupRootNodes.RemoveAt(id);

                                    id = groupList.IndexOf(subgroup);
                                    groupList.RemoveAt(id);
                                    lmBounds.RemoveAt(id);

                                    for(int x=id; x<groupList.Count; x++)
                                    {
                                        groupList[x].id--;
                                        lmid--;
                                    }

                                    // Modify implicit group storage
                                    joined = true;
                                    stack.Clear();
                                    stack.Push(subgroupRootNode);
                                    while(stack.Count > 0)
                                    {
                                        var node = stack.Pop();
                                        if (node.obj != null)
                                        {
                                            var rect = holderRect[node.obj];
                                            holderRect[node.obj] = new Rect(rect.x * sx + ox,
                                                                            rect.y * sy + oy,
                                                                            rect.width * sx,
                                                                            rect.height * sy);

                                            tempStorage.implicitGroupMap[node.obj] = newGroup;
                                            for(int k=0; k<storages[sceneToID[node.obj.scene]].implicitGroupedObjects.Count; k++)
                                            {
                                                if (storages[sceneToID[node.obj.scene]].implicitGroupedObjects[k] == node.obj)
                                                {
                                                    storages[sceneToID[node.obj.scene]].implicitGroups[k] = newGroup;
                                                    //Debug.LogError("Implicit set (join): " + k+" "+newGroup.name);
                                                }
                                            }
                                        }
                                        if (node.child0 != null) stack.Push(node.child0);
                                        if (node.child1 != null) stack.Push(node.child1);
                                    }
                                }
                            }
                        }
                    }
                }
                if (joined)
                {
                    for(int i=0; i<objsToWrite.Count; i++)
                    {
                        objsToWriteGroup[i] = GetLMGroupFromObject(objsToWrite[i]);
                    }
                }

                // Re-transform vertices to world space
                if (unwrapUVs)
                {
                    objsToWriteVerticesPosW = new List<Vector3[]>();
                    objsToWriteVerticesNormalW = new List<Vector3[]>();
                    objsToWriteVerticesTangentW = new List<Vector4[]>();
                    for(int i=0; i<objsToWrite.Count; i++)
                    {
                        var obj = objsToWrite[i];
                        var lmgroup = objsToWriteGroup[i];
                        var m = obj.GetComponent<MeshFilter>().sharedMesh;
                        var vertices = m.vertices;
                        var tform = obj.transform;
                        objsToWriteVerticesPosW.Add(new Vector3[vertices.Length]);
                        for(int t=0; t<vertices.Length; t++)
                        {
                            objsToWriteVerticesPosW[i][t] = tform.TransformPoint(vertices[t]);
                        }
                        var normals = m.normals;
                        objsToWriteVerticesNormalW.Add(new Vector3[vertices.Length]);
                        var nbuff = objsToWriteVerticesNormalW[i];
                        var localScale = obj.transform.localScale;
                        bool flipX = localScale.x < 0;
                        bool flipY = localScale.y < 0;
                        bool flipZ = localScale.z < 0;
                        for(int t=0; t<vertices.Length; t++)
                        {
                            if (normals.Length == 0)
                            {
                                nbuff[t] = Vector3.up;
                            }
                            else
                            {
                                nbuff[t] = normals[t];
                                if (flipX) nbuff[t].x *= -1;
                                if (flipY) nbuff[t].y *= -1;
                                if (flipZ) nbuff[t].z *= -1;
                                nbuff[t] = tform.TransformDirection(nbuff[t]);
                            }
                        }
                        if (NeedsTangents(lmgroup, tangentSHLights))
                        {
                            var tangents = m.tangents;
                            while(objsToWriteVerticesTangentW.Count < i) objsToWriteVerticesTangentW.Add(null);
                            objsToWriteVerticesTangentW.Add(new Vector4[vertices.Length]);
                            var tbuff = objsToWriteVerticesTangentW[i];
                            Vector3 tangent = Vector3.zero;
                            for(int t=0; t<vertices.Length; t++)
                            {
                                if (tangents.Length == 0)
                                {
                                    tbuff[t] = Vector3.right;
                                }
                                else
                                {
                                    tangent.Set(flipX ? -tangents[t].x : tangents[t].x,
                                                flipY ? -tangents[t].y : tangents[t].y,
                                                flipZ ? -tangents[t].z : tangents[t].z);
                                    tangent = tform.TransformDirection(tangent);
                                    tbuff[t] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[t].w);
                                }
                            }
                        }
                    }
                }


                // Update objToWriteGroups because of autoAtlas
                if (autoAtlas)
                {
                    for(int i=0; i<objsToWrite.Count; i++)
                    {
                        objsToWriteGroup[i] = GetLMGroupFromObject(objsToWrite[i]);
                    }
                }

                // Done collecting groups

                if (groupList.Count == 0 && modifyLightmapStorage)
                {
                    DebugLogError("You need to mark some objects static or add Bakery Lightmap Group Selector components on them.");
                    CloseAllFiles();
                    userCanceled = true;
                    ProgressBarEnd();
                    yield break;
                }

                if (objsToWrite.Count == 0)
                {
                    DebugLogError("You need to mark some objects static or add Bakery Lightmap Group Selector components on them.");
                    CloseAllFiles();
                    userCanceled = true;
                    ProgressBarEnd();
                    yield break;
                }

                if (atlasOnly)
                {
                    atlasOnlyObj = new List<MeshRenderer>();
                    atlasOnlySize = new List<int>();
                    atlasOnlyID = new List<int>();
                    atlasOnlyScaleOffset = new List<Vector4>();
                    var emptyVec4 = new Vector4(1,1,0,0);
                    Rect rc = new Rect();
                    for(int i=0; i<objsToWrite.Count; i++)
                    {
                        var lmgroup = objsToWriteGroup[i];
                        var holderObj = objsToWriteHolder[i];
                        if (holderObj != null)
                        {
                            if (!holderRect.TryGetValue(holderObj, out rc))
                            {
                                holderObj = null;
                            }
                        }
                        var scaleOffset = holderObj == null ? emptyVec4 : new Vector4(rc.width, rc.height, rc.x, rc.y);
                        atlasOnlyObj.Add(objsToWrite[i].GetComponent<MeshRenderer>());
                        atlasOnlyScaleOffset.Add(scaleOffset);
                        atlasOnlySize.Add(lmgroup == null ? 0 : lmgroup.resolution);
                        atlasOnlyID.Add(lmgroup == null ? 0 : lmgroup.id);
                    }
                    yield break;
                }

                // Sort LMGroups so vertex groups are never first (because Unity assumes lightmap compression on LM0)
                for(int i=0; i<groupList.Count; i++)
                {
                    groupList[i].sortingID = i;
                }
                groupList.Sort(delegate(BakeryLightmapGroup a, BakeryLightmapGroup b)
                {
                    int aa = (a.mode == BakeryLightmapGroup.ftLMGroupMode.Vertex) ? -1 : 1;
                    int bb = (b.mode == BakeryLightmapGroup.ftLMGroupMode.Vertex) ? -1 : 1;
                    return bb.CompareTo(aa);
                });
                var lmBounds2 = new List<Bounds>();
                for(int i=0; i<groupList.Count; i++)
                {
                    lmBounds2.Add(lmBounds[groupList[i].sortingID]); // apply same sorting to lmBounds
                    groupList[i].id = i;
                }
                lmBounds = lmBounds2;

                // Check for existing files
                if (overwriteWarning)
                {
                    var checkGroupList = groupList;
                    if (overwriteWarningSelectedOnly)
                    {
                        var selObjs = Selection.objects;
                        checkGroupList = new List<BakeryLightmapGroup>();
                        for(int o=0; o<selObjs.Length; o++)
                        {
                            if (selObjs[o] as GameObject == null) continue;
                            var selGroup = ftBuildGraphics.GetLMGroupFromObject(selObjs[o] as GameObject);
                            if (selGroup == null) continue;
                            if (!checkGroupList.Contains(selGroup))
                            {
                                checkGroupList.Add(selGroup);
                            }
                        }
                    }
                    var existingFilenames = "";
                    for(int i=0; i<checkGroupList.Count; i++)
                    {
                        var nm = checkGroupList[i].name;
                        var filename = nm + "_final" + overwriteExtensionCheck;
                        var outputPath = ftRenderLightmap.outputPath;
                        if (File.Exists("Assets/" + outputPath + "/" + filename))
                        {
                            existingFilenames += filename + "\n";
                        }
                    }
                    if (existingFilenames.Length > 0)
                    {
                        ProgressBarEnd(false);
                        if (!EditorUtility.DisplayDialog("Lightmap overwrite", "These lightmaps will be overwritten:\n\n" + existingFilenames, "Overwrite", "Cancel"))
                        {
                            CloseAllFiles();
                            userCanceled = true;
                            ProgressBarEnd();
                            yield break;
                        }
                        ProgressBarInit("Exporting scene - preparing...", window);
                    }
                }

                ftRenderLightmap.giLodModeEnabled = ftRenderLightmap.giLodMode == ftRenderLightmap.GILODMode.ForceOn;
                ulong approxMem = 0;

                if (groupList.Count > 100)
                {
                    ProgressBarEnd(false);
                    if (!EditorUtility.DisplayDialog("Lightmap count check", groupList.Count + " lightmaps are going to be rendered. Continue?", "Continue", "Cancel"))
                    {
                        CloseAllFiles();
                        userCanceled = true;
                        ProgressBarEnd();
                        yield break;
                    }
                    ProgressBarInit("Exporting scene - preparing...", window);
                }

                if (memoryWarning || ftRenderLightmap.giLodMode == ftRenderLightmap.GILODMode.Auto)
                {
                    for(int i=0; i<groupList.Count; i++)
                    {
                        var lmgroup = groupList[i];
                        var res = lmgroup.resolution;
                        ulong lightingSize = (ulong)(res * res * 4 * 2); // RGBA16f
                        approxMem += lightingSize;
                    }
                    var tileSize = ftRenderLightmap.tileSize;
                    approxMem += (ulong)(tileSize * tileSize * 16 * 2); // maximum 2xRGBA32f (for fixPos12)
                }

                if (memoryWarning)
                {
                    ProgressBarEnd(false);
                    if (!EditorUtility.DisplayDialog("Lightmap memory check", "Rendering may require more than " + (ulong)((approxMem/1024)/1024) + "MB of video memory. Continue?", "Continue", "Cancel"))
                    {
                        CloseAllFiles();
                        userCanceled = true;
                        ProgressBarEnd();
                        yield break;
                    }
                    ProgressBarInit("Exporting scene - preparing...", window);
                }


                if (ftRenderLightmap.giLodMode == ftRenderLightmap.GILODMode.Auto)
                {

                    approxMem /= 1024;
                    approxMem /= 1024;
                    approxMem += 1024; // scene geometry size estimation - completely random

                    if ((int)approxMem > SystemInfo.graphicsMemorySize)
                    {
                        Debug.Log("GI VRAM auto optimization ON: estimated usage " + (int)approxMem + " > " + SystemInfo.graphicsMemorySize);
                        ftRenderLightmap.giLodModeEnabled = true;
                    }
                    else
                    {
                        Debug.Log("GI VRAM auto optimization OFF: estimated usage " + (int)approxMem + " < " + SystemInfo.graphicsMemorySize);
                    }
                }

                // Generate terrain geometry with detail enough for given size for UVGBuffer purposes
                fhmaps = new BinaryWriter(File.Open(scenePath + "/heightmaps.bin", FileMode.Create));
                if (ftRenderLightmap.clientMode) ftClient.serverFileList.Add("heightmaps.bin");
                if (exportTerrainAsHeightmap)
                {
                    for(int i=0; i<objsToWrite.Count; i++)
                    {
                        var obj = objsToWrite[i];
                        if (obj.name != "__ExportTerrain") continue;

                        var holderObj = objsToWriteHolder[i];
                        Rect rc = new Rect();
                        if (holderObj != null)
                        {
                            if (!holderRect.TryGetValue(holderObj, out rc))
                            {
                                holderObj = null;
                            }
                        }
                        if (holderObj == null) continue;

                        var lmgroup = objsToWriteGroup[i];
                        //float terrainPixelWidth = rc.width * lmgroup.resolution;
                        //float terrainPixelHeight = rc.height * lmgroup.resolution;

                        var index = terrainObjectList.IndexOf(obj.transform.parent.gameObject);
                        var terrain = terrainObjectToActual[index];
                        var tdata = terrain.terrainData;
                        //var heightmapResolution = tdata.heightmapResolution;

                        //int closestSize = (int)Mathf.Min(Mathf.NextPowerOfTwo((int)Mathf.Max(terrainPixelWidth, terrainPixelHeight)), heightmapResolution-1);
                        //if (closestSize < 2) continue;
                        //int mipLog2 = (int)(Mathf.Log(closestSize) / Mathf.Log(2.0f));
                        //int maxMipLog2 = (int)(Mathf.Log(heightmapResolution-1) / Mathf.Log(2.0f));
                        //int mip = maxMipLog2 - mipLog2;

                        float scaleX = tdata.size.x;// / (heightmapResolution-1);
                        //float scaleY = tdata.size.y;
                        float scaleZ = tdata.size.z;// / (heightmapResolution-1);
                        float offsetX = obj.transform.position.x;
                        float offsetY = obj.transform.position.y;
                        float offsetZ = obj.transform.position.z;

                        terrainObjectToLMID[index] = lmgroup.id;
                        terrainObjectToBoundsUV[index*4] = rc.x;
                        terrainObjectToBoundsUV[index*4+1] = rc.y;
                        terrainObjectToBoundsUV[index*4+2] = rc.width;
                        terrainObjectToBoundsUV[index*4+3] = rc.height;

                        if (uvgbHeightmap)
                        {
                            var indexArrays = objsToWriteIndices[i] = new int[1][];
                            var indexArray = indexArrays[0] = new int[6];//(closestSize-1)*(closestSize-1)*6];
                            //int indexOffset = 0;
                            //int vertOffset = 0;
                            var uvArray = objsToWriteVerticesUV[i] = objsToWriteVerticesUV2[i] = new Vector2[4];//closestSize*closestSize];

                            var posArray = objsToWriteVerticesPosW[i] = new Vector3[4];
                            var normalArray = objsToWriteVerticesNormalW[i] = new Vector3[4];

                            posArray[0] = new Vector3(offsetX, offsetY, offsetZ);
                            posArray[1] = new Vector3(offsetX + scaleX, offsetY, offsetZ);
                            posArray[2] = new Vector3(offsetX, offsetY, offsetZ + scaleZ);
                            posArray[3] = new Vector3(offsetX + scaleX, offsetY, offsetZ + scaleZ);

                            normalArray[0] = Vector3.up;
                            normalArray[1] = Vector3.up;
                            normalArray[2] = Vector3.up;
                            normalArray[3] = Vector3.up;

                            uvArray[0] = new Vector2(0,0);
                            uvArray[1] = new Vector2(1,0);
                            uvArray[2] = new Vector2(0,1);
                            uvArray[3] = new Vector2(1,1);

                            indexArray[0] = 0;
                            indexArray[1] = 2;
                            indexArray[2] = 3;

                            indexArray[3] = 0;
                            indexArray[4] = 3;
                            indexArray[5] = 1;
                        }
                        else
                        {
                            /*if (mip == 0)
                            {
                                // use existing heightmap
                                var heights = tdata.GetHeights(0, 0, heightmapResolution, heightmapResolution);
                                var posArray = objsToWriteVerticesPosW[i] = new Vector3[heightmapResolution * heightmapResolution];
                                objsToWriteVerticesNormalW[i] = terrainObjectToNormalMip0[index];
                                closestSize = heightmapResolution;
                                scaleX /= closestSize-1;
                                scaleZ /= closestSize-1;
                                for(int y=0; y<closestSize; y++)
                                {
                                    for(int x=0; x<closestSize; x++)
                                    {
                                        float px = x * scaleX + offsetX;
                                        float pz = y * scaleZ + offsetZ;
                                        posArray[y * closestSize + x] = new Vector3(px, heights[y, x] * scaleY + offsetY, pz);
                                    }
                                }
                            }
                            else
                            {
                                // use mip
                                var heights = terrainObjectToHeightMips[index][mip - 1];
                                var posArray = objsToWriteVerticesPosW[i] = new Vector3[closestSize * closestSize];
                                objsToWriteVerticesNormalW[i] = terrainObjectToNormalMips[index][mip - 1];
                                scaleX /= closestSize-1;
                                scaleZ /= closestSize-1;
                                for(int y=0; y<closestSize; y++)
                                {
                                    for(int x=0; x<closestSize; x++)
                                    {
                                        float px = x * scaleX + offsetX;
                                        float pz = y * scaleZ + offsetZ;
                                        posArray[y * closestSize + x] = new Vector3(px, heights[y * closestSize + x] * scaleY + offsetY, pz);
                                    }
                                }
                            }
                            var indexArrays = objsToWriteIndices[i] = new int[1][];
                            var indexArray = indexArrays[0] = new int[(closestSize-1)*(closestSize-1)*6];
                            int indexOffset = 0;
                            int vertOffset = 0;
                            var uvArray = objsToWriteVerticesUV[i] = objsToWriteVerticesUV2[i] = new Vector2[closestSize*closestSize];
                            for(int y=0; y<closestSize; y++)
                            {
                                for(int x=0; x<closestSize; x++)
                                {
                                    uvArray[y * closestSize + x] = new Vector2(x / (float)(closestSize-1), y / (float)(closestSize-1));

                                    if (x < closestSize-1 && y < closestSize-1)
                                    {
                                        indexArray[indexOffset] = vertOffset;
                                        indexArray[indexOffset + 1] = vertOffset + closestSize;
                                        indexArray[indexOffset + 2] = vertOffset + closestSize + 1;

                                        indexArray[indexOffset + 3] = vertOffset;
                                        indexArray[indexOffset + 4] = vertOffset + closestSize + 1;
                                        indexArray[indexOffset + 5] = vertOffset + 1;

                                        indexOffset += 6;
                                    }
                                    vertOffset++;
                                }
                            }*/
                        }
                    }

                    // Export heightmap metadata
                    if (terrainObjectToActual.Count > 0)
                    {
                        terrainObjectToHeightMapPtr = new IntPtr[terrainObjectToHeightMap.Count];
                        for(int i=0; i<terrainObjectToHeightMap.Count; i++)
                        {
                            fhmaps.Write(terrainObjectToLMID[i]);
                            for(int fl=0; fl<6; fl++) fhmaps.Write(terrainObjectToBounds[i*6+fl]);
                            for(int fl=0; fl<4; fl++) fhmaps.Write(terrainObjectToBoundsUV[i*4+fl]);
                        }
                    }
                }

                // Write lightmap definitions
                var flms = new BinaryWriter(File.Open(scenePath + "/lms.bin", FileMode.Create));
                var flmlod = new BinaryWriter(File.Open(scenePath + "/lmlod.bin", FileMode.Create));
                var flmuvgb = new BinaryWriter(File.Open(scenePath + "/lmuvgb.bin", FileMode.Create));

                if (ftRenderLightmap.clientMode)
                {
                    ftClient.serverFileList.Add("lms.bin");
                    ftClient.serverFileList.Add("lmlod.bin");
                    ftClient.serverFileList.Add("lmuvgb.bin");
                }

                // Init global UVGBuffer flags
                int uvgbGlobalFlags = 0;
                if (exportShaderColors)
                {
                    if (ftRenderLightmap.renderDirMode == ftRenderLightmap.RenderDirMode.BakedNormalMaps)
                    {
                        uvgbGlobalFlags = UVGBFLAG_FACENORMAL | UVGBFLAG_POS | UVGBFLAG_SMOOTHPOS;
                    }
                    else if (ftRenderLightmap.renderDirMode == ftRenderLightmap.RenderDirMode.RNM ||
                            (ftRenderLightmap.renderDirMode == ftRenderLightmap.RenderDirMode.SH && tangentSHLights))
                    {
                        uvgbGlobalFlags = UVGBFLAG_NORMAL | UVGBFLAG_FACENORMAL | UVGBFLAG_POS | UVGBFLAG_SMOOTHPOS | UVGBFLAG_TANGENT;
                    }
                    else
                    {
                        uvgbGlobalFlags = UVGBFLAG_NORMAL | UVGBFLAG_FACENORMAL | UVGBFLAG_POS | UVGBFLAG_SMOOTHPOS;
                    }
                }
                else
                {
                    uvgbGlobalFlags = UVGBFLAG_NORMAL | UVGBFLAG_FACENORMAL | UVGBFLAG_ALBEDO | UVGBFLAG_EMISSIVE | UVGBFLAG_POS | UVGBFLAG_SMOOTHPOS;
                }
                if (terrainObjectToActual.Count > 0) uvgbGlobalFlags |= UVGBFLAG_TERRAIN;
                SetUVGBFlags(uvgbGlobalFlags);

                for(int i=0; i<groupList.Count; i++)
                {
                    var lmgroup = groupList[i];
                    flms.Write(lmgroup.name);

                    flmlod.Write(lmgroup.sceneLodLevel);

                    int uvgbflags = 0;

                    if (lmgroup.containsTerrains && exportTerrainAsHeightmap)
                        uvgbflags = uvgbGlobalFlags | (UVGBFLAG_NORMAL | UVGBFLAG_TERRAIN);

                    if (lmgroup.renderDirMode == BakeryLightmapGroup.RenderDirMode.BakedNormalMaps)
                        uvgbflags = UVGBFLAG_FACENORMAL | UVGBFLAG_POS | UVGBFLAG_SMOOTHPOS;

                    if (lmgroup.renderDirMode == BakeryLightmapGroup.RenderDirMode.RNM ||
                        (lmgroup.renderDirMode == BakeryLightmapGroup.RenderDirMode.SH && tangentSHLights))
                        uvgbflags = UVGBFLAG_NORMAL | UVGBFLAG_FACENORMAL | UVGBFLAG_POS | UVGBFLAG_SMOOTHPOS | UVGBFLAG_TANGENT;

                    if (lmgroup.probes) uvgbflags = UVGBFLAG_RESERVED;

                    flmuvgb.Write(uvgbflags);

                    if (ftRenderLightmap.clientMode)
                    {
                        if (uvgbflags == 0) uvgbflags = uvgbGlobalFlags;

                        ftClient.serverFileList.Add("uvpos_" + lmgroup.name + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"));
                        ftClient.serverFileList.Add("uvnormal_" + lmgroup.name + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"));
                        ftClient.serverFileList.Add("uvalbedo_" + lmgroup.name + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"));
                        if (lmgroup.mode != BakeryLightmapGroup.ftLMGroupMode.Vertex)
                        {
                            if ((uvgbflags & UVGBFLAG_SMOOTHPOS) != 0) ftClient.serverFileList.Add("uvsmoothpos_" + lmgroup.name + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"));
                        }
                        if ((uvgbflags & UVGBFLAG_FACENORMAL) != 0) ftClient.serverFileList.Add("uvfacenormal_" + lmgroup.name + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"));
                        if ((uvgbflags & UVGBFLAG_TANGENT) != 0) ftClient.serverFileList.Add("uvtangent_" + lmgroup.name + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"));
                    }

                    if (lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.Vertex)
                    {
                        int atlasTexSize = (int)Mathf.Ceil(Mathf.Sqrt((float)lmgroup.totalVertexCount));
                        if (atlasTexSize > 8192) Debug.LogWarning("Warning: vertex lightmap group " + lmgroup.name + " uses resolution of " + atlasTexSize);
                        atlasTexSize = (int)Mathf.Ceil(atlasTexSize / (float)ftRenderLightmap.tileSize) * ftRenderLightmap.tileSize;
                        flms.Write(-atlasTexSize);
                    }
                    else
                    {
                        flms.Write(lmgroup.resolution);
                    }
                    //Debug.LogError(lmgroup.name+": " + lmgroup.resolution);
                }
                flms.Close();
                flmlod.Close();
                flmuvgb.Close();

                voffset = ioffset = soffset = 0; // vertex/index/surface write

                // Per-surface alpha texture IDs
                var alphaIDs = new List<ushort>();

                int albedoCounter = 0;
                var albedoMap = new Dictionary<IntPtr, int>(); // albedo ptr -> ID map

                int alphaCounter = 0;
                var alphaMap = new Dictionary<IntPtr, List<int>>(); // alpha ptr -> ID map

                var dummyTexList = new List<Texture>(); // list of single-color 1px textures
                var dummyPixelArray = new Color[1];

                if (ftRenderLightmap.checkOverlaps)
                {
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    var qmesh = quad.GetComponent<MeshFilter>().sharedMesh;
                    var pmesh = plane.GetComponent<MeshFilter>().sharedMesh;
                    DestroyImmediate(quad);
                    DestroyImmediate(plane);
                    bool canCheck = ftModelPostProcessor.InitOverlapCheck();
                    if (!canCheck)
                    {
                        DebugLogError("Can't load ftOverlapTest.shader");
                        CloseAllFiles();
                        userCanceled = true;
                        ProgressBarEnd();
                        yield break;
                    }
                    for(int g=0; g<groupList.Count; g++)
                    {
                        var lmgroup = groupList[g];
                        if (lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.Vertex) continue;
                        for(int i=0; i<objsToWrite.Count; i++)
                        {
                            if (objsToWriteGroup[i] != lmgroup) continue;
                            var obj = objsToWrite[i];

                            var mesh = obj.GetComponent<MeshFilter>().sharedMesh;
                            if (mesh == qmesh || mesh == pmesh) continue;

                            var uv = objsToWriteVerticesUV[i];//mesh.uv;
                            var uv2 = objsToWriteVerticesUV2[i];//mesh.uv2;
                            var usedUVs = uv2.Length == 0 ? uv : uv2;
                            bool validUVs = true;
                            for(int v=0; v<usedUVs.Length; v++)
                            {
                                if (usedUVs[v].x < -0.0001f || usedUVs[v].x > 1.0001f || usedUVs[v].y < -0.0001f || usedUVs[v].y > 1.0001f)
                                {
                                    validUVs = false;
                                    break;
                                }
                            }
                            if (!validUVs)
                            {
                                string objPath = obj.name;
                                var prt = obj.transform.parent;
                                while(prt != null)
                                {
                                    objPath = prt.name + "\\" + objPath;
                                    prt = prt.parent;
                                }
                                ftRenderLightmap.simpleProgressBarEnd();
                                if (!EditorUtility.DisplayDialog("Incorrect UVs", "Object " + objPath + " UVs are out of 0-1 bounds", "Continue", "Stop"))
                                {
                                    CloseAllFiles();
                                    userCanceled = true;
                                    ProgressBarEnd();
                                    yield break;
                                }
                                ProgressBarInit("Exporting scene - preparing...", window);
                            }

                            int overlap = ftModelPostProcessor.DoOverlapCheck(obj, false);
                            if (overlap != 0)
                            {
                                storage.debugRT = ftModelPostProcessor.rt;
                                string objPath = obj.name;
                                var prt = obj.transform.parent;
                                while(prt != null)
                                {
                                    objPath = prt.name + "\\" + objPath;
                                    prt = prt.parent;
                                }
                                if (overlap < 0)
                                {
                                    ftRenderLightmap.simpleProgressBarEnd();
                                    if (!EditorUtility.DisplayDialog("Incorrect UVs", "Object " + objPath + " has no UV2", "Continue", "Stop"))
                                    {
                                        CloseAllFiles();
                                        userCanceled = true;
                                        ProgressBarEnd();
                                        yield break;
                                    }
                                    ProgressBarInit("Exporting scene - preparing...", window);
                                }
                                else
                                {
                                  ftRenderLightmap.simpleProgressBarEnd();
                                    if (!EditorUtility.DisplayDialog("Incorrect UVs", "Object " + objPath + " has overlapping UVs", "Continue", "Stop"))
                                    {
                                        CloseAllFiles();
                                        userCanceled = true;
                                        ProgressBarEnd();
                                        yield break;
                                    }
                                    ProgressBarInit("Exporting scene - preparing...", window);
                                }
                            }
                        }
                    }
                    ftModelPostProcessor.EndOverlapCheck();
                }

                // Prepare progressbar
                int progressNumObjects = 0;
                foreach(GameObject obj in objects)
                {
                    if (obj == null) continue;
                    if (!obj.activeInHierarchy) continue;
                    progressNumObjects++;
                }

                // Open files to write
                fscene = new BinaryWriter(File.Open(scenePath + "/objects.bin", FileMode.Create));
                fmesh = new BinaryWriter(File.Open(scenePath + "/mesh.bin", FileMode.Create));
                flmid = new BinaryWriter(File.Open(scenePath + "/lmid.bin", FileMode.Create));
                fseamfix = new BinaryWriter(File.Open(scenePath + "/seamfix.bin", FileMode.Create));
                fsurf = new BinaryWriter(File.Open(scenePath + "/surf.bin", FileMode.Create));
                fmatid = new BinaryWriter(File.Open(scenePath + "/matid.bin", FileMode.Create));
                fmatide = new BinaryWriter(File.Open(scenePath + "/emissiveid.bin", FileMode.Create));
                fmatideb = new BinaryWriter(File.Open(scenePath + "/emissivemul.bin", FileMode.Create));
                fmatidh = new BinaryWriter(File.Open(scenePath + "/heightmapid.bin", FileMode.Create));
                falphaid = new BinaryWriter(File.Open(scenePath + "/alphaid.bin", FileMode.Create));
                fvbfull = new BinaryWriter(File.Open(scenePath + "/vbfull.bin", FileMode.Create));
                fvbtrace = new BinaryWriter(File.Open(scenePath + "/vbtrace.bin", FileMode.Create));
                fvbtraceTex = new BinaryWriter(File.Open(scenePath + "/vbtraceTex.bin", FileMode.Create));
                fvbtraceUV0 = new BinaryWriter(File.Open(scenePath + "/vbtraceUV0.bin", FileMode.Create));
                fib = new BinaryWriter(File.Open(scenePath + "/ib.bin", FileMode.Create));
                fib32 = new BinaryWriter(File.Open(scenePath + "/ib32.bin", FileMode.Create));
                fib32lod = new BinaryWriter[sceneLodsUsed];
                for(int i=0; i<sceneLodsUsed; i++)
                {
                    fib32lod[i] = new BinaryWriter(File.Open(scenePath + "/ib32_lod" + i + ".bin", FileMode.Create));
                    if (ftRenderLightmap.clientMode) ftClient.serverFileList.Add("ib32_lod" + i + ".bin");
                }
                falphaidlod = new BinaryWriter[sceneLodsUsed];
                for(int i=0; i<sceneLodsUsed; i++)
                {
                    falphaidlod[i] = new BinaryWriter(File.Open(scenePath + "/alphaid_lod" + i + ".bin", FileMode.Create));
                    if (ftRenderLightmap.clientMode) ftClient.serverFileList.Add("alphaid2_lod" + i + ".bin"); // alphaid2, not alphaid
                }

                if (ftRenderLightmap.clientMode)
                {
                    ftClient.serverFileList.Add("objects.bin");
                    ftClient.serverFileList.Add("mesh.bin");
                    ftClient.serverFileList.Add("lmid.bin");
                    ftClient.serverFileList.Add("seamfix.bin");
                    ftClient.serverFileList.Add("surf.bin");
                    ftClient.serverFileList.Add("matid.bin");
                    ftClient.serverFileList.Add("emissiveid.bin");
                    ftClient.serverFileList.Add("emissivemul.bin");
                    ftClient.serverFileList.Add("heightmapid.bin");
                    ftClient.serverFileList.Add("alphaid2.bin"); // alphaid2, not alphaid
                        ftClient.serverFileList.Add("alphabuffer.bin");
                    ftClient.serverFileList.Add("vbfull.bin");
                    ftClient.serverFileList.Add("vbtrace.bin");
                    ftClient.serverFileList.Add("vbtraceTex.bin");
                    ftClient.serverFileList.Add("vbtraceUV0.bin");
                    ftClient.serverFileList.Add("ib.bin");
                    ftClient.serverFileList.Add("ib32.bin");
                }

                // Export heightmap metadata
                //fhmaps.Write(terrainObjectToActual.Count);
                if (terrainObjectToActual.Count > 0)
                {
                    //terrainObjectToHeightMapPtr = new IntPtr[terrainObjectToHeightMap.Count];
                    /*for(int i=0; i<terrainObjectToHeightMap.Count; i++)
                    {
                        for(int fl=0; fl<6; fl++) fhmaps.Write(terrainObjectToBounds[i*6+fl]);
                    }*/
                }

                // Export some scene data
                // - LMIDs
                // - mesh definitions
                // - surface definitions
                // - albedo IDs
                // - alpha IDs
                // - update LMGroup bounds
                // - export index buffer
                // - generate tracing index buffer

                areaLightCounter = -2;
                //var defaultTexST = new Vector4(1,1,0,0);
                //var objsToWriteTexST = new List<Vector4>();

                for(int i=0; i<objsToWrite.Count; i++)
                {
                    var obj = objsToWrite[i];
                    var lmgroup = objsToWriteGroup[i];
                    var holderObj = objsToWriteHolder[i];

                    if (obj == null)
                    {
                        // wtf
                        DebugLogError("Object " + objsToWriteNames[i] + " was destroyed mid-export");
                        CloseAllFiles();
                        userCanceled = true;
                        ProgressBarEnd();
                        yield break;
                    }

                    var m = obj.GetComponent<MeshFilter>().sharedMesh;
                    var mr = obj.GetComponent<MeshRenderer>();

                    var inds = objsToWriteIndices[i];

                    // Write LMID, mesh and surface definition
                    int id = exportLMID(flmid, obj, lmgroup);
                    exportMesh(fmesh, m);
                    exportSurfs(fsurf, inds, inds.Length);// m);

                    int lodLevel;
                    if (!objToLodLevel.TryGetValue(obj, out lodLevel)) lodLevel = -1;

                    bool isTerrain = (exportTerrainAsHeightmap && obj.name == "__ExportTerrain");

                    // Write albedo IDs, collect alpha IDs, update LMGroup bounds
                    if (id >= 0) {
                        for(int k=0; k<m.subMeshCount; k++) {
                            // Get mesh albedos
                            int texID = -1;
                            Material mat = null;
                            Texture tex = null;
                            //var texST = defaultTexST;
                            if (k < mr.sharedMaterials.Length) {
                                mat = mr.sharedMaterials[k];
                                if (mat != null)
                                {
                                    if (mat.HasProperty("_MainTex"))
                                    {
                                        tex = mat.mainTexture;
                                        //if (mat.HasProperty("_MainTex_ST"))
                                        //{
                                          //  texST = mat.GetVector("_MainTex_ST");
                                        //}
                                    }
                                    else if (mat.HasProperty("_BaseColorMap"))
                                    {
                                        // HDRP
                                        tex = mat.GetTexture("_BaseColorMap");
                                    }
                                }
                            }
                            IntPtr texPtr = (IntPtr)0;
                            Texture texWrite = null;
                            if (tex != null)
                            {
                                texPtr = tex.GetNativeTexturePtr();
                                texWrite = tex;
                                if (texPtr == (IntPtr)0)
                                {
                                    if ((tex as RenderTexture) != null)
                                    {
                                        Debug.LogError("RenderTexture " + tex.name + " cannot be used as albedo (GetNativeTexturePtr returned null)");
                                        tex = null;
                                        texWrite = null;
                                    }
                                    else
                                    {
                                        Debug.LogError("Texture " + tex.name + " cannot be used as albedo (GetNativeTexturePtr returned null)");
                                        tex = null;
                                        texWrite = null;
                                    }
                                }
                            }
                            if (tex == null)
                            {
                                // Create dummy 1px texture
                                var dummyTex = new Texture2D(1,1);
                                dummyPixelArray[0] = (mat == null || !mat.HasProperty("_Color")) ? Color.white : mat.color;
                                dummyTex.SetPixels(dummyPixelArray);
                                dummyTex.Apply();
                                texWrite = dummyTex;
                                dummyTexList.Add(dummyTex);
                                texPtr = dummyTex.GetNativeTexturePtr();
                                if (texPtr == (IntPtr)0)
                                {
                                    Debug.LogError("Failed to call GetNativeTexturePtr() on newly created texture");
                                    texWrite = null;
                                }
                            }
                            if (!albedoMap.TryGetValue(texPtr, out texID))
                            {
                                lmAlbedoList.Add(texPtr);
                                lmAlbedoListTex.Add(texWrite);
                                albedoMap[texPtr] = albedoCounter;
                                texID = albedoCounter;
                                albedoCounter++;
                            }

                            // Write albedo ID
                            fmatid.Write((ushort)texID);

                            // Get mesh alphas
                            ushort alphaID = 0xFFFF;
                            if (tex != null) {
                                var matTag = mat.GetTag("RenderType", true);
                                bool isCutout = matTag == "TransparentCutout";
                                if (isCutout || matTag == "Transparent" || matTag == "TreeLeaf") {

                                    float alphaRef = 0.5f;
                                    if (mat != null && mat.HasProperty("_Cutoff"))
                                    {
                                        alphaRef = mat.GetFloat("_Cutoff");
                                    }
                                    float opacity = 1.0f;
                                    if (!isCutout && mat.HasProperty("_Color"))
                                    {
                                        opacity = mat.color.a;
                                    }
                                    // let constant alpha affect cutout theshold for alphablend materials
                                    alphaRef = 1.0f - (1.0f - alphaRef) * opacity;
                                    if (alphaRef > 1) alphaRef = 1;

                                    // allow same map instances with different threshold
                                    List<int> texIDs;
                                    if (!alphaMap.TryGetValue(texPtr, out texIDs))
                                    {
                                        alphaMap[texPtr] = texIDs = new List<int>();

                                        lmAlphaList.Add(texPtr);
                                        lmAlphaListTex.Add(tex);
                                        lmAlphaRefList.Add(alphaRef);

                                        texIDs.Add(alphaCounter);
                                        texID = alphaCounter;
                                        alphaCounter++;
                                        //Debug.Log("Alpha " + texID+": " + tex.name+" "+alphaRef);
                                        alphaID = (ushort)texID;
                                    }
                                    else
                                    {
                                        int matchingInstance = -1;
                                        for(int instance=0; instance<texIDs.Count; instance++)
                                        {
                                            texID = texIDs[instance];
                                            if (Mathf.Abs(lmAlphaRefList[texID] - alphaRef) <= alphaInstanceThreshold)
                                            {
                                                matchingInstance = instance;
                                                alphaID = (ushort)texID;
                                                break;
                                            }
                                        }
                                        if (matchingInstance < 0)
                                        {
                                            lmAlphaList.Add(texPtr);
                                            lmAlphaListTex.Add(tex);
                                            lmAlphaRefList.Add(alphaRef);

                                            texIDs.Add(alphaCounter);
                                            texID = alphaCounter;
                                            alphaCounter++;
                                            //Debug.Log("Alpha " + texID+": " + tex.name+" "+alphaRef);
                                            alphaID = (ushort)texID;
                                        }
                                    }
                                }
                            }
                            alphaIDs.Add(alphaID);

                            // Get mesh emissives
                            if (exportShaderColors)
                            {
                                for(int s=0; s<sceneCount; s++)
                                {
                                    if (storages[s] == null) continue;
                                    while(storages[s].hasEmissive.Count <= id) storages[s].hasEmissive.Add(true);
                                    storages[s].hasEmissive[id] = true;
                                }
                            }

                            texID = -1;
                            tex = null;
                            if (mat!=null && mat.shaderKeywords.Contains("_EMISSION"))
                            {
                                if (mat.HasProperty("_EmissionMap")) tex = mat.GetTexture("_EmissionMap");
                                if (tex != null)
                                {
                                    texPtr = tex.GetNativeTexturePtr();
                                    if (texPtr == (IntPtr)0)
                                    {
                                        if ((tex as RenderTexture) != null)
                                        {
                                            Debug.LogError("RenderTexture " + tex.name + " cannot be used as emission (GetNativeTexturePtr returned null)");
                                            tex = null;
                                        }
                                        else
                                        {
                                            Debug.LogError("Texture " + tex.name + " cannot be used as emission (GetNativeTexturePtr returned null)");
                                            tex = null;
                                        }
                                        //DebugLogError("Null emission tex ptr");
                                    }
                                }
                                if (tex == null && mat.HasProperty("_EmissionColor"))
                                {
                                    // Create dummy 1px texture
                                    var dummyTex = new Texture2D(1,1);
                                    dummyPixelArray[0] = mat.GetColor("_EmissionColor");
                                    dummyTex.SetPixels(dummyPixelArray);
                                    dummyTex.Apply();
                                    tex = dummyTex;
                                    dummyTexList.Add(dummyTex);
                                    texPtr = dummyTex.GetNativeTexturePtr();
                                    if (texPtr == (IntPtr)0)
                                    {
                                        Debug.LogError("Failed to call GetNativeTexturePtr() on newly created texture");
                                        texWrite = null;
                                        //DebugLogError("Null dummy tex ptr");
                                    }
                                }
                                if (!albedoMap.TryGetValue(texPtr, out texID))
                                {
                                    lmAlbedoList.Add(texPtr);
                                    lmAlbedoListTex.Add(tex);
                                    albedoMap[texPtr] = albedoCounter;
                                    texID = albedoCounter;
                                    albedoCounter++;
                                }
                                for(int s=0; s<sceneCount; s++)
                                {
                                    if (storages[s] == null) continue;
                                    while(storages[s].hasEmissive.Count <= id) storages[s].hasEmissive.Add(false);
                                    storages[s].hasEmissive[id] = true;
                                }

                                fmatide.Write((ushort)texID);
                                fmatideb.Write(mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor").maxColorComponent : 1);
                            }
                            else
                            {
                                fmatide.Write((ushort)0xFFFF);
                                fmatideb.Write(0.0f);
                            }

                            if (isTerrain && uvgbHeightmap)
                            {
                                var hindex = terrainObjectList.IndexOf(obj.transform.parent.gameObject);
                                //var htex = terrainObjectToHeightMap[hindex];
                                //texPtr = htex.GetNativeTexturePtr();

                                //heightmapList.Add(texPtr);
                                //heightmapListTex.Add(htex);
                                //heightmapListBounds.Add();

                                //texID = heightmapCounter;
                                //heightmapCounter++;

                                fmatidh.Write((ushort)hindex);//texID);
                            }
                            else
                            {
                                fmatidh.Write((ushort)0xFFFF);
                            }
                        }

                        // Update LMGroup bounds
                        if (modifyLightmapStorage)
                        {
                            if (lmBounds[id].size == Vector3.zero) {
                                lmBounds[id] = mr.bounds;
                            } else {
                                var b = lmBounds[id];
                                b.Encapsulate(mr.bounds);
                                lmBounds[id] = b;
                            }
                        }

                    } else {
                        // Write empty albedo/alpha IDs for non-lightmapped
                        for(int k=0; k<m.subMeshCount; k++) {
                            fmatid.Write((ushort)0);
                            alphaIDs.Add(0xFFFF);
                            fmatide.Write((ushort)0xFFFF);
                            fmatideb.Write(0.0f);
                            fmatidh.Write((ushort)0xFFFF);
                        }
                    }

                    int currentVoffset = voffset;
                    voffset += objsToWriteVerticesPosW[i].Length;// m.vertexCount;

                    // Check if mesh is flipped
                    bool isFlipped = Mathf.Sign(obj.transform.lossyScale.x*obj.transform.lossyScale.y*obj.transform.lossyScale.z) < 0;

                    while(lmIndexArrays.Count <= id)
                    {
                        lmIndexArrays.Add(new List<int>());
                        lmLocalToGlobalIndices.Add(new List<int>());
                        lmVOffset.Add(0);
                    }

                    var mmr = obj.GetComponent<MeshRenderer>();
                    var castsShadows = mmr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;
                    if (exportTerrainAsHeightmap && obj.name == "__ExportTerrain") castsShadows = false; // prevent exporting placeholder quads to ftrace

                    time = GetTime();
                    for(int k=0;k<m.subMeshCount;k++) {
                        // Export regular index buffer
                        //var indexCount = exportIB(fib, m, k, isFlipped, false, 0, null, 0);
                        var indexCount = exportIB(fib, inds[k], isFlipped, false, 0, null, 0);

                        bool submeshCastsShadows = castsShadows;
                        if (submeshCastsShadows)
                        {
                            var mats = mmr.sharedMaterials;
                            if (mats.Length > k)
                            {
                                if (mats[k] != null)
                                {
                                    var matTag = mats[k].GetTag("RenderType", true);
                                    if (matTag == "Transparent" || matTag == "TreeLeaf")
                                    {
                                        if (mats[k].HasProperty("_Color"))
                                        {
                                            if (mats[k].color.a < 0.5f) submeshCastsShadows = false;
                                        }
                                    }
                                }
                            }
                        }

                        // Generate tracing index buffer, write alpha IDs per triangle
                        if (submeshCastsShadows)
                        {
                            var alphaID = alphaIDs[(alphaIDs.Count - m.subMeshCount) + k];

                            if (lodLevel < 0)
                            {
                                // Export persistent IB
                                var indicesOpaqueArray = indicesOpaque;
                                var indicesTransparentArray = indicesTransparent;
                                var falphaidFile = falphaid;

                                exportIB32(indicesOpaqueArray, indicesTransparentArray, id>=0 ? lmIndexArrays[id] : null,
                                             inds[k], isFlipped, currentVoffset, id>=0 ? lmVOffset[id] : 0, falphaidFile, alphaID);
                            }
                            else
                            {
                                // Export LOD IBs
                                var visList = objToLodLevelVisible[obj];
                                for(int vlod=0; vlod<visList.Count; vlod++)
                                {
                                    int lod = visList[vlod];
                                    var indicesOpaqueArray = indicesOpaqueLOD[lod];
                                    var indicesTransparentArray = indicesTransparentLOD[lod];
                                    var falphaidFile = falphaidlod[lod];

                                    exportIB32(indicesOpaqueArray, indicesTransparentArray, id>=0 ? lmIndexArrays[id] : null,
                                                 inds[k], isFlipped, currentVoffset, id>=0 ? lmVOffset[id] : 0, falphaidFile, alphaID);
                                }
                            }
                        }
                        ioffset += indexCount;
                    }
                    ibTime += GetTime() - time;

                    if (id >= 0)
                    {
                        var vcount = objsToWriteVerticesPosW[i].Length;//m.vertexCount;
                        var remapArray = lmLocalToGlobalIndices[id];
                        var addition = lmVOffset[id];
                        for(int k=0; k<vcount; k++)
                        {
                            remapArray.Add(k + currentVoffset);
                        }
                        lmVOffset[id] += vcount;
                    }
                }

        }
        catch
        {
            DebugLogError("Error exporting scene - see console for details");
            CloseAllFiles();
            userCanceled = true;
            ProgressBarEnd();
            throw;
        }

        ProgressBarShow("Exporting scene - finishing objects...", 0.5f);
        if (userCanceled)
        {
            CloseAllFiles();
            ProgressBarEnd();
            yield break;
        }
        yield return null;

        try
        {
                // Write vertex buffers and update storage
                Rect rc = new Rect();
                var emptyVec4 = new Vector4(1,1,0,0);
                for(int i=0; i<objsToWrite.Count; i++)
                {
                    var obj = objsToWrite[i];
                    var m = obj.GetComponent<MeshFilter>().sharedMesh;
                    var lmgroup = objsToWriteGroup[i];

                    var id = lmgroup == null ? -1 : objsToWriteGroup[i].id;

                    BakeryLightMesh areaLight = obj.GetComponent<BakeryLightMesh>();
                    if (areaLight == null)
                    {
                        var areaIndex = temporaryAreaLightMeshList.IndexOf(obj);
                        if (areaIndex >= 0) areaLight = temporaryAreaLightMeshList2[areaIndex];
                    }
                    //var areaLight =
                    if (areaLight != null) id = areaLight.lmid;

                    var vertexBake = lmgroup != null ? (lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.Vertex) : false;
                    //var castsShadows = obj.GetComponent<MeshRenderer>().shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;

                    var holderObj = objsToWriteHolder[i];
                    if (holderObj != null)
                    {
                        if (!holderRect.TryGetValue(holderObj, out rc))
                        {
                            holderObj = null;
                        }
                    }

                    time = GetTime();
                    //var vertices = m.vertices;
                    //var normals = m.normals;
                    //var tangents = m.tangents;
                    var uv = objsToWriteVerticesUV[i];//m.uv;
                    var uv2 = objsToWriteVerticesUV2[i];//m.uv2;
                    if (uv2.Length == 0 && !vertexBake) uv2 = uv;//m.uv;
                    vbTimeRead += GetTime() - time;

                    var inds = objsToWriteIndices[i];

                        var time2 = GetTime();
                        time = time2;

                    // Transform UVs
                    var tformedPos = objsToWriteVerticesPosW[i];// new Vector3[vertices.Length];
                    var tformedNormals = objsToWriteVerticesNormalW[i];// new Vector3[normals.Length];
                    Vector4[] tformedTangents = null;
                    if (NeedsTangents(lmgroup, tangentSHLights))
                    {
                        tformedTangents = objsToWriteVerticesTangentW[i];
                    }
                    Vector2[] tformedUV2;
                    if (areaLight == null && !vertexBake)
                    {
                        tformedUV2 = holderObj == null ? uv2 : new Vector2[tformedPos.Length];
                        for(int t=0; t<tformedPos.Length; t++)
                        {
                            if (holderObj != null)
                            {
                                tformedUV2[t].x = uv2[t].x * rc.width + rc.x;
                                tformedUV2[t].y = uv2[t].y * rc.height + rc.y;
                            }
                        }
                        objsToWriteUVOverride.Add(null);
                    }
                    else if (vertexBake)
                    {
                        tformedUV2 = GenerateVertexBakeUVs(lmgroup.vertexCounter, tformedPos.Length, lmgroup.totalVertexCount);
                        lmgroup.vertexCounter += tformedPos.Length;
                        objsToWriteUVOverride.Add(tformedUV2);
                    }
                    else
                    {
                        tformedUV2 = uv;
                        objsToWriteUVOverride.Add(null);
                    }

                    if (id >= 0)
                    {
                        while(lmUVArrays.Count <= id)
                        {
                            lmUVArrays.Add(new List<float>());
                        }
                        var lmUVArray = lmUVArrays[id];
                        for(int k=0; k<tformedUV2.Length; k++)
                        {
                            lmUVArray.Add(tformedUV2[k].x);
                            lmUVArray.Add(tformedUV2[k].y);
                        }
                    }

                    exportVBFull(fvbfull, tformedPos, tformedNormals, tformedTangents, uv, tformedUV2);
                        vbTimeWriteFull += GetTime() - time;
                        time = GetTime();
                    //if (castsShadows)
                    //{
                        exportVBTrace(fvbtrace, m, tformedPos, tformedNormals);
                            vbTimeWriteT += GetTime() - time;
                            time = GetTime();
                        exportVBTraceTexAttribs(vbtraceTexPosNormalArray, vbtraceTexUVArray, tformedPos, tformedNormals, tformedUV2, id, vertexBake);
                            vbTimeWriteT2 += GetTime() - time;
                            time = GetTime();
                        exportVBTraceUV0(fvbtraceUV0, uv, tformedPos.Length);
                            vbTimeWriteT3 += GetTime() - time;
                            time = GetTime();
                    //}
                    voffset += tformedPos.Length;
                    vbTimeWrite += GetTime() - time2;


                    // update storage
                    // also write seamfix.bin
                    var sceneID = sceneToID[obj.scene];
                    if (obj.name == "__ExportTerrain")
                    {
                        fseamfix.Write(false);
                        var index = terrainObjectList.IndexOf(obj.transform.parent.gameObject);
                        var terrain = terrainObjectToActual[index];
                        var scaleOffset = holderObj == null ? emptyVec4 : new Vector4(rc.width, rc.height, rc.x, rc.y);
                        if (!storages[sceneID].bakedRenderersTerrain.Contains(terrain))
                        {
                            if (modifyLightmapStorage)
                            {
                                storages[sceneID].bakedRenderersTerrain.Add(terrain);
                                storages[sceneID].bakedIDsTerrain.Add(id < 0 ? -1 : id);
                                storages[sceneID].bakedScaleOffsetTerrain.Add(scaleOffset);
                            }
                        }
                        objsToWriteScaleOffset.Add(scaleOffset);
                    }
                    else
                    {
                        fseamfix.Write(true);
                        var scaleOffset = holderObj == null ? emptyVec4 : new Vector4(rc.width, rc.height, rc.x, rc.y);
                        if (modifyLightmapStorage)
                        {
                            bool vertexImplicit = false;
                            if (vertexBake)
                            {
                                if (lmgroup.isImplicit) vertexImplicit = true;
                            }
                            if (!vertexImplicit)
                            {
                                storages[sceneID].bakedRenderers.Add(obj.GetComponent<MeshRenderer>());
                                storages[sceneID].bakedIDs.Add(id < 0 ? -1 : id);
                                storages[sceneID].bakedScaleOffset.Add(scaleOffset);
                                storages[sceneID].bakedVertexOffset.Add(vertexBake ? (lmgroup.vertexCounter - tformedPos.Length) : -1);
                                storages[sceneID].bakedVertexColorMesh.Add(null);
                            }
                        }
                        objsToWriteScaleOffset.Add(scaleOffset);
                    }
                }

                // Generate LOD UVs
                if (ftRenderLightmap.giLodModeEnabled)
                {
                    for(int s=0; s<sceneCount; s++)
                    {
                        if (storages[s] == null) continue;
                        storages[s].lmGroupMinLOD = new int[groupList.Count];
                        storages[s].lmGroupLODMatrix = new int[groupList.Count * groupList.Count];
                    }
                    for(int i=0; i<groupList.Count; i++)
                    {
                        var lmgroup = groupList[i];
                        if (lmgroup.resolution < 128)
                        {
                            lmUVArrays2.Add(null);
                            lmIndexArrays2.Add(null);
                            lmUVArrays3.Add(null);
                            continue;
                        }
                        if (lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.Vertex || lmgroup.containsTerrains)
                        {
                            lmUVArrays2.Add(null);
                            lmIndexArrays2.Add(null);
                            lmUVArrays3.Add(null);
                            if (lmgroup.containsTerrains)
                            {
                                int minLodResolutionTerrain = 128;
                                for(int s=0; s<sceneCount; s++)
                                {
                                    if (storages[s] == null) continue;
                                    int minLOD = (int)(Mathf.Log(lmgroup.resolution, 2.0f) - Mathf.Log(minLodResolutionTerrain, 2.0f)) - 1;
                                    if (minLOD < 0) minLOD = 0;
                                    storages[s].lmGroupMinLOD[lmgroup.id] = minLOD;
                                }
                            }
                            continue;
                        }
                        int id = lmgroup.id;
                        lmUVArrays2.Add(lmUVArrays[i].ToArray());
                        lmIndexArrays2.Add(lmIndexArrays[i].ToArray());

                        lmUVArrays3.Add(lmUVArrays[i].ToArray());
                        int uvIslands = uvrLoad(lmUVArrays2[i], lmUVArrays2[i].Length/2, lmIndexArrays2[i], lmIndexArrays2[i].Length);
                        if (uvIslands <= 0)
                        {
                            Debug.LogError("Can't generate LOD UVs for  " + lmgroup.name+" "+lmUVArrays2[i].Length+" "+lmIndexArrays2[i].Length+" "+lmgroup.containsTerrains);
                            uvrUnload();
                            continue;
                        }
                        int minLodResolution = Mathf.NextPowerOfTwo((int)Mathf.Ceil(Mathf.Sqrt((float)uvIslands)));
                        minLodResolution = minLodResolution << 1;
                        if (minLodResolution > lmgroup.resolution)
                        {
                            Debug.LogWarning("Not generating LOD UVs for " + lmgroup.name + ", because there are too many UV islands");
                            uvrUnload();
                            continue;
                        }
                        Debug.Log("Min LOD resolution for " + lmgroup.name + " is " + minLodResolution);
                        for(int s=0; s<sceneCount; s++)
                        {
                            if (storages[s] == null) continue;
                            int minLOD = (int)(Mathf.Log(lmgroup.resolution, 2.0f) - Mathf.Log(minLodResolution, 2.0f)) - 1;
                            if (minLOD < 0) minLOD = 0;
                            storages[s].lmGroupMinLOD[lmgroup.id] = minLOD;
                        }

                        int uvrErrCode = uvrRepack(0, minLodResolution);
                        if (uvrErrCode == -1)
                        {
                            Debug.LogError("Can't repack LOD UVs for " + lmgroup.name);
                            uvrUnload();
                            continue;
                        }
                        Debug.Log("Tries left: " + uvrErrCode);
                        uvrUnload();
                        var numLocalVerts = lmUVArrays2[i].Length / 2;
                        for(int k=0; k<numLocalVerts; k++)
                        {
                            float u = lmUVArrays2[i][k * 2];
                            u = Mathf.Clamp(u, 0, 0.99999f);
                            u += id * 10;
                            if (i >= 0 && (int)u > id*10)
                            {
                                Debug.LogError("Float overflow");
                            }
                            lmUVArrays2[i][k * 2] = u;
                        }
                    }
                }

                // Write vbTraceTex
                int numTraceVerts = vbtraceTexUVArray.Count/2;
                for(int i=0; i<numTraceVerts; i++)
                {
                    fvbtraceTex.Write(vbtraceTexPosNormalArray[i * 6]);
                    fvbtraceTex.Write(vbtraceTexPosNormalArray[i * 6 + 1]);
                    fvbtraceTex.Write(vbtraceTexPosNormalArray[i * 6 + 2]);
                    fvbtraceTex.Write(vbtraceTexPosNormalArray[i * 6 + 3]);
                    fvbtraceTex.Write(vbtraceTexPosNormalArray[i * 6 + 4]);
                    fvbtraceTex.Write(vbtraceTexPosNormalArray[i * 6 + 5]);

                    fvbtraceTex.Write(vbtraceTexUVArray[i * 2]);
                    fvbtraceTex.Write(vbtraceTexUVArray[i * 2 + 1]);
                }

                // Generate LOD UV buffer
                if (ftRenderLightmap.giLodModeEnabled)
                {
                    var uvBuffOffsets = new int[lmUVArrays3.Count];
                    var uvBuffLengths = new int[lmUVArrays3.Count];
                    int uvBuffSize = 0;
                    for(int i=0; i< lmUVArrays3.Count; i++)
                    {
                        if (lmUVArrays3[i] == null) continue;
                        uvBuffOffsets[i] = uvBuffSize;
                        uvBuffLengths[i] = lmUVArrays3[i].Length;
                        uvBuffSize += lmUVArrays3[i].Length;
                    }
                    var uvSrcBuff = new float[uvBuffSize];
                    var uvDestBuff = new float[uvBuffSize];
                    for(int i=0; i< lmUVArrays3.Count; i++)
                    {
                        if (lmUVArrays3[i] == null) continue;
                        var arr = lmUVArrays3[i];
                        var arr2 = lmUVArrays2[i];
                        var offset = uvBuffOffsets[i];
                        for(int j=0; j<arr.Length; j++)
                        {
                            uvSrcBuff[j + offset] = arr[j];
                            uvDestBuff[j + offset] = arr2[j];
                        }
                    }
                    var lmrIndicesOffsets = new int[lmIndexArrays2.Count];
                    var lmrIndicesLengths = new int[lmIndexArrays2.Count];
                    int lmrIndicesSize = 0;
                    for(int i=0; i< lmIndexArrays2.Count; i++)
                    {
                        if (lmIndexArrays2[i] == null) continue;
                        lmrIndicesOffsets[i] = lmrIndicesSize;
                        lmrIndicesLengths[i] = lmIndexArrays2[i].Length;
                        lmrIndicesSize += lmIndexArrays2[i].Length;
                    }
                    var lmrIndicesBuff = new int[lmrIndicesSize];
                    for(int i=0; i< lmIndexArrays2.Count; i++)
                    {
                        if (lmIndexArrays2[i] == null) continue;
                        var arr = lmIndexArrays2[i];
                        var offset = lmrIndicesOffsets[i];
                        for(int j=0; j<arr.Length; j++)
                        {
                            lmrIndicesBuff[j + offset] = arr[j];
                        }
                    }

                    for(int s=0; s<sceneCount; s++)
                    {
                        if (storages[s] == null) continue;
                        storages[s].uvBuffOffsets = uvBuffOffsets;
                        storages[s].uvBuffLengths = uvBuffLengths;
                        storages[s].uvSrcBuff = uvSrcBuff;
                        storages[s].uvDestBuff = uvDestBuff;
                        storages[s].lmrIndicesOffsets = lmrIndicesOffsets;
                        storages[s].lmrIndicesLengths = lmrIndicesLengths;
                        storages[s].lmrIndicesBuff = lmrIndicesBuff;

                    }
                    vbtraceTexUVArrayLOD = new float[vbtraceTexUVArray.Count];
                    for(int i=0; i<groupList.Count; i++)
                    {
                        var lmgroup = groupList[i];
                        if (lmgroup.resolution < 128) continue;
                        if (lmgroup.mode == BakeryLightmapGroup.ftLMGroupMode.Vertex || lmgroup.containsTerrains) continue;
                        var remapArray = lmLocalToGlobalIndices[i];
                        var uvArray = lmUVArrays2[i];
                        for(int j=0; j<remapArray.Count; j++)
                        {
                            vbtraceTexUVArrayLOD[remapArray[j]*2] = uvArray[j*2];
                            vbtraceTexUVArrayLOD[remapArray[j]*2+1] = uvArray[j*2+1];
                        }
                    }
                }

                // Write tracing index buffer
                fib32.Write(indicesOpaque.Count); // firstAlphaTriangle
                for(int i=0; i<indicesOpaque.Count; i++) fib32.Write(indicesOpaque[i]); // opaque triangles
                for(int i=0; i<indicesTransparent.Count; i++) fib32.Write(indicesTransparent[i]); // alpha triangles

                // Write scene LOD tracing index buffers
                for(int lod=0; lod<sceneLodsUsed; lod++)
                {
                    var indicesOpaqueArray = indicesOpaqueLOD[lod];
                    var indicesTransparentArray = indicesTransparentLOD[lod];
                    fib32lod[lod].Write(indicesOpaqueArray.Count);
                    for(int i=0; i<indicesOpaqueArray.Count; i++) fib32lod[lod].Write(indicesOpaqueArray[i]); // opaque triangles
                    for(int i=0; i<indicesTransparentArray.Count; i++) fib32lod[lod].Write(indicesTransparentArray[i]); // alpha triangles
                }

                Debug.Log("Wrote binaries in " + ((GetTime() - totalTime)/1000.0) + "s");
                Debug.Log("VB read time " + (vbTimeRead/1000.0) + "s");
                Debug.Log("VB write time " + (vbTimeWrite/1000.0) + "s");
                Debug.Log("VB write time (full) " + (vbTimeWriteFull/1000.0) + "s");
                Debug.Log("VB write time (trace) " + (vbTimeWriteT/1000.0) + "s");
                Debug.Log("VB write time (trace tex) " + (vbTimeWriteT2/1000.0) + "s");
                Debug.Log("VB write time (UV0) " + (vbTimeWriteT3/1000.0) + "s");
                Debug.Log("IB time " + (ibTime/1000.0) + "s");


                fscene.Write(objsToWrite.Count);
                int meshID = 0;
                foreach(var obj in objsToWrite) {
                    fscene.Write(meshID);
                    meshID++;
                }
                foreach(var obj in objsToWrite) {
                    fscene.Write(obj.name);
                }

                fscene.Close();
                fmesh.Close();
                flmid.Close();
                fseamfix.Close();
                fsurf.Close();
                fmatid.Close();
                fmatide.Close();
                fmatideb.Close();
                fmatidh.Close();
                fvbfull.Close();
                fvbtrace.Close();
                fvbtraceTex.Close();
                fvbtraceUV0.Close();
                fib.Close();
                fib32.Close();
                falphaid.Close();
                fhmaps.Close();

                if (fib32lod != null)
                {
                    for(int i=0; i<fib32lod.Length; i++) fib32lod[i].Close();
                }
                if (falphaidlod != null)
                {
                    for(int i=0; i<falphaidlod.Length; i++) falphaidlod[i].Close();
                }

                if (modifyLightmapStorage)
                {
                    for(int s=0; s<sceneCount; s++)
                    {
                        if (storages[s] == null) continue;
                        storages[s].bounds = lmBounds;
                    }
                }
            //}

            startMsU = GetTime();
        }
        catch
        {
            DebugLogError("Error exporting scene - see console for details");
            CloseAllFiles();
            userCanceled = true;
            ProgressBarEnd();
            throw;
        }

        if (exportShaderColors && renderTextures)
        {
            yield return null;
            ProgressBarShow("Exporting scene - shaded surface colors...", 0.55f);
            for(int g=0; g<groupList.Count; g++)
            {
                var hasEmissive = storage.hasEmissive.Count > groupList[g].id && storage.hasEmissive[groupList[g].id];
                bool vertexBake = groupList[g].mode == BakeryLightmapGroup.ftLMGroupMode.Vertex;

                int res = groupList[g].resolution;
                if (vertexBake)
                {
                    if (groupList[g].totalVertexCount == 0)
                    {
                        DebugLogError("Vertex lightmap group " + groupList[g].name + " has 0 static vertices. Make sure objects inside the group don't all have Scale In Lightmap == 0.");
                        CloseAllFiles();
                        userCanceled = true;
                        ProgressBarEnd();
                        yield break;
                    }
                    int atlasTexSize = (int)Mathf.Ceil(Mathf.Sqrt((float)groupList[g].totalVertexCount));
                    atlasTexSize = (int)Mathf.Ceil(atlasTexSize / (float)ftRenderLightmap.tileSize) * ftRenderLightmap.tileSize;
                    res = atlasTexSize;
                }

                var bakeWithNormalMaps = (groupList[g].renderDirMode == BakeryLightmapGroup.RenderDirMode.BakedNormalMaps) ?
                    true : (ftRenderLightmap.renderDirMode == ftRenderLightmap.RenderDirMode.BakedNormalMaps);

                if (groupList[g].probes) bakeWithNormalMaps = false;

                ftUVGBufferGen.StartUVGBuffer(res, hasEmissive, bakeWithNormalMaps);
                for(int i=0; i<objsToWrite.Count; i++)
                {
                    var obj = objsToWrite[i];
                    var lmgroup = objsToWriteGroup[i];
                    if (lmgroup == null) continue;
                    if (lmgroup.id != groupList[g].id) continue;
                    ftUVGBufferGen.RenderUVGBuffer(obj.GetComponent<MeshFilter>().sharedMesh,
                        obj.GetComponent<MeshRenderer>().sharedMaterials,
                        objsToWriteScaleOffset[i],
                        obj.transform.localToWorldMatrix,
                        vertexBake,
                        objsToWriteUVOverride[i],
                        bakeWithNormalMaps && !exportTerrainAsHeightmap && obj.name == "__ExportTerrain");
                }
                ftUVGBufferGen.EndUVGBuffer();

                var albedo = ftUVGBufferGen.texAlbedo;
                var emissive = ftUVGBufferGen.texEmissive;
                var normal = ftUVGBufferGen.texNormal;
                if (hasEmissive)
                {
                    //albedo = ftUVGBufferGen.GetAlbedoWithoutEmissive(ftUVGBufferGen.texAlbedo, ftUVGBufferGen.texEmissive);
                    //if ((unityVersionMajor == 2017 && unityVersionMinor < 2) || unityVersionMajor < 2017)
                    //{
#if UNITY_2017_2_OR_NEWER
#else
                        // Unity before 2017.2: emissive packed to RGBM
                        // Unity after 2017.2: linear emissive
                        emissive = ftUVGBufferGen.DecodeFromRGBM(emissive);
#endif
                    //}
                    if (ftRenderLightmap.hackEmissiveBoost != 1.0f)
                    {
                        ftUVGBufferGen.Multiply(emissive, ftRenderLightmap.hackEmissiveBoost);
                    }
                    if (!vertexBake) ftUVGBufferGen.Dilate(emissive);
                }
                if (!vertexBake) ftUVGBufferGen.Dilate(albedo);

                SaveGBufferMap(albedo.GetNativeTexturePtr(),
                    scenePath + "/uvalbedo_" + groupList[g].name + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"),
                    ftRenderLightmap.compressedGBuffer);
                GL.IssuePluginEvent(5);
                //if (g==2) storage.debugTex = emissive;
                yield return null;

                if (hasEmissive)
                {
                    SaveGBufferMap(emissive.GetNativeTexturePtr(),
                        scenePath + "/uvemissive_" + groupList[g].name + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"),
                        ftRenderLightmap.compressedGBuffer);
                    GL.IssuePluginEvent(5);
                    yield return null;
                    if (ftRenderLightmap.clientMode) ftClient.serverFileList.Add("uvemissive_" + groupList[g].name + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"));
                }

                if (bakeWithNormalMaps)
                {
                    SaveGBufferMap(normal.GetNativeTexturePtr(),
                        scenePath + "/uvnormal_" + groupList[g].name + (ftRenderLightmap.compressedGBuffer ? ".lz4" : ".dds"),
                        ftRenderLightmap.compressedGBuffer);
                    GL.IssuePluginEvent(5);
                    yield return null;
                }
            }
        }

        ProgressBarShow(exportShaderColors ? "Exporting scene - alpha buffer..." : "Exporting scene - UV GBuffer and alpha buffer...", 0.55f);
        if (userCanceled)
        {
            CloseAllFiles();
            userCanceled = true;
            ProgressBarEnd();
            yield break;
        }
        yield return null;

        InitShaders();
        LoadScene(scenePath);

        // Force load textures to VRAM
        var forceRt = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var forceTex = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
        if (!exportShaderColors)
        {
            for(int i=0; i<lmAlbedoListTex.Count; i++)
            {
                Graphics.Blit(lmAlbedoListTex[i] as Texture2D, forceRt);
                Graphics.SetRenderTarget(forceRt);
                forceTex.ReadPixels(new Rect(0,0,1, 1), 0, 0, true);
                forceTex.Apply();
                lmAlbedoList[i] = lmAlbedoListTex[i].GetNativeTexturePtr();
            }
        }
        for(int i=0; i<lmAlphaListTex.Count; i++)
        {
            Graphics.Blit(lmAlphaListTex[i] as Texture2D, forceRt);
            Graphics.SetRenderTarget(forceRt);
            forceTex.ReadPixels(new Rect(0,0,1, 1), 0, 0, true);
            forceTex.Apply();
            lmAlphaList[i] = lmAlphaListTex[i].GetNativeTexturePtr();
        }

        if (exportShaderColors)
        {
            if (terrainObjectToActual.Count > 0)
            {
                //for(int i=0; i<heightmapListTex.Count; i++)
                terrainObjectToHeightMapPtr = new IntPtr[terrainObjectToHeightMap.Count];
                for(int i=0; i<terrainObjectToHeightMap.Count; i++)
                {
                    Graphics.Blit(terrainObjectToHeightMap[i] as Texture2D, forceRt);
                    Graphics.SetRenderTarget(forceRt);
                    forceTex.ReadPixels(new Rect(0,0,1, 1), 0, 0, true);
                    forceTex.Apply();
                    terrainObjectToHeightMapPtr[i] = terrainObjectToHeightMap[i].GetNativeTexturePtr();
                }
                SetAlbedos(terrainObjectToHeightMap.Count, terrainObjectToHeightMapPtr);
                int cerr = CopyAlbedos();
                if (cerr != 0)
                {
                    DebugLogError("Failed to copy textures (" + cerr + ")");
                    CloseAllFiles();
                    userCanceled = true;
                    ProgressBarEnd();
                    yield break;
                }
            }
            else
            {
                SetAlbedos(0, null);
            }
        }
        else
        {
            SetAlbedos(lmAlbedoList.Count, lmAlbedoList.ToArray());
        }
        SetAlphas(lmAlphaList.Count, lmAlphaList.ToArray(), lmAlphaRefList.ToArray(), sceneLodsUsed);

        GL.IssuePluginEvent(6); // render alpha buffer
        int uerr = 0;
        while(uerr == 0)
        {
            uerr = GetABGErrorCode();
            yield return null;
        }

        /*yield return new WaitForEndOfFrame();
        int uerr = ftGenerateAlphaBuffer();*/
        if (uerr != 0 && uerr != 99999)
        {
            DebugLogError("ftGenerateAlphaBuffer error: " + uerr);
            CloseAllFiles();
            userCanceled = true;
            ProgressBarEnd();
            yield break;
        }

        if (!renderTextures)
        {
            ProgressBarEnd(false);
            yield break;
        }

        //ProgressBarShow("Exporting scene - UV GBuffer...", 0.8f);
        //if (userCanceled) yield break;
        //yield return null;

        //GL.IssuePluginEvent(1); // render UV GBuffer
        //yield return new WaitForEndOfFrame();

        SetFixPos(false);//true); // do it manually
        SetCompression(ftRenderLightmap.compressedGBuffer);

        if (!exportShaderColors)
        {
            uerr = ftRenderUVGBuffer();
            if (uerr != 0)
            {
                DebugLogError("ftRenderUVGBuffer error: " + uerr);
                CloseAllFiles();
                userCanceled = true;
                ProgressBarEnd();
                yield break;
            }
        }

        ms = GetTime();
        Debug.Log("UVGB/fixPos/alpha time: " + ((ms - startMsU) / 1000.0) + " seconds");

        ProgressBarEnd(false);

        Debug.Log("Finished");
    }

    int countChildrenFlat(Transform tform)
    {
        int count = 1;
        foreach(Transform t in tform)
        {
            count += countChildrenFlat(t);
        }
        return count;
    }
}

#endif
