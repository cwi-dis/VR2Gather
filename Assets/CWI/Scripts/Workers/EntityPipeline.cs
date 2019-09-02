using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPipeline : MonoBehaviour {
    Workers.BaseWorker  reader;
    Workers.BaseWorker  codec;
    Workers.BaseWorker  writer;
    Workers.BaseWorker  preparer;
    MonoBehaviour       render;

    // Start is called before the first frame update
    public EntityPipeline Init(Config._User cfg, Transform parent) {
        if (cfg.Render.forceMesh || SystemInfo.graphicsShaderLevel < 50)
        { // Mesh
            preparer = new Workers.MeshPreparer();
            render = gameObject.AddComponent<Workers.PointMeshRenderer>();
            ((Workers.PointMeshRenderer)render).preparer = (Workers.MeshPreparer)preparer;
        }
        else
        { // Buffer
            preparer = new Workers.BufferPreparer();
            render = gameObject.AddComponent<Workers.PointBufferRenderer>();
            ((Workers.PointBufferRenderer)render).preparer = (Workers.BufferPreparer)preparer;
        }

        int forks = 1;
        switch (cfg.sourceType) {
            case "pcself": // old "rs2"

                reader = new Workers.RS2Reader(cfg.PCSelfConfig);
                reader.AddNext(preparer).AddNext(reader); // <- local render tine.

                try
                {
                    codec = new Workers.PCEncoder(cfg.PCSelfConfig.Encoder);
                }
                catch (System.EntryPointNotFoundException)
                {
                    Debug.LogError("EntityPipeline: PCEncoder() raised EntryPointNotFound exception, skipping voice encoding");
                }
                try
                {
                    writer = new Workers.B2DWriter(cfg.PCSelfConfig.Bin2Dash);
                }
                catch (System.EntryPointNotFoundException)
                {
                    Debug.LogError("EntityPipeline: B2DWriter() raised EntryPointNotFound exception, skipping voice encoding");
                }
                if (codec != null && writer != null)
                {
                    reader.AddNext(codec).AddNext(writer).AddNext(reader); // <- encoder and bin2dash tine.
                    forks = 2;
                }
                try
                {
                    gameObject.AddComponent<VoiceDashSender>().Init(cfg.PCSelfConfig.AudioBin2Dash);
                }
                catch (System.EntryPointNotFoundException e)
                {
                    Debug.LogError("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding");
                }
                break;
            case "pcsub":
                reader = new Workers.SUBReader(cfg.SUBConfig);
                codec  = new Workers.PCDecoder();
                reader.AddNext(codec).AddNext(preparer).AddNext(reader);
                if (cfg.AudioSUBConfig != null)
                    gameObject.AddComponent<VoiceDashReceiver>().Init(cfg.AudioSUBConfig);
                break;
            case "net":
                reader = new Workers.NetReader(cfg.NetConfig);
                codec = new Workers.PCDecoder();
                reader.AddNext(codec).AddNext(preparer).AddNext(reader);
                break;
        }



        if(reader!=null) reader.token = new Workers.Token(forks);

        transform.parent = parent;
        transform.position = new Vector3(cfg.Render.position.x, cfg.Render.position.y, cfg.Render.position.z);
        transform.rotation = Quaternion.Euler(cfg.Render.rotation);
        transform.localScale = cfg.Render.scale;
        return this;
    }

    // Start is called before the first frame update
    public EntityPipeline Init(Config._User cfg, Transform parent, string _url) {
        if (cfg.Render.forceMesh || SystemInfo.graphicsShaderLevel < 50) { // Mesh
            preparer = new Workers.MeshPreparer();
            render = gameObject.AddComponent<Workers.PointMeshRenderer>();
            ((Workers.PointMeshRenderer)render).preparer = (Workers.MeshPreparer)preparer;
        }
        else { // Buffer
            preparer = new Workers.BufferPreparer();
            render = gameObject.AddComponent<Workers.PointBufferRenderer>();
            ((Workers.PointBufferRenderer)render).preparer = (Workers.BufferPreparer)preparer;
        }

        int forks = 1;
        switch (cfg.sourceType) {
            case "pcself": // old "rs2"
                
                reader = new Workers.RS2Reader(cfg.PCSelfConfig);
                reader.AddNext(preparer).AddNext(reader); // <- local render tine.

                try {
                    codec = new Workers.PCEncoder(cfg.PCSelfConfig.Encoder);
                }
                catch (System.EntryPointNotFoundException) {
                    Debug.LogError("EntityPipeline: PCEncoder() raised EntryPointNotFound exception, skipping voice encoding");
                }
                try {
                    writer = new Workers.B2DWriter(cfg.PCSelfConfig.Bin2Dash, _url);
                }
                catch (System.EntryPointNotFoundException) {
                    Debug.LogError("EntityPipeline: B2DWriter() raised EntryPointNotFound exception, skipping voice encoding");
                }
                if (codec != null && writer != null) {
                    reader.AddNext(codec).AddNext(writer).AddNext(reader); // <- encoder and bin2dash tine.
                    forks = 2;
                }
                try {                    
                    gameObject.AddComponent<VoiceDashSender>().Init(cfg.PCSelfConfig.AudioBin2Dash, _url);
                }
                catch (System.EntryPointNotFoundException e) {
                    Debug.LogError("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding");
                }
                break;
            case "pcsub":
                reader = new Workers.SUBReader(cfg.SUBConfig, _url);
                codec = new Workers.PCDecoder();
                reader.AddNext(codec).AddNext(preparer).AddNext(reader);
                if (cfg.AudioSUBConfig != null)
                    gameObject.AddComponent<VoiceDashReceiver>().Init(cfg.AudioSUBConfig, _url);
                break;
            case "net":
                reader = new Workers.NetReader(cfg.NetConfig);
                codec = new Workers.PCDecoder();
                reader.AddNext(codec).AddNext(preparer).AddNext(reader);
                break;
        }



        if (reader != null) reader.token = new Workers.Token(forks);

        transform.parent = parent;
        //transform.position = new Vector3(cfg.Render.position.x, cfg.Render.position.y, cfg.Render.position.z);
        //transform.rotation = Quaternion.Euler(cfg.Render.rotation);
        transform.localScale = cfg.Render.scale;
        return this;
    }

    // Update is called once per frame
    void OnDestroy() {
        if (reader != null)     reader.Stop();
        if (codec != null)      codec.Stop();
        if (writer != null)     writer.Stop();
        if (preparer != null)   preparer.Stop();
    }
}
