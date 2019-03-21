using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowTVMs : MonoBehaviour
{
    uint clientID;
    public string connectionURI = "amqp://tofis:tofis@127.0.0.1:5672/";
    public string exchangeName  = "transcoder_out";

    private static void OnMeshReceivedGlobalHandler(uint cID, ReconstructionReceiver.DMesh mesh) {
        Debug.Log("OnMeshReceivedGlobalHandler " + cID);
    }

    private static void OnConnectionErrorHandler(uint cID) {
        Debug.Log("OnConnectionErrorHandler " + cID);
    }

    void Awake() {
        ReconstructionReceiver.Init();
        clientID = (uint)ReconstructionReceiver.AddClient();
    }

    // Use this for initialization
    void Start() {
        ReconstructionReceiver.SetConnectionURI(clientID, connectionURI);
        ReconstructionReceiver.SetExchangeName(clientID, exchangeName);
        ReconstructionReceiver.RegisterOnReceivedMeshCallBack(clientID, new ReconstructionReceiver.OnReceivedMesh(OnMeshReceivedGlobalHandler));
        ReconstructionReceiver.RegisterOnConnectionErrorCallBack(clientID, new ReconstructionReceiver.OnConnectionError(OnConnectionErrorHandler));
        ReconstructionReceiver.StartClient(clientID);
    }

    void OnDestroy() {
        Close();
    }

    public void Close() {
        ReconstructionReceiver.RegisterOnReceivedMeshCallBack(clientID, null);
        ReconstructionReceiver.RegisterOnConnectionErrorCallBack(clientID, null);
        ReconstructionReceiver.StopClient(clientID);
    }

    void OnApplicationQuit() {
        ReconstructionReceiver.Shutdown();
    }
}

