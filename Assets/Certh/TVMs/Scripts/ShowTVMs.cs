using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class ShowTVMs : NetworkBehaviour {
    uint clientID;
    [SyncVar]
    public string connectionURI;
    public string exchangeName;

    public class MeshData {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Color[] Attribs;
        public int[] Indices;
        
        public void Read(ReconstructionReceiver.DMesh mesh) {
            IntPtr pVertices = mesh.vertices;
            Vertices = new Vector3[mesh.nVertices];
            Normals = new Vector3[mesh.nVertices];
            Attribs = new Color[mesh.nVertices];

            for (int i = 0; i < mesh.nVertices; i++) {
                ReconstructionReceiver.Vertex vert = (ReconstructionReceiver.Vertex)Marshal.PtrToStructure(pVertices, typeof(ReconstructionReceiver.Vertex));
                Vertices[i] = new Vector3(vert.x, vert.y, -vert.z);
                Normals[i] = new Vector3(vert.nx, vert.ny, -vert.nz);
                Attribs[i] = new Color(vert.tid1, vert.tid2, vert.w);

                pVertices = new IntPtr((long)pVertices + Marshal.SizeOf(typeof(ReconstructionReceiver.Vertex)));
            }

            IntPtr pIndices = mesh.indices;
            Indices = new int[mesh.nTriangles * 3];
            Marshal.Copy(pIndices, Indices, 0, mesh.nTriangles * 3);
        }

        public void Set(Mesh mesh) {
            mesh.vertices = Vertices;
            mesh.normals = Normals;
            mesh.colors = Attribs;
            mesh.SetIndices(Indices, MeshTopology.Triangles, 0);
        }
    }

    public class TextureData {
        Texture2D texture;
        public byte[] texData;
        public int textureWidth;
        public int textureHeight;

        public void Read(ReconstructionReceiver.DMesh mesh) {
            IntPtr pointer = mesh.textures;
            var textureData = (ReconstructionReceiver.Texture)Marshal.PtrToStructure(pointer, typeof(ReconstructionReceiver.Texture));
            if (textureWidth != textureData.width && textureHeight != textureData.height) {
                textureWidth = textureData.width;
                textureHeight = textureData.height;
                texData = new byte[textureWidth * textureHeight * 3];
            }
            Marshal.Copy(textureData.data, texData, 0, textureData.width * textureData.height * 3);
        }

        public void Set(Material material) {
            if (texture != null && (texture.width != textureWidth || texture.height != textureHeight)) {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (texture == null) {
                texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
                material.SetTexture("Texture", texture);
                material.SetInt("TextureWidth", textureWidth);
                material.SetInt("TextureHeight", textureHeight);
            }
            texture.LoadRawTextureData(texData);
            texture.Apply(false);
        }
    }

    public class InfoData {
        Matrix4x4 ColorIntrinsics;
        Matrix4x4 Global2Color;
        public void Read(ReconstructionReceiver.DMesh mesh)
        {
            // mesh.acquisitionTimestamp;
            // mesh.kinectTimestamp;

            IntPtr[] pIntrinsics = new IntPtr[1];
            IntPtr[] pGlobal2LocalColor = new IntPtr[1];

            Marshal.Copy(mesh.intrinsics, pIntrinsics, 0, 1);
            Marshal.Copy(mesh.global2LocalColor, pGlobal2LocalColor, 0, 1);

            float[] intrinsics = new float[16];
            float[] global2LocalColor = new float[16];

            Marshal.Copy(pIntrinsics[0], intrinsics, 0, 16);
            Marshal.Copy(pGlobal2LocalColor[0], global2LocalColor, 0, 16);
            Matrix4x4 intr = new Matrix4x4();
            Matrix4x4 g2c = new Matrix4x4();
            for (int k = 0; k < 4; k++) {
                // transposed
                intr.SetColumn(k, new Vector4(intrinsics[0 * 4 + k], intrinsics[1 * 4 + k], intrinsics[2 * 4 + k], intrinsics[3 * 4 + k]));
                g2c.SetColumn(k, new Vector4(global2LocalColor[0 * 4 + k], global2LocalColor[1 * 4 + k], global2LocalColor[2 * 4 + k], global2LocalColor[3 * 4 + k]));
            }
            // convert from meters to mm
            g2c.m03 *= 1000;
            g2c.m13 *= 1000;
            g2c.m23 *= 1000;

            ColorIntrinsics = intr;
            Global2Color = g2c;
        }

        public void Set(Material material) {
            material.SetMatrix("Global2LocalColor", Global2Color);
            material.SetMatrix("Intrinsics", ColorIntrinsics);
        }
    }

    public class TVMMeshData {
        public uint id;
        public bool isNew { get; private set; }

        public MeshData     meshData    = new MeshData();
        public TextureData  textureData = new TextureData();
        public InfoData     infoData    = new InfoData();

        public void Read(ReconstructionReceiver.DMesh mesh) {
            meshData.Read(mesh);
            textureData.Read(mesh);
            infoData.Read(mesh);
            isNew = true;
        }

        Mesh            mesh;
        MeshFilter      meshFilter;
        MeshRenderer    meshRenderer;
        Material        material;

        public TVMMeshData(GameObject gameObject, Shader shader) {
            mesh = new Mesh();
            mesh.MarkDynamic();
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            material = new Material(shader);
            meshRenderer.material = material;
        }

        public void UpdateMesh() {
            mesh.Clear();

            meshData.Set(mesh);
            textureData.Set(material);
            infoData.Set(material);
            isNew = false;
        }

    }

    static int TVMInstances = 0;

    static List<TVMMeshData> meshDatas = new List<TVMMeshData>();

    private static void OnMeshReceivedGlobalHandler(uint cID, ReconstructionReceiver.DMesh mesh) {
        lock (meshDatas) {
            for( int i=0;i< meshDatas.Count;++i)
                if(meshDatas[i].id == cID)
                    meshDatas[i].Read(mesh);
        }
    }

    private static void OnConnectionErrorHandler(uint cID) {
        Debug.Log("OnConnectionErrorHandler " + cID);
    }

    void Awake() {
        if(TVMInstances==0) ReconstructionReceiver.Init();
        clientID = (uint)ReconstructionReceiver.AddClient();
        TVMInstances++;
    }

    // Use this for initialization
    void Start() {
        ReconstructionReceiver.SetConnectionURI(clientID, connectionURI);
        ReconstructionReceiver.SetExchangeName(clientID, exchangeName);
        Debug.Log("Start: " + connectionURI);
        ReconstructionReceiver.RegisterOnReceivedMeshCallBack(clientID, new ReconstructionReceiver.OnReceivedMesh(OnMeshReceivedGlobalHandler));
        ReconstructionReceiver.RegisterOnConnectionErrorCallBack(clientID, new ReconstructionReceiver.OnConnectionError(OnConnectionErrorHandler));
        ReconstructionReceiver.StartClient(clientID);
        meshDatas.Add( new TVMMeshData(gameObject, shader) { id = clientID } );
    }

    void OnDisable() {
        Close();
    }

    public void Close() {
        ReconstructionReceiver.RegisterOnReceivedMeshCallBack(clientID, null);
        ReconstructionReceiver.RegisterOnConnectionErrorCallBack(clientID, null);
        ReconstructionReceiver.StopClient(clientID);
        TVMInstances--;
        if(TVMInstances==0) ReconstructionReceiver.Shutdown();
    }

    public Shader shader;
    private void Update() {
        lock (meshDatas) {
            for (int i = 0; i < meshDatas.Count; ++i)
                if (meshDatas[i].isNew) 
                    meshDatas[i].UpdateMesh();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }


    private void OnGUI()
    {
        GUILayout.Label((Time.frameCount / Time.time).ToString());
    }


}
