using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class BakeryDirectLight : MonoBehaviour
{
    public Color color = Color.white;
    public float intensity = 1.0f;
    public float shadowSpread = 0.01f;//0.05f;
    public int samples = 16;
    //public uint bitmask = 1;
    public int bitmask = 1;
    public bool bakeToIndirect = false;
    public bool shadowmask = false;
    public bool shadowmaskDenoise = false;
    public float indirectIntensity = 1.0f;

    public int UID;

#if UNITY_EDITOR
    void Start()
    {
        if (gameObject.GetComponent<BakerySkyLight>() != null ||
            gameObject.GetComponent<BakeryPointLight>() != null ||
            gameObject.GetComponent<BakeryLightMesh>() != null)
        {
            EditorUtility.DisplayDialog("Bakery", "Can't have more than one Bakery light on one object", "OK");
            DestroyImmediate(this);
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
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

    void OnDrawGizmos()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawSphere(transform.position, 0.1f);

      //Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      var endPoint = transform.position + transform.forward * 2;
      Gizmos.DrawLine(transform.position, endPoint);

      //Gizmos.color = Color.blue;
      Gizmos.DrawWireSphere(transform.position, 0.2f);

      Gizmos.DrawLine(endPoint, endPoint + (transform.position + transform.right - endPoint).normalized * 0.5f);
      Gizmos.DrawLine(endPoint, endPoint + (transform.position - transform.right - endPoint).normalized * 0.5f);
      Gizmos.DrawLine(endPoint, endPoint + (transform.position + transform.up - endPoint).normalized * 0.5f);
      Gizmos.DrawLine(endPoint, endPoint + (transform.position - transform.up - endPoint).normalized * 0.5f);
    }

#endif
}

