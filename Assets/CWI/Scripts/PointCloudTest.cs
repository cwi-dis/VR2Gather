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
    cwipc frameReady = null;
    PCBaseReader currentPCReader;
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

        frameReady = null;
        currentPCReader = null;
        if (cfg.sourceType == "cwicpcdir")
        {
            currentPCReader = new PCCompressedDirectoryReader(cfg.cwicpcDirectory);
            if (currentPCReader == null)
            {
                Debug.LogError("Cannot create compressed directory pointcloud reader");
            }
        }
        else if (cfg.sourceType == "synthetic")
        {
            currentPCReader = new PCSyntheticReader();
            if (currentPCReader == null)
            {
                Debug.LogError("Cannot create synthetic pointcloud reader");
            }
        }
        else if (cfg.sourceType == "realsense2")
        {
            currentPCReader = new PCRealSense2Reader();
            if (currentPCReader == null)
            {
                Debug.LogError("Cannot create realsense2 pointcloud reader");
            }
        }
        else if (cfg.sourceType == "network")
        {
            currentPCReader = new PCSocketReader(cfg.networkHost, cfg.networkPort);
            if (currentPCReader == null)
                Debug.LogError("Cannot create remote socket pointclud reader");
        }
        else if (cfg.sourceType == "sub") {
            currentPCReader = new PCSUBReader(cfg.subURL, cfg.subStreamNumber);
            if (currentPCReader == null) 
                Debug.LogError("Cannot create remote signals pointcloud reader");
        }
        else
        {
            Debug.LogError("Unimplemented config.json sourceType: " + cfg.sourceType);
        }

        asyncTask();
    }

    async Task asyncTask() {
        var initTime = Time.realtimeSinceStartup;
        while (!stopTask) {
            var tmp_pc = currentPCReader.get();
            lock (this)
            {
                if (frameReady == null)
                {
                    frameReady = tmp_pc;
                    if (frameReady!=null)
                    {
                        if (bUseMesh) frameReady.getVertexArray();
                        else frameReady.getByteArray();
                        var ts = frameReady.timestamp(); // 
                        var dif = Time.realtimeSinceStartup - initTime;
                    }
                }
            }
            if (frameReady != null)
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

        if (currentPCReader != null && currentPCReader.eof())
            stopTask = true;

        lock (this) {
            if (frameReady != null) {
                // Copy the pointcloud to a mesh or a pointbuffer
                if (bUseMesh)  frameReady.load_to_mesh(ref mesh);
                else pointCount = frameReady.load_to_pointbuffer(ref pointBuffer);
                frameReady.free();
                frameReady = null;
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

    void OnDisable() {
        if (currentPCReader != null) {
            currentPCReader.free();
            currentPCReader = null;
        }
        if (pointBuffer != null) {
            pointBuffer.Release();
            pointBuffer = null;
        }
    }


}
