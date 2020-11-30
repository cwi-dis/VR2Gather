using UnityEngine;
using Utils;
using DataProviders;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(NetworkDataProvider))]
[RequireComponent(typeof(AdjustTVMesh))]

public class MeshConstructor : MonoBehaviour
{
    public int mesh_id;
    private bool received_new_frame = false;
    private int allFrames = 0;
    private IDataProvider m_DataProvider;
    private System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch stopWatch_d = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch stopWatch_d_f = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch stopWatch_d_t = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch stopWatch_d_g = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch stopWatch_d_p = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch stopWatch_r = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch stopWatch_ft = new System.Diagnostics.Stopwatch();
    private string firstFrameTime = "";
    private PerformanceMetrics performanceMetricsObj;

    private List<Texture2D> m_Textures = new List<Texture2D>();
    private List<Vector3> m_vertices = new List<Vector3>();
    private List<int> m_head = new List<int>();
    private List<Vector4> m_participatingCams = new List<Vector4>();
    private List<int> m_faces = new List<int>();
    private byte[][] m_textures;
    private List<float> m_colorExts = new List<float>();
    private List<float> m_colorInts = new List<float>();
    private List<float> m_radialCoeffs = new List<float>();
    private List<float> m_tangentialCoeffs = new List<float>();
    private List<long> deserializeTime = new List<long>();
    private List<long> renderingTime = new List<long>();
    private int m_numDevs;
    private int m_width;
    private int m_height;
    private long m_timestamp;
    private object m_lockobj = new object();
    private int ind = 0;
    private int total_decompressed_buffer_size = 0;
    public int fps = 0;
    private long timeNow;

    // Updating the mesh every time a new buffer is received from the network
    private void DataProvider_OnNewData(object sender, EventArgs<byte[]> e)
    {
        allFrames += 1;

        lock (e)
        {
            if (e.Value != null && received_new_frame == false)
            {
                stopWatch_d = System.Diagnostics.Stopwatch.StartNew();
                stopWatch_ft = System.Diagnostics.Stopwatch.StartNew();

                // Starting the stopwatch which counts the time needed to process a buffer until the mesh is rendered
                if (stopWatch.ElapsedMilliseconds == 0)
                    stopWatch = System.Diagnostics.Stopwatch.StartNew();

                // Flaging that a new buffer is received
                int size = Marshal.SizeOf(e.Value[0]) * e.Value.Length; // Buffer 's size
                var buffer = e.Value; // Buffer 's data
                var gcRes = GCHandle.Alloc(buffer, GCHandleType.Pinned); // GCHandler for the buffer
                var pnt = gcRes.AddrOfPinnedObject(); // Buffer 's address
                stopWatch_d_f = System.Diagnostics.Stopwatch.StartNew();
                IntPtr meshPtr = DllFunctions.callTVMFrameDLL(pnt, buffer.Length, mesh_id); // Pointer of the returned structure
                DllFunctions.Mesh currentMesh = (DllFunctions.Mesh)Marshal.PtrToStructure(meshPtr, typeof(DllFunctions.Mesh)); // C# struct equivalent of the one produced by the native C++ DLL
                stopWatch_d_f.Stop();

                // Clearing the lists of the deserialized buffer 's data
                m_vertices.Clear();
                m_faces.Clear();
                m_participatingCams.Clear();
                m_colorExts.Clear();
                m_colorInts.Clear();
                m_tangentialCoeffs.Clear();
                m_radialCoeffs.Clear();
                m_timestamp = 0;

                try
                {
                    stopWatch_d_t = System.Diagnostics.Stopwatch.StartNew();
                    // Defining the textures from the returned struct
                    DefineTexture(currentMesh);
                    stopWatch_d_t.Stop();

                    stopWatch_d_g = System.Diagnostics.Stopwatch.StartNew();
                    // Defining the mesh data from the returned struct
                    DefineShape(currentMesh);
                    stopWatch_d_g.Stop();

                    stopWatch_d_p = System.Diagnostics.Stopwatch.StartNew();
                    // Defining the shader 's parameters from the returned struct
                    DefineShaderParams(currentMesh);
                    stopWatch_d_p.Stop();

                    // Freeing the GCHandler
                    gcRes.Free();
                    performanceMetricsObj.updateReceivingAndDeserializationMetrics(new List<System.Diagnostics.Stopwatch>() { stopWatch_d, stopWatch_d_f, stopWatch_d_t, stopWatch_d_g, stopWatch_d_p },
                                                                                   new List<int>() { size, total_decompressed_buffer_size, m_vertices.ToArray().Length, allFrames });
                    ++fps;
                    total_decompressed_buffer_size = 0;
                    received_new_frame = true;
                }
                catch (UnityException ex)
                {
                    Debug.Log(ex.Message);
                }

                stopWatch_d.Stop();

            }
        }
    }

