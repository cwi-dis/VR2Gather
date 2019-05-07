using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using System.Threading.Tasks;
using System.Threading;

public class PointCloudTest : MonoBehaviour {

    bool bUseMesh;
    ComputeBuffer pointBuffer;
    int pointCount = 0;
    Mesh mesh;
    cwipc pc = null;
    cwipc_source pcSource;
    float pcSourceStartTime;
    bool stopTask = false;
    public Color pointTint = Color.gray;

    Material pointMaterial;


    public void Init(Config._PCs cfg, Shader pointShader ) {
        bUseMesh = cfg.forceMesh || SystemInfo.graphicsShaderLevel < 50;

        transform.position = new Vector3(cfg.position.x, cfg.position.y, cfg.position.z);
        transform.rotation = Quaternion.Euler(cfg.rotation);
        transform.localScale = cfg.scale;

        if (pointMaterial == null) {
            pointMaterial = new Material(pointShader);
            pointMaterial.SetFloat("_PointSize", cfg.pointSize);
            pointMaterial.SetColor("_Tint", pointTint);
            pointMaterial.hideFlags = HideFlags.DontSave;
        }

        if (bUseMesh) {
            var mf = gameObject.AddComponent<MeshFilter>();
            var mr = gameObject.AddComponent<MeshRenderer>();
            mf.mesh = mesh = new Mesh();
            mf.mesh.MarkDynamic();
            mr.material = pointMaterial;

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        pc = null;
        pcSource = null;
        /*
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
        else */
        if (cfg.sourceType == "cwicpcdir")
        {
            pcSource = cwipc_util_pinvoke.sourceFromCompressedDir(cfg.cwicpcDirectory);
            if (pcSource == null)
            {
                Debug.LogError("Cannot create compressed directory pointcloud source");
            }
        }
        else if (cfg.sourceType == "plydir")
        {
            pcSource = cwipc_util_pinvoke.sourceFromPlyDir(cfg.cwicpcDirectory);
            if (pcSource == null)
            {
                Debug.LogError("Cannot create ply directory pointcloud source");
            }
        }
        else if (cfg.sourceType == "synthetic")
        {
            pcSource = cwipc_util_pinvoke.sourceFromSynthetic();
            if (pcSource == null)
            {
                Debug.LogError("Cannot create synthetic pointcloud source");
            }
        }
        else if (cfg.sourceType == "realsense2")
        {
            pcSource = cwipc_util_pinvoke.sourceFromRealsense2();
            if (pcSource == null)
            {
                Debug.LogError("Cannot create realsense2 pointcloud source");
            }
        }
        else if (cfg.sourceType == "network")
        {
            pcSource = cwipc_util_pinvoke.sourceFromNetwork(cfg.networkHost, cfg.networkPort);
            if (pcSource == null)
            {
                Debug.LogError("Cannot create compressed network pointcloud source");
            }
        }
        else if (cfg.sourceType == "sub")
        {
            pcSource = cwipc_util_pinvoke.sourceFromSUB(cfg.subURL, cfg.subStreamNumber);
            if (pcSource == null)
            {
                Debug.LogError("Cannot create signals-unity-bridge pointcloud source");
            }
        }
        else
        {
            Debug.LogError("Unimplemented config.json sourceType: " + cfg.sourceType);
        }

        asyncTask();
    }

    async Task asyncTask()
    {
        var initTime = Time.realtimeSinceStartup;
        while (!stopTask)
        {
            var tmp_pc = pcSource.get();
            lock (this)
            {
                if (pc == null)
                {
                    pc = tmp_pc;
                    if (pc!=null)
                    {
                        if (bUseMesh) pc.getVertexArray();
                        else pc.getByteArray();
                        var ts = pc.timestamp(); // 
                        var dif = Time.realtimeSinceStartup - initTime;
                    }
                }
            }
            if (pc != null)
                await Task.Delay(1000 / 30 );
            else
                await Task.Delay(1);
        }
    }
    
    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            stopTask = true;
            Application.Quit();
        }

        if (pcSource != null && pcSource.eof())
            stopTask = true;

        lock (this)
        {
            if (pc != null)
            {
                // Copy the pointcloud to a mesh or a pointbuffer
                if (bUseMesh)  pc.load_to_mesh(ref mesh);
                else pointCount = pc.load_to_pointbuffer(ref pointBuffer);
                pc.free();
                pc = null;
            }
        }
    }


    void OnRenderObject() {
        if (bUseMesh) return;

        if (pointCount == 0 || pointBuffer ==null || !pointBuffer.IsValid() ) return;

        var camera = Camera.current;
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;

        if (camera.name == "Preview Scene Camera") return;

        // TODO: Do view frustum culling here.

        pointMaterial.SetBuffer("_PointBuffer", pointBuffer);

        pointMaterial.SetPass(0);
        pointMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
        
        Graphics.DrawProcedural(MeshTopology.Points, pointCount, 1);
    }

    void OnDisable()
    {
        if (pointBuffer != null)
        {
            pointBuffer.Release();
            pointBuffer = null;
        }
        if (pcSource != null)
        {
            pcSource.free();
            pcSource = null;
        }
    }


}
