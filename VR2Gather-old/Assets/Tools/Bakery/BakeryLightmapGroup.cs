using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public struct BakeryLightmapGroupPlain
{
    public string name;
    public int resolution, id, renderMode, renderDirMode, atlasPacker;
    public bool vertexBake;
    public bool containsTerrains;
    public bool probes;
    public bool isImplicit;
    public bool computeSSS;
    public int sssSamples;
    public float sssDensity;
    public float sssR, sssG, sssB;
    public float fakeShadowBias;
    public bool transparentSelfShadow;
    public bool flipNormal;
    public string parentName;
};

[CreateAssetMenu(menuName = "Bakery lightmap group")]
public class BakeryLightmapGroup : ScriptableObject
{
    public enum ftLMGroupMode
    {
        OriginalUV = 0,
        PackAtlas = 1,
        Vertex = 2
    };

    public enum RenderMode
    {
        FullLighting = 0,
        Indirect = 1,
        Shadowmask = 2,
        Subtractive = 3,
        AmbientOcclusionOnly = 4,
        Auto = 1000
    };

    public enum RenderDirMode
    {
        None = 0,
        BakedNormalMaps = 1,
        DominantDirection = 2,
        RNM = 3,
        SH = 4,
        ProbeSH = 5,
        Auto = 1000
    };

    public enum AtlasPacker
    {
        Default = 0,
        xatlas = 1,
        Auto = 1000
    };

    [SerializeField, Range(1, 8192)]
    public int resolution = 512;

    [SerializeField]
    public int bitmask = 1;

    [SerializeField]
    public int id = -1;

    public int sortingID = -1;

    [SerializeField]
    public bool isImplicit = false;

    [SerializeField]
    public float area = 0.0f;

    [SerializeField]
    public int totalVertexCount = 0;

    [SerializeField]
    public int vertexCounter = 0;

    [SerializeField]
    public int sceneLodLevel = -1;

    [SerializeField]
    public string sceneName;

    [SerializeField]
    public bool containsTerrains;

    [SerializeField]
    public bool probes;

    [SerializeField]
    public ftLMGroupMode mode = ftLMGroupMode.PackAtlas;

    [SerializeField]
    public RenderMode renderMode = RenderMode.Auto;

    [SerializeField]
    public RenderDirMode renderDirMode = RenderDirMode.Auto;

    [SerializeField]
    public AtlasPacker atlasPacker = AtlasPacker.Auto;

    //[SerializeField]
    //public bool aoIsThickness = false;

    [SerializeField]
    public bool computeSSS = false;

    [SerializeField]
    public int sssSamples = 16;

    [SerializeField]
    public float sssDensity = 10;

    [SerializeField]
    public Color sssColor = Color.white;

    [SerializeField]
    public float fakeShadowBias = 0.0f;

    [SerializeField]
    public bool transparentSelfShadow = false;

    [SerializeField]
    public bool flipNormal = false;

    [SerializeField]
    public string parentName;

    [SerializeField]
    public string overridePath = "";

    public BakeryLightmapGroupPlain GetPlainStruct()
    {
        BakeryLightmapGroupPlain str;
        str.name = name;
        str.id = id;
        str.resolution = resolution;
        str.vertexBake = mode == ftLMGroupMode.Vertex;
        str.isImplicit = isImplicit;
        str.renderMode = (int)renderMode;
        str.renderDirMode = (int)renderDirMode;
        str.atlasPacker = (int)atlasPacker;
        str.computeSSS = computeSSS;
        str.sssSamples = sssSamples;
        str.sssDensity = sssDensity;
        str.sssR = sssColor.r;
        str.sssG = sssColor.g;
        str.sssB = sssColor.b;
        str.containsTerrains = containsTerrains;
        str.probes = probes;
        str.fakeShadowBias = fakeShadowBias;
        str.transparentSelfShadow = transparentSelfShadow;
        str.flipNormal = flipNormal;
        str.parentName = parentName;
        return str;
    }
}

