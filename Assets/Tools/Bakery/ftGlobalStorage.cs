using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ftGlobalStorage : ScriptableObject
{

#if UNITY_EDITOR

    [System.Serializable]
    public struct AdjustedMesh
    {
        //[SerializeField]
        //public string assetPath;

        [SerializeField]
        public List<string> meshName;

        [SerializeField]
        public List<int> padding;
    };

    // UV adjustment

    [SerializeField]
    public List<string> modifiedAssetPathList = new List<string>();

    [SerializeField]
    public List<int> modifiedAssetPaddingHash = new List<int>();

    // Legacy
    [SerializeField]
    public List<Mesh> modifiedMeshList = new List<Mesh>();
    [SerializeField]
    public List<int> modifiedMeshPaddingList = new List<int>();

    [SerializeField]
    public List<AdjustedMesh> modifiedAssets = new List<AdjustedMesh>();

    // UV overlap marks

    [SerializeField]
    public List<string> assetList = new List<string>();

    [SerializeField]
    public List<int> uvOverlapAssetList = new List<int>(); // -1 = no UV1, 0 = no overlap, 1 = overlap


    // Temp

    public Dictionary<string, int> modifiedMeshPaddingMap;

    public void InitModifiedMeshMap(string assetPath) {

        modifiedMeshPaddingMap = new Dictionary<string, int>();
        /*for(int i=0; i<modifiedMeshList.Count; i++) {
            var m = modifiedMeshList[i];
            if (m == null) continue;
            var mpath = AssetDatabase.GetAssetPath(m);
            if (mpath != assetPath) continue;

            modifiedMeshPaddingMap[m.name] = modifiedMeshPaddingList[i];
        }*/
        var index = modifiedAssetPathList.IndexOf(assetPath);
        if (index < 0) return;
        var m = modifiedAssets[index];
        for(int j=0; j<m.meshName.Count; j++)
        {
            modifiedMeshPaddingMap[m.meshName[j]] = m.padding[j];
        }
    }

    public void ConvertFromLegacy()
    {
        for(int a=0; a<modifiedAssetPathList.Count; a++)
        {
            while(modifiedAssets.Count <= a)
            {
                var str = new AdjustedMesh();
                str.meshName = new List<string>();
                str.padding = new List<int>();
                modifiedAssets.Add(str);
            }
            var assetPath = modifiedAssetPathList[a];
            for(int i=0; i<modifiedMeshList.Count; i++) {
                var m = modifiedMeshList[i];
                if (m == null) continue;
                var mpath = AssetDatabase.GetAssetPath(m);
                if (mpath != assetPath) continue;

                modifiedAssets[a].meshName.Add(m.name);
                modifiedAssets[a].padding.Add(modifiedMeshPaddingList[i]);
            }
        }
        modifiedMeshList = new List<Mesh>();
        modifiedMeshPaddingList = new List<int>();
    }

    public int CalculatePaddingHash(int id)
    {
        string s = "";
        var list = modifiedAssets[id].padding;
        for(int i=0; i<list.Count; i++) s += list[i]+"_";
        return s.GetHashCode();
    }

#if UNITY_2017_1_OR_NEWER
    public void SyncModifiedAsset(int index)
    {
        var importer = AssetImporter.GetAtPath(modifiedAssetPathList[index]) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("Can't get importer for " + modifiedAssetPathList[index]);
            return;
        }
        var data = modifiedAssets[index];
        var str = JsonUtility.ToJson(data);
        var props = importer.extraUserProperties;

        // check if Bakery properties already present
        int propID = -1;
        for(int i=0; i<props.Length; i++)
        {
            if (props[i].Substring(0,7) == "#BAKERY")
            {
                propID = i;
                break;
            }
        }

        if (propID < 0)
        {
            // keep existing properties
            var newProps = new string[props.Length + 1];
            for(int i=0; i<props.Length; i++) newProps[i] = props[i];
            props = newProps;
            propID = props.Length - 1;
        }

        props[propID] = "#BAKERY" + str;

        importer.extraUserProperties = props;
    }
#endif

#endif

}

