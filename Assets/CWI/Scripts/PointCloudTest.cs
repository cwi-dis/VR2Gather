using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;


public class PointCloudTest : MonoBehaviour {

    ComputeBuffer pointBuffer;
    Mesh mesh;
    cwipc pc;
    cwipc_source pcSource;
    float pcSourceStartTime;

    void OnDisable() {
        if (pointBuffer != null) {
            pointBuffer.Release();
            pointBuffer = null;
        }
    }

    public float pointSize = 0.05f;
    float _pointSize = 0;
    public Color pointTint = Color.white;
    Color _pointTint = Color.clear;

    IEnumerator Start()
    {
        if (SystemInfo.graphicsShaderLevel < 50)
        {
            var mf = gameObject.AddComponent<MeshFilter>();
            var mr = gameObject.AddComponent<MeshRenderer>();
            mf.mesh = mesh = new Mesh();
            if (pointMaterial == null)
            {
                pointMaterial = new Material(pointShader40);
                pointMaterial.hideFlags = HideFlags.DontSave;
            }
            mr.material = pointMaterial;

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        yield return null;
        pc = null;
        pcSource = null;
        if (Config.Instance.PCs.sourceType == "cwicpcfile")
        {
            pc = cwipc_util_pinvoke.getOnePointCloudFromCWICPC(Config.Instance.PCs.cwicpcFilename);
            if (pc == null) Debug.LogError("GetPointCloudFromCWICPC did not return a pointcloud");
        }
        else if (Config.Instance.PCs.sourceType == "plyfile")
        {
            pc = cwipc_util_pinvoke.getOnePointCloudFromPly(Config.Instance.PCs.plyFilename);
            if (pc == null) Debug.LogError("GetPointCloudFromPly did not return a pointcloud");
        }
        else if (Config.Instance.PCs.sourceType == "cwicpcdir")
        {
            pcSource = cwipc_util_pinvoke.sourceFromCompressedDir(Config.Instance.PCs.cwicpcDirectory);
            if (pcSource == null)
            {
                Debug.LogError("Cannot create compressed directory pointcloud source");
            }
        }
        else if (Config.Instance.PCs.sourceType == "plydir")
        {
            pcSource = cwipc_util_pinvoke.sourceFromPlyDir(Config.Instance.PCs.cwicpcDirectory);
            if (pcSource == null)
            {
                Debug.LogError("Cannot create ply directory pointcloud source");
            }
        }
        else if (Config.Instance.PCs.sourceType == "synthetic")
        {
            pcSource = cwipc_util_pinvoke.sourceFromSynthetic();
            if (pcSource == null)
            {
                Debug.LogError("Cannot create synthetic pointcloud source");
            }
        }
        else if (Config.Instance.PCs.sourceType == "realsense2")
        {
            pcSource = cwipc_util_pinvoke.sourceFromRealsense2();
            if (pcSource == null)
            {
                Debug.LogError("Cannot create realsense2 pointcloud source");
            }
        }
        else if (Config.Instance.PCs.sourceType == "network")
        {
            pcSource = cwipc_util_pinvoke.sourceFromNetwork(Config.Instance.PCs.networkHost, Config.Instance.PCs.networkPort);
            if (pcSource == null)
            {
                Debug.LogError("Cannot create compressed network pointcloud source");
            }
        }
        else
        {
            Debug.LogError("Unimplemented config.json sourceType: " + Config.Instance.PCs.sourceType);
        }
        if (pcSource != null && pc == null)
        {
            pcSourceStartTime = Time.realtimeSinceStartup;
            // We have a pointcloud source but no pointcloud yet. Get one.
            pc = pcSource.get();
            if (pc == null) Debug.LogError("Cannot get pointcloud from source");
        }
        if (pc != null)
        {
            if (SystemInfo.graphicsShaderLevel < 50)
                pc.copy_to_mesh(ref mesh);
            else
                pc.copy_to_pointbuffer(ref pointBuffer);

        }
    }

    void Update() {
        if ( Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
        // If we have a pointcloud source and it is at end-of-file we delete it
        if (pcSource != null && pcSource.eof())
        {
            Debug.Log("cwipc_source end-of-file. Deleting source.");
            float now = Time.realtimeSinceStartup;
            Debug.Log("cwipc_source produced pointclouds for " + (now - pcSourceStartTime) + " seconds");
            pcSource.free();
            pcSource = null;
        }
        // If we have a pointcloud source and it has a pointcloud available we get the new pointcloud
        if (pcSource != null && pcSource.available(false))
        {
            // Free the previous pointcloud, if there was one
            if (pc != null)
            {
                pc.free();
                pc = null;
            }
            // Get the new pointcloud
            pc = pcSource.get();
            if (pc == null)
            { 
                Debug.LogError("Cannot get pointcloud from source");
            }
            else
            {
                // Copy the pointcloud to a mesh or a pointbuffer
                if (SystemInfo.graphicsShaderLevel < 50)
                    pc.copy_to_mesh(ref mesh);
                else
                    pc.copy_to_pointbuffer(ref pointBuffer);
            }
        }

    }

    public Shader pointShader = null;
    public Shader pointShader40 = null;
    Material pointMaterial;

    void OnRenderObject() {
        if (SystemInfo.graphicsShaderLevel < 50) return;

        if (pointBuffer==null || !pointBuffer.IsValid()) return;

        var camera = Camera.current;
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;

        if (camera.name == "Preview Scene Camera") return;

        // TODO: Do view frustum culling here.

        if (pointMaterial == null) {
            pointMaterial = new Material(pointShader);
            pointMaterial.hideFlags = HideFlags.DontSave;
            pointMaterial.SetBuffer("_PointBuffer", pointBuffer);
        }

        pointMaterial.SetPass(0);
        pointMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
        if (_pointTint != pointTint) { _pointTint = pointTint; pointMaterial.SetColor("_Tint", _pointTint); }
        if (_pointSize != pointSize) { _pointSize = pointSize; pointMaterial.SetFloat("_PointSize", _pointSize); }
        Graphics.DrawProcedural(MeshTopology.Points, pointBuffer.count, 1);
    }

}
