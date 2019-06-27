using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using System.Threading.Tasks;
using System.Threading;

public class PointCloudBaseRenderer : MonoBehaviour {
    public Shader               shader;
    public Color                pointTint = Color.gray;
    protected bool              stopTask = false;
    protected Material          material;
    protected PointCloudFrame   frameReady = null;
    protected PCBaseReader      currentPCReader;

    public virtual void Init(Config._PCs cfg)
    {
    }

    protected void InternalInit(Config._PCs cfg, Shader pointShader ) {
        transform.position = new Vector3(cfg.Render.position.x, cfg.Render.position.y, cfg.Render.position.z);
        transform.rotation = Quaternion.Euler(cfg.Render.rotation);
        transform.localScale = cfg.Render.scale;

        if (material == null) {
            material = new Material(pointShader);
            material.SetFloat("_PointSize", cfg.Render.pointSize);
            material.SetColor("_Tint", pointTint);
            material.hideFlags = HideFlags.DontSave;
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
        else if (cfg.sourceType == "rs2")
        {
            currentPCReader = new PCRealSense2Reader(cfg);
            if (currentPCReader == null)
            {
                Debug.LogError("Cannot create realsense2 pointcloud reader");
            }
        }
        else if (cfg.sourceType == "net")
        {
            currentPCReader = new PCSocketReader(cfg.NetConfig.hostName, cfg.NetConfig.port);
            if (currentPCReader == null)
                Debug.LogError("Cannot create remote socket pointclud reader");
        }
        else if (cfg.sourceType == "sub") {
            currentPCReader = new PCSUBReader(cfg.SUBConfig);
            if (currentPCReader == null) 
                Debug.LogError("Cannot create remote signals pointcloud reader");
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
            var tmpFrame = currentPCReader.get();
            lock (this)
            {
                if (frameReady == null)
                {
                    frameReady = tmpFrame;
                    if (frameReady != null)
                    {
                        OnData();
                        var ts = frameReady.timestamp();
                        var dif = Time.realtimeSinceStartup - initTime;
                    }
                }
            }
            if (frameReady != null)
                await Task.Delay(1000 / 30);
            else
                await Task.Delay(1);
        }
    }


    protected virtual void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            stopTask = true;
            Application.Quit();
        }

        if (currentPCReader != null && currentPCReader.eof())
            stopTask = true;

        lock (this) {
            if (frameReady != null) {
                OnUpdate();
                frameReady.FreeFrameData();
                frameReady = null;
            }
        }
        if (currentPCReader != null) currentPCReader.update();
    }

    protected virtual void OnUpdate() { }
    protected virtual void OnData() { }

    public virtual void OnDisable() {
        if (currentPCReader != null)
        {
            currentPCReader.free();
            currentPCReader = null;
        }
    }


}