    // Assigning the function DataProvider_OnNewData to NetworkDataProvider in order to update the mesh every time a new buffer is received from the network
    private void Awake()
    {
        // Assigning the function DataProvider_OnNewData to NetworkDataProvider in order to update the mesh every time a new buffer is received from the network
        m_DataProvider = GetComponent<NetworkDataProvider>();
        m_DataProvider.OnNewData += DataProvider_OnNewData;
        performanceMetricsObj = this.gameObject.AddComponent<PerformanceMetrics>();
        performanceMetricsObj.runPerfomanceMetricsExe();
        InvokeRepeating("printAndSaveMetricsEveryTenSec", 10.0f, 10.0f);
        List<string> playerIds = new List<string>();
        for (int i = 0; i < OrchestratorController.Instance.ConnectedUsers.Length; i++)
            if (OrchestratorController.Instance.ConnectedUsers[i].userData.userRepresentationType.ToString().ToLower().Contains("tvm"))
                playerIds.Add(OrchestratorController.Instance.ConnectedUsers[i].userId);
        playerIds.Sort();
        mesh_id = playerIds.IndexOf(this.gameObject.transform.parent.name.Split('_')[1]);
    }

    private void OnDestroy()
    {
        // Removing the function DataProvider_OnNewData from its assignment to the NetworkDataProvider when the game object gets destroyed
        m_DataProvider.OnNewData -= DataProvider_OnNewData;
        performanceMetricsObj.clearAll();
        performanceMetricsObj.saveAndDestroy(this.transform.parent.parent.name + "_ID_" + this.gameObject.transform.parent.name.Split('_')[1]);
    }

