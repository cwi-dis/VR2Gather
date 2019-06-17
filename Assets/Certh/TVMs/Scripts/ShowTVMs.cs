using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;

public class ShowTVMs : MonoBehaviour {
    uint clientID;
    public string connectionURI;
    public string exchangeName;
    private Config._TVMs cfg;

    public int Packets { get { return totalPackets; } }

    public static int totalPackets = 0;
    public int pps = 0;

    private float timeCounter = 0.0f;
    private static int packetCounter = 0;

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
        public struct Texture
        {
            Texture2D    texture;
            IntPtr       ptrData;
            int          width;
            int          height;
            int          lenght;
            int          parameterID;


            public void Read(ReconstructionReceiver.Texture data)
            {
                if (ptrData == IntPtr.Zero) {
                    width = data.width;
                    height = data.height;
                    lenght = width * height * 3;
                    ptrData = Marshal.AllocHGlobal(lenght);
                }
                CopyMemory(ptrData, data.data, lenght);
            }

            public void Set(int id, Material material)
            {
                if (texture == null) {
                    texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                    texture.wrapMode = TextureWrapMode.Clamp;
                    if (parameterID == 0) parameterID = Shader.PropertyToID("Texture" + id);
                }
                material.SetTexture(parameterID, texture);
                texture.LoadRawTextureData(ptrData, lenght);
                texture.Apply(false);
            }
        };
        Texture[] textures;

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, int count);

        public void Read(ReconstructionReceiver.DMesh mesh) {
            // mesh.nTextures

            IntPtr pointer = mesh.textures;
            if (textures == null) textures = new Texture[mesh.nTextures];
            for (int i = 0; i < textures.Length; ++i) {
                textures[i].Read( Marshal.PtrToStructure< ReconstructionReceiver.Texture>(pointer) );
                pointer = new IntPtr((long)pointer + Marshal.SizeOf(typeof(ReconstructionReceiver.Texture)));
            }
        }

        public void Set(Material material) {
            for (int i = 0; i < textures.Length; ++i) 
                textures[i].Set(i, material);
        }
    }

    public class InfoData {
        public struct Matrices
        {
            int intrinsicID;
            int global2LocalID;
            Matrix4x4 Intrinsics;
            Matrix4x4 Global2Local;

            static float[] tmp1 = new float[16];
            static float[] tmp2 = new float[16];


            public void Read(IntPtr pIntrinsics, IntPtr pGlobal2Local)
            {
                Marshal.Copy(pIntrinsics, tmp1, 0, 16);
                Marshal.Copy(pGlobal2Local, tmp2, 0, 16);

                for (int k = 0; k < 4; k++) {
                    // transposed
                    Intrinsics.SetColumn(k, new Vector4(tmp1[0 * 4 + k], tmp1[1 * 4 + k], tmp1[2 * 4 + k], tmp1[3 * 4 + k]));
                    Global2Local.SetColumn(k, new Vector4(tmp2[0 * 4 + k], tmp2[1 * 4 + k], tmp2[2 * 4 + k], tmp2[3 * 4 + k]));
                }
            }

            public void Set(int id, Material material)
            {
                if (global2LocalID == 0) {
                    global2LocalID = Shader.PropertyToID("Global2Local" + id);
                    intrinsicID = Shader.PropertyToID("Intrinsics" + id);
                }
                material.SetMatrix(global2LocalID, Global2Local);
                material.SetMatrix(intrinsicID, Intrinsics);
            }

        }
        Matrices[] matrices;
        public void Read(ReconstructionReceiver.DMesh mesh){
            // mesh.acquisitionTimestamp;
            // mesh.kinectTimestamp;

            if (matrices == null) matrices = new Matrices[mesh.nTextures];

            IntPtr[] pIntrinsics = new IntPtr[mesh.nTextures];
            IntPtr[] pGlobal2Local = new IntPtr[mesh.nTextures];
            Marshal.Copy(mesh.intrinsics, pIntrinsics, 0, mesh.nTextures); // Copy de nTextures pointers to the matrix.
            Marshal.Copy(mesh.global2LocalColor, pGlobal2Local, 0, mesh.nTextures);

            for (int i = 0; i < matrices.Length; i++)
                matrices[i].Read(pIntrinsics[i], pGlobal2Local[i]);
        }

        public void Set(Material material) {
            for (int i = 0; i < matrices.Length; i++)
                matrices[i].Set(i, material);
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

    public static List<TVMMeshData> meshDatas = new List<TVMMeshData>();

    private static void OnMeshReceivedGlobalHandler(uint cID, ReconstructionReceiver.DMesh mesh) {
        lock (meshDatas) {
            for (int i = 0; i < meshDatas.Count; ++i)
                if (meshDatas[i].id == cID) {
                    meshDatas[i].Read(mesh);
                    //++packetCounter;
                    //++totalPackets;
                    Debug.Log("TVM TimeStamp: " + mesh.acquisitionTimestamp.ToString());
                }
        }
    }

    private static void OnConnectionErrorHandler(uint cID) {
        Debug.Log("OnConnectionErrorHandler " + cID);
    }

    void Awake() {
        if(TVMInstances==0) ReconstructionReceiver.Init();
        clientID = (uint)ReconstructionReceiver.AddClient();
        TVMInstances++;
        cfg = Config.Instance.TVMs;
    }

    // Use this for initialization
    void Start() {
        ReconstructionReceiver.SetConnectionURI(clientID, connectionURI);
        ReconstructionReceiver.SetExchangeName(clientID, exchangeName);
        ReconstructionReceiver.RegisterOnReceivedMeshCallBack(clientID, new ReconstructionReceiver.OnReceivedMesh(OnMeshReceivedGlobalHandler));
        ReconstructionReceiver.RegisterOnConnectionErrorCallBack(clientID, new ReconstructionReceiver.OnConnectionError(OnConnectionErrorHandler));
        ReconstructionReceiver.StartClient(clientID);
        meshDatas.Add( new TVMMeshData(gameObject, shader) { id = clientID } );
        // TVM Calibration
        gameObject.transform.localPosition = cfg.offsetPosition;
        gameObject.transform.localRotation = Quaternion.Euler(cfg.offsetRotation);
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

        //CalculatePackets();
    }

    private void CalculatePackets() {
        if (timeCounter <= 1.0f) {
            timeCounter += Time.deltaTime;
        }
        else {
            pps = packetCounter / (int)timeCounter;
            timeCounter = 0.0f;
            packetCounter = 0;
        }
    }
}