/*
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml;

public class NetworkStreamReconstructionProvider : MonoBehaviour, IReconstructionProvider
{
    public string m_ConnectionURI = "amqp://aldoum:skeleton@195.251.117.61:5672/";
    public string m_ExchangeName = "3DLive_Reconstruction";
    public string m_ConfigFilename = "config.xml";

    public bool CloneFlag
    {
        get
        {
            return _cloneFlag;
        }

        set
        {
            _cloneFlag = value;
        }
    }

    private bool _cloneFlag = false;
    private uint m_ClientID = 0;
    private static Dictionary<uint, NetworkStreamReconstructionProvider> m_Providers = 
        new Dictionary<uint, NetworkStreamReconstructionProvider>();
    private MeshData m_MeshData = new MeshData();
    private ReconstructedMeshInfo m_MeshInfo = new ReconstructedMeshInfo();
    private volatile bool m_HasNew = false;

    private SyncStreamManager syncStreamManager;

    private static void OnMeshReceivedGlobalHandler(uint cID, ReconstructionReceiver.DMesh mesh)
    {
        m_Providers[cID].OnMeshReceivedHandler(mesh);
    }

    private static void OnConnectionErrorHandler(uint cID)
    {
    }

    private void OnMeshReceivedHandler(ReconstructionReceiver.DMesh mesh)
    {
        lock (m_MeshData)
        {
            m_MeshData.Clear();

            IntPtr pVertices = mesh.vertices;
            for (int i = 0; i < mesh.nVertices; i++)
            {
                ReconstructionReceiver.Vertex vert = (ReconstructionReceiver.Vertex)Marshal
                    .PtrToStructure(pVertices, typeof(ReconstructionReceiver.Vertex));
                m_MeshData.Vertices.Add(new Vector3(vert.x, vert.y, -vert.z));
                m_MeshData.Normals.Add(new Vector3(vert.nx, vert.ny, -vert.nz));
                m_MeshData.VtxAttributes.Add(new Color(vert.tid1, vert.tid2, vert.w));

                pVertices = new IntPtr((long)pVertices + Marshal.SizeOf(
                    typeof(ReconstructionReceiver.Vertex)));
            }

            IntPtr pIndices = mesh.indices;
            int[] indices = new int[mesh.nTriangles * 3];
            Marshal.Copy(pIndices, indices, 0, mesh.nTriangles * 3);
            m_MeshData.Indices.AddRange(indices);

            IntPtr pTextures = mesh.textures;

            for (int i = 0; i < mesh.nTextures; i++)
            {
                ReconstructionReceiver.Texture tex = (ReconstructionReceiver.Texture)Marshal
                    .PtrToStructure(pTextures, typeof(ReconstructionReceiver.Texture));
                if (i == 0)
                {
                    m_MeshData.TextureSize = new Size2D(tex.width, tex.height);
                }
                byte[] texData = new byte[tex.width * tex.height * 3];
                Marshal.Copy(tex.data, texData, 0, tex.width * tex.height * 3);
                m_MeshData.TextureData.Add(texData);
                pTextures = new IntPtr((long)pTextures + Marshal.SizeOf(
                    typeof(ReconstructionReceiver.Texture)));
            }
            m_MeshData.TextureFormat = TextureFormat.RGB24;
            m_MeshInfo.VertexInfo = VertexInfo.Position | VertexInfo.Normal | VertexInfo.UV;
            m_MeshInfo.Viewpoints = new ViewpointInfo[mesh.nTextures];
            
            m_MeshInfo.AcquisitionTimestamp = mesh.acquisitionTimestamp;
            m_MeshInfo.KinectTimestamp = mesh.kinectTimestamp;
            if (syncStreamManager != null)
            {
                syncStreamManager.OnNewTimeStamp("tvmStream", new DateTime(1970, 1, 1, 0, 0, 0, 
                    DateTimeKind.Utc).AddTicks(mesh.acquisitionTimestamp));
            }

            IntPtr[] pIntrinsics = new IntPtr[mesh.nTextures];
            IntPtr[] pGlobal2LocalColor = new IntPtr [mesh.nTextures];

            Marshal.Copy(mesh.intrinsics, pIntrinsics, 0, mesh.nTextures);
            Marshal.Copy(mesh.global2LocalColor, pGlobal2LocalColor, 0, mesh.nTextures);

            float[] intrinsics = new float[16];
            float[] global2LocalColor = new float[16];

            for (int i = 0; i < mesh.nTextures; i++)
            {
                Marshal.Copy(pIntrinsics[i], intrinsics, 0, 16);
                Marshal.Copy(pGlobal2LocalColor[i], global2LocalColor, 0, 16);
                Matrix4x4 intr = new Matrix4x4();
                Matrix4x4 g2c = new Matrix4x4();
                for (int k = 0; k < 4; k++)
                {
                    intr.SetColumn(k, new Vector4(intrinsics[k*4], intrinsics[k*4 + 1], 
                        intrinsics[k*4 + 2], intrinsics[k*4 + 3]));
                    g2c.SetColumn(k, new Vector4(global2LocalColor[k*4], global2LocalColor[k*4 + 1], 
                        global2LocalColor[k*4 + 2], global2LocalColor[k*4 + 3]));
                }
                // convert from meters to mm
                g2c.m30 *= 1000;
                g2c.m31 *= 1000;
                g2c.m32 *= 1000;

                m_MeshInfo.Viewpoints[i] = new ViewpointInfo(intr, g2c);
            }
            m_HasNew = true;
        }
    }

    private void ReadRabbitServerConfig()
    {
        try
        {
            XmlDocument doc = new XmlDocument();
            doc.Load (m_ConfigFilename);

            XmlNode node = doc.SelectSingleNode("/Config/RabbitMQServerURI");
            m_ConnectionURI = node.Attributes["value"].Value;

            node = doc.SelectSingleNode("/Config/ExchangeName");
            m_ExchangeName = node.Attributes["value"].Value;
        }
        catch(Exception)
        {

        }
    }

    void Awake()
    {
        //ReconstructionReceiverWrapper.Load();
        ReconstructionReceiver.Init();
        m_ClientID = (uint)ReconstructionReceiver.AddClient();
        m_Providers.Add(m_ClientID, this);        
    }

    // Use this for initialization
    void Start ()
    {
        syncStreamManager = FindObjectOfType<SyncStreamManager>();

        // ReadRabbitServerConfig();
        ReconstructionReceiver.SetConnectionURI(m_ClientID, m_ConnectionURI);
        ReconstructionReceiver.SetExchangeName(m_ClientID, m_ExchangeName);
        ReconstructionReceiver.RegisterOnReceivedMeshCallBack(m_ClientID, 
            new ReconstructionReceiver.OnReceivedMesh(OnMeshReceivedGlobalHandler));
        ReconstructionReceiver.RegisterOnConnectionErrorCallBack(m_ClientID, 
            new ReconstructionReceiver.OnConnectionError(OnConnectionErrorHandler));
        ReconstructionReceiver.StartClient(m_ClientID);        
    }

    // Update is called once per frame
    void Update ()
    {
        //Debug.Log ("Receiving frame-rate: " + ReconstructionReceiver.GetReconstructionReceiveFrameRate(m_ClientID));
    }

    #region IReconstructionProvider implementation

    public MeshData MeshData
    {
        get { return m_MeshData; }
    }

    public ReconstructedMeshInfo MeshInfo
    {
        get { return m_MeshInfo; }
    }

    public bool HasNew
    {
        get { return m_HasNew; }
    }

    #endregion

    void OnDestroy()
    {
        Close();
    }

    public void Close()
    {
        ReconstructionReceiver.RegisterOnReceivedMeshCallBack(m_ClientID, null);
        ReconstructionReceiver.RegisterOnConnectionErrorCallBack(m_ClientID, null);
        ReconstructionReceiver.StopClient(m_ClientID);
        m_Providers.Remove(m_ClientID);
    }

    void OnLevelUnload()
    {
        ReconstructionReceiver.Shutdown();
        //ReconstructionReceiverWrapper.UnLoad();
    }

    void OnApplicationQuit()
    {
        ReconstructionReceiver.Shutdown();
        //ReconstructionReceiverWrapper.UnLoad();
    }
}
*/