    private void Update()
    {
        // Checking if a new buffer is received
        if (!received_new_frame)
            return;

        ind += 1;

        try
        {
            stopWatch_r = System.Diagnostics.Stopwatch.StartNew();
            stopWatch_r.Start();

            List<Vector3> vert;
            List<Vector4> ids;
            List<int> face;
            List<float> c_extrinsics;
            List<float> c_intrinsics;
            List<float> radial;
            List<float> tangential;
            byte[][] texts = new byte[m_numDevs][];
            List<Matrix4x4> exts = new List<Matrix4x4>();
            List<Matrix4x4> intrs = new List<Matrix4x4>();
            int width;
            int height;
            int numDevs;
            long tmstp;

            // Locking all the variables that refer to the data that need to be fed to the shader
            lock (m_lockobj)
            {
                vert = m_vertices;
                ids = m_participatingCams;
                face = m_faces;
                width = m_width;
                height = m_height;
                numDevs = m_numDevs;
                tmstp = m_timestamp;
                c_extrinsics = m_colorExts;
                c_intrinsics = m_colorInts;
                radial = m_radialCoeffs;
                tangential = m_tangentialCoeffs;
                for (int i = 0; i < numDevs; i++)
                    texts[i] = m_textures[i];
                for (int i = 0; i < numDevs; i++)
                {

                    Matrix4x4 current_exts = (new Matrix4x4(new Vector4(c_extrinsics[16 * i], c_extrinsics[16 * i + 1], c_extrinsics[16 * i + 2], c_extrinsics[16 * i + 3]),
                                           new Vector4(c_extrinsics[16 * i + 4], c_extrinsics[16 * i + 5], c_extrinsics[16 * i + 6], c_extrinsics[16 * i + 7]),
                                           new Vector4(c_extrinsics[16 * i + 8], c_extrinsics[16 * i + 9], c_extrinsics[16 * i + 10], c_extrinsics[16 * i + 11]),
                                           new Vector4(c_extrinsics[16 * i + 12], c_extrinsics[16 * i + 13], c_extrinsics[16 * i + 14], c_extrinsics[16 * i + 15])));
                    Matrix4x4 current_intrs = (new Matrix4x4(new Vector4(c_intrinsics[9 * i], c_intrinsics[9 * i + 1], c_intrinsics[9 * i + 2], 0),
                                           new Vector4(c_intrinsics[9 * i + 3], c_intrinsics[9 * i + 4], c_intrinsics[9 * i + 5], 0),
                                           new Vector4(c_intrinsics[9 * i + 6], c_intrinsics[9 * i + 7], c_intrinsics[9 * i + 8], 0),
                                           new Vector4(0, 0, 0, 1))).transpose;
                    exts.Add(current_exts);
                    intrs.Add(current_intrs);
                }
            }

            // Defining the vertices, triangles and normals of the mesh
            GetComponent<MeshFilter>().mesh.Clear();
            GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            GetComponent<MeshFilter>().mesh.vertices = vert.ToArray();
            GetComponent<MeshFilter>().mesh.SetIndices(face.ToArray(), MeshTopology.Triangles, 0);
            GetComponent<MeshFilter>().mesh.RecalculateNormals();


            // Passing the camera ids participating in forming a vertex 's texture as well as the weights of each one
            GetComponent<MeshFilter>().mesh.tangents = ids.ToArray();


            // Providing all the data needed for the shader
            GetComponent<MeshRenderer>().material.shader = Shader.Find("Unlit/TVMeshShader"); // Shader 's file name
            GetComponent<MeshRenderer>().material.SetMatrixArray("ColorIntrinsics", intrs); // Color intrinsics matrix in an array (column major)
            GetComponent<MeshRenderer>().material.SetMatrixArray("ColorExtrinsics", exts); // Color extrinsics matrix in an array (column major)
            GetComponent<MeshRenderer>().material.SetFloatArray("RadialDistortionCoeffs", radial);
            GetComponent<MeshRenderer>().material.SetFloatArray("TangentialDistortionCoeffs", tangential);

            Texture2D current_tex = null;

            // Assigning the textures

            for (int i = 0; i < numDevs; i++)
            {
                // After the first reconstructed mesh, delete the existing texture of the current id in the list instead of recreating the latter
                if (i < m_Textures.Count)
                {
                    current_tex = m_Textures[i];
                    Texture2D.Destroy(m_Textures[i]);
                    current_tex = null;
                }

                // Loading texture data
                current_tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                current_tex.LoadRawTextureData(texts[i]);

                // Assigning the texture to the list
                if (i >= m_Textures.Count)
                    m_Textures.Add(current_tex); // for the first reconstructed mesh
                else
                    m_Textures[i] = current_tex; // for every other situation

                // Applying the texture changes
                current_tex.Apply();
            }


            // Defining all the shader 's variables
            for (int i = 0; i < numDevs; i++)
                GetComponent<MeshRenderer>().material.SetTexture("Texture" + i, m_Textures[i]); // Textures

            GetComponent<MeshRenderer>().material.SetInt("CameraNumber", numDevs); // Number of cameras of the setup
            GetComponent<MeshRenderer>().material.SetInt("TextureWidth", width); // Texture image width
            GetComponent<MeshRenderer>().material.SetInt("TextureHeight", height); // Texture image height

            stopWatch_r.Stop();
            stopWatch_ft.Stop();
            timeNow = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
            performanceMetricsObj.updateRenderingMetrics(stopWatch_r, stopWatch_ft, Convert.ToInt32(timeNow - tmstp));
        }
        catch (Exception ex)
        {
            received_new_frame = false;

            Debug.Log(ex);
            return;
        }

        received_new_frame = false;
    }


    void printAndSaveMetricsEveryTenSec()
    {
        stopWatch.Stop();
        allFrames = 0;
        performanceMetricsObj.printMetrics(stopWatch, this.transform.parent.parent.name);
        performanceMetricsObj.saveMetrics(stopWatch);
        performanceMetricsObj.clearAll();
        stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();
    }

    // Defining textures of the shader
    void DefineTexture(DllFunctions.Mesh mesh)
    {
        //Marshaling for the texture images
        IntPtr[] texturePtr = new IntPtr[mesh.numDevices];
        byte[][] textures = new byte[mesh.numDevices][];

        Marshal.Copy(mesh.textures, texturePtr, 0, mesh.numDevices);

        for (int i = 0; i < mesh.numDevices; i++)
        {
            textures[i] = new byte[mesh.width * mesh.height * 3];
            Marshal.Copy(texturePtr[i], textures[i], 0, mesh.width * mesh.height * 3);
            if (stopWatch.ElapsedMilliseconds > 0)
                total_decompressed_buffer_size += mesh.width * mesh.height * 3;
        }

        // Lock the byte arrays to feed the game object
        lock (m_lockobj)
        {
            m_textures = textures;
        }
    }

