using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class BakeryLightMesh : MonoBehaviour
{
    public int UID;
    public static List<MeshFilter> All = new List<MeshFilter>();

    public Color color = Color.white;
    public float intensity = 1.0f;
    public Texture2D texture = null;
    public float cutoff = 100;
    public int samples = 256;
    public int samples2 = 16;
    public int bitmask = 1;
    public bool selfShadow = true;
    public bool bakeToIndirect = true;
    public float indirectIntensity = 1.0f;

    public int lmid = -2;

    public static int lightsChanged = 0;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (lightsChanged == 0) lightsChanged = 1;
    }

    void Start()
    {
        if (gameObject.GetComponent<BakeryDirectLight>() != null ||
            gameObject.GetComponent<BakeryPointLight>() != null ||
            gameObject.GetComponent<BakerySkyLight>() != null)
        {
            EditorUtility.DisplayDialog("Bakery", "Can't have more than one Bakery light on one object", "OK");
            DestroyImmediate(this);
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        All.Add(gameObject.GetComponent<MeshFilter>());

        if (UID == 0) UID = Guid.NewGuid().GetHashCode();
        ftUniqueIDRegistry.Register(UID, gameObject.GetInstanceID());
    }

    void OnDestroy()
    {
        if (UID == 0) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        ftUniqueIDRegistry.Deregister(UID);
    }

    void Update()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!ftUniqueIDRegistry.Mapping.ContainsKey(UID)) ftUniqueIDRegistry.Register(UID, gameObject.GetInstanceID());
        if (gameObject.GetInstanceID() != ftUniqueIDRegistry.GetInstanceId(UID))
        {
            UID = Guid.NewGuid().GetHashCode();
            ftUniqueIDRegistry.Register(UID, gameObject.GetInstanceID());
        }
    }
#endif

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
        var mr = gameObject.GetComponent<MeshRenderer>();
        if (mr!=null) Gizmos.DrawWireSphere(mr.bounds.center, cutoff);
	}
}



