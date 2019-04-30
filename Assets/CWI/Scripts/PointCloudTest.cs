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

    void Awake() {
        bUseMesh = SystemInfo.graphicsShaderLevel < 50;
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

    public float pointSize = 0.05f;
    float _pointSize = 0;
    public Color pointTint = Color.white;
    Color _pointTint = Color.clear;

    void Start()
    {
        if (bUseMesh)
        {
            var mf = gameObject.AddComponent<MeshFilter>();
            var mr = gameObject.AddComponent<MeshRenderer>();
            mf.mesh = mesh = new Mesh();
            mf.mesh.MarkDynamic();
            if (pointMaterial == null)
            {
                pointMaterial = new Material(pointShader40);
                pointMaterial.hideFlags = HideFlags.DontSave;
            }
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
        if (Config.Instance.PCs.sourceType == "cwicpcdir")
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
        else if (Config.Instance.PCs.sourceType == "sub")
        {
            pcSource = cwipc_util_pinvoke.sourceFromSUB(Config.Instance.PCs.subURL, Config.Instance.PCs.subStreamNumber);
            if (pcSource == null)
            {
                Debug.LogError("Cannot create signals-unity-bridge pointcloud source");
            }
        }
        else
        {
            Debug.LogError("Unimplemented config.json sourceType: " + Config.Instance.PCs.sourceType);
        }

        asyncTask();
    }

    async Task asyncTask()
    {
        while (!stopTask)
        {
            lock (this)
            {
                if (pc == null)
                {
                    pc = pcSource.get();
                    if (bUseMesh) pc.getVertexArray();
                    else pc.getByteArray();
                    var ts = pc.timestamp(); // 
                }
            }
            await Task.Delay(1000 / 30 );
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
                if (bUseMesh) pc.load_to_mesh(ref mesh);
                else pointCount = pc.load_to_pointbuffer(ref pointBuffer);
                pc.free();
                pc = null;
            }
        }
    }

    public Shader pointShader = null;
    public Shader pointShader40 = null;
    Material pointMaterial;

    void OnRenderObject() {
        if (bUseMesh) return;

        if (pointCount == 0 || pointBuffer ==null || !pointBuffer.IsValid() ) return;

        var camera = Camera.current;
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;

        if (camera.name == "Preview Scene Camera") return;

        // TODO: Do view frustum culling here.

        if (pointMaterial == null) {
            pointMaterial = new Material(pointShader);
            pointMaterial.hideFlags = HideFlags.DontSave;
        }
        pointMaterial.SetBuffer("_PointBuffer", pointBuffer);

        pointMaterial.SetPass(0);
        pointMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
        if (_pointTint != pointTint) { _pointTint = pointTint; pointMaterial.SetColor("_Tint", _pointTint); }
        if (_pointSize != pointSize) { _pointSize = pointSize; pointMaterial.SetFloat("_PointSize", _pointSize); }
        Graphics.DrawProcedural(MeshTopology.Points, pointCount, 1);
    }

}