    // Defining the rest of the shader 's parameters
    void DefineShaderParams(DllFunctions.Mesh mesh)
    {
        // Marshaling for the weigths of each of the cameras defining a vertex 's texture
        float[] camWeights = new float[mesh.numVertices];
        Marshal.Copy(mesh.weights, camWeights, 0, mesh.numVertices);

        // Marshaling for the ids of the cameras participating to a vertex 's texture
        int[] cam1 = new int[mesh.numVertices];
        int[] cam2 = new int[mesh.numVertices];
        Vector4[] camParticipation = new Vector4[mesh.numVertices];
        Marshal.Copy(mesh.id1, cam1, 0, mesh.numVertices);
        Marshal.Copy(mesh.id2, cam2, 0, mesh.numVertices);

        // Assigning vertices to a Vector3 array in order to feed the shader
        for (int i = 0; i < mesh.numVertices; i++)
            camParticipation[i] = (new Vector4((float)cam1[i], (float)cam2[i], camWeights[i], 1 - camWeights[i]));

        // Marshaling for the color extrinsics
        float[] colorExtrinsics = new float[mesh.numDevices * 16];
        Marshal.Copy(mesh.colorExts, colorExtrinsics, 0, mesh.numDevices * 16);

        // Marshaling for the color intrinsics
        float[] colorIntrinsics = new float[mesh.numDevices * 9];
        Marshal.Copy(mesh.colorInts, colorIntrinsics, 0, mesh.numDevices * 9);

        // Marshaling for the color extrinsics
        float[] radialCoeffs = new float[mesh.numDevices * 6];
        Marshal.Copy(mesh.radialDistortionCoeffs, radialCoeffs, 0, mesh.numDevices * 6);

        // Marshaling for the color extrinsics
        float[] tangentialCoeffs = new float[mesh.numDevices * 2];
        Marshal.Copy(mesh.tangentialDistortionCoeffs, tangentialCoeffs, 0, mesh.numDevices * 2);

        long timestamp = Convert.ToInt64(Marshal.PtrToStringAnsi(mesh.timestamp));

        if (stopWatch.ElapsedMilliseconds > 0)
            total_decompressed_buffer_size += (sizeof(float) * (mesh.numVertices + mesh.numDevices * 16 + mesh.numDevices * 9 + mesh.numDevices * 6 + mesh.numDevices * 2) + sizeof(int) * (2 * mesh.numVertices + 5));

        // Lock the arrays in order to feed the game object
        lock (m_lockobj)
        {
            m_numDevs = mesh.numDevices;
            m_width = mesh.width;
            m_height = mesh.height;
            m_participatingCams.AddRange(camParticipation);
            m_colorExts.AddRange(colorExtrinsics);
            m_colorInts.AddRange(colorIntrinsics);
            m_radialCoeffs.AddRange(radialCoeffs);
            m_tangentialCoeffs.AddRange(tangentialCoeffs);
            m_timestamp = timestamp;
        }
    }

    //Creating the mesh
    void DefineShape(DllFunctions.Mesh mesh)
    {
        // Marshaling for the faces
        int[] faces = new int[mesh.numFaces * 3];
        Marshal.Copy(mesh.faces, faces, 0, mesh.numFaces * 3);

        // Marshaling for the vertices
        float[] vertexArray = new float[mesh.numVertices * 3];
        Vector3[] vertices = new Vector3[mesh.numVertices];
        Marshal.Copy(mesh.vertices, vertexArray, 0, mesh.numVertices * 3);

        for (int i = 0; i < mesh.numVertices; i++)
            vertices[i] = (new Vector3(vertexArray[3 * i], vertexArray[3 * i + 1], vertexArray[3 * i + 2]));

        if (stopWatch.ElapsedMilliseconds > 0)
            total_decompressed_buffer_size += (sizeof(float) * (mesh.numVertices * 3) + sizeof(int) * (mesh.numFaces * 3));

        // Lock the arrays in order to feed the game object
        lock (m_lockobj)
        {
            m_vertices.AddRange(vertices);
            m_faces.AddRange(faces);
        }
    }
}
