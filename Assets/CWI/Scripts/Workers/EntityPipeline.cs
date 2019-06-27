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
    public EntityPipeline Init(Config._PCs cfg, Transform parent) {
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
            case "rs2":
                reader = new Workers.RS2Reader(cfg.Realsense2Config);
                reader.AddNext(preparer).AddNext(reader); // <- local render tine.
                if (cfg.Encoder != null && cfg.Bin2Dash != null) {
                    codec = new Workers.PCEncoder(cfg.Encoder);
                    writer = new Workers.B2DWriter(cfg.Bin2Dash);
                    reader.AddNext(codec).AddNext(writer).AddNext(reader); // <- encoder and bin2dash tine.
                    forks = 2;
                }
                break;
            case "sub":
                reader = new Workers.SUBReader(cfg.SUBConfig);
                codec  = new Workers.PCDecoder();
                reader.AddNext(codec).AddNext(preparer).AddNext(reader);
                break;
            case "net":
                reader = new Workers.NetReader(cfg.NetConfig);
                codec = new Workers.PCDecoder();
                reader.AddNext(codec).AddNext(preparer).AddNext(reader);
                break;
        }



        reader.token = new Workers.Token(forks);

        transform.parent = parent;
        transform.position = new Vector3(cfg.Render.position.x, cfg.Render.position.y, cfg.Render.position.z);
        transform.rotation = Quaternion.Euler(cfg.Render.rotation);
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
