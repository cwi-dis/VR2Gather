using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using System.Threading.Tasks;
using System.Threading;

public class PointCloudMeshRenderer : PointCloudBaseRenderer {
    Mesh mesh;

    public override void Init(Config._PCs cfg ) {
        InternalInit(cfg, Resources.Load<Shader>("PointCloudMesh") );

        var mf = gameObject.AddComponent<MeshFilter>();
        var mr = gameObject.AddComponent<MeshRenderer>();
        mf.mesh = mesh = new Mesh();
        mf.mesh.MarkDynamic();
        mr.material = material;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    protected override void OnData() { frameReady.getVertexArray(); }                        
    protected override void  OnUpdate() { frameReady.load_to_mesh(ref mesh); }
}
