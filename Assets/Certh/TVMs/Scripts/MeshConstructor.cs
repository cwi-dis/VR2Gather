using UnityEngine;
using Utils;
using DataProviders;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

using System;
using System.IO;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(NetworkDataProvider))]

public class MeshConstructor : MonoBehaviour
{
    public int mesh_id;
    private bool received_new_frame = false;
    private GameObject gameObj;
    private GameObject mirror;
    private IDataProvider m_DataProvider;
    private System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch stopWatch1 = new System.Diagnostics.Stopwatch();
    private List<Texture2D> m_Textures = new List<Texture2D>();
    private List<Vector3> m_vertices = new List<Vector3>();
    private List<Vector4> m_participatingCams = new List<Vector4>();
    private List<int> m_faces = new List<int>();
    private byte[][] m_textures;
    private List<float> m_colorExts = new List<float>();
    private List<float> m_colorInts = new List<float>();
    private List<long> deserializeTime = new List<long>();
    private List<long> renderingTime = new List<long>();
    private int m_numDevs;
    private int m_width;
    private int m_height;
    private object m_lockobj = new object();
    private int ind = 0;

    private double CalculateStdDev(IEnumerable<long> values) {
        double ret = 0;
        if (values.Count() > 0) {
            //Compute the Average      
            double avg = values.Average();
            //Perform the Sum of (value-avg)_2_2      
            double sum = values.Sum(d => Math.Pow(d - avg, 2));
            //Put it all together      
            ret = Math.Sqrt((sum) / (values.Count() - 1));
        }
        return ret;
    }

    // Updating the mesh every time a new buffer is received from the network
    private void DataProvider_OnNewData(object sender, EventArgs<byte[]> e) {
        // Starting the stopwatch which counts the time needed to process a buffer until the mesh is rendered
        //stopWatch = System.Diagnostics.Stopwatch.StartNew();

        lock (e) {
            if (e.Value != null) {
                //// Storing the received buffer to a .bin file
                //File.WriteAllBytes(@"C:\VCL_User_Prod\RealSenzBinsCorto\RealSenzBin_" + ind + ".bin", e.Value);
                //Debug.Log("file saved");

                //stopWatch = System.Diagnostics.Stopwatch.StartNew();
                //stopWatch.Start();

                // Flaging that a new buffer is received
                int size = Marshal.SizeOf(e.Value[0]) * e.Value.Length; // Buffer 's size
                var buffer = e.Value; // Buffer 's data
                var gcRes = GCHandle.Alloc(buffer, GCHandleType.Pinned); // GCHandler for the buffer
                var pnt = gcRes.AddrOfPinnedObject(); // Buffer 's address
                IntPtr meshPtr = DllFunctions.callTVMFrameDLL(pnt, buffer.Length, mesh_id); // Pointer of the returned structure
                DllFunctions.Mesh currentMesh = (DllFunctions.Mesh)Marshal.PtrToStructure(meshPtr, typeof(DllFunctions.Mesh)); // C# struct equivalent of the one produced by the native C++ DLL

                // Clearing the lists of the deserialiazed buffer 's data
                m_vertices.Clear();
                m_faces.Clear();
                m_participatingCams.Clear();
                m_colorExts.Clear();
                m_colorInts.Clear();

                try {
                    // Defining the textures from the returned struct
                    DefineTexture(currentMesh);

                    // Defining the mesh data from the returned struct
                    CreateShape(currentMesh);

                    // Defining the shader 's parameters from the returned struct
                    DefineShaderParams(currentMesh);

                    // Freeing the GCHandler
                    gcRes.Free();

                    received_new_frame = true;
                }
                catch (UnityException ex) {
                    Debug.Log(ex.Message);
                }
                //stopWatch.Stop();
                //deserializeTime.Add(stopWatch.ElapsedMilliseconds);
                //if(deserializeTime.Count == 1000)
                //    Debug.Log("Deserialization time for 1000 frames -> Mean: " + deserializeTime.Average() + ", " + "Std: " + CalculateStdDev(deserializeTime));
            }
        }
    }

    // Assigning the function DataProvider_OnNewData to NetworkDataProvider in order to update the mesh every time a new buffer is received from the network
    private void Awake() {
        // Assigning the function DataProvider_OnNewData to NetworkDataProvider in order to update the mesh every time a new buffer is received from the network
        m_DataProvider = GetComponent<NetworkDataProvider>();
        m_DataProvider.OnNewData += DataProvider_OnNewData;

        // Defining the position of the original mesh
        //this.transform.position = new Vector3(-1.08f, -1.14f, 2.15f);
    }

    private void OnDestroy() {
        // Removing the function DataProvider_OnNewData from its assignment to the NetworkDataProvider when the game object gets destroyed
        m_DataProvider.OnNewData -= DataProvider_OnNewData;
    }


    private void Update() {
        // Checking if a new buffer is received
        if (!received_new_frame)
            return;

        try {
            //stopWatch1 = System.Diagnostics.Stopwatch.StartNew();
            //stopWatch1.Start();

            List<Vector3> vert;
            List<Vector4> ids;
            List<int> face;
            List<float> c_extrinsics;
            List<float> c_intrinsics;
            byte[][] texts = new byte[m_numDevs][];
            int width;
            int height;
            int numDevs;

            // Locking all the variables that refer to the data that need to be fed to the shader
            lock (m_lockobj) {
                vert = m_vertices;
                ids = m_participatingCams;
                face = m_faces;
                width = m_width;
                height = m_height;
                numDevs = m_numDevs;
                c_extrinsics = m_colorExts;
                c_intrinsics = m_colorInts;
                for (int i = 0; i < numDevs; i++)
                    texts[i] = m_textures[i];
            }

            if (vert.ToArray().Length != ids.ToArray().Length)
               return;

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

            Texture2D current_tex = null;

            // Assigning the textures
            for (int i = 0; i < numDevs; i++) {
                // After the first reconstructed mesh, delete the existing texture of the current id in the list instead of recreating the latter
                if (i < m_Textures.Count) {
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

            GetComponent<MeshRenderer>().material.SetFloatArray("ColorIntrinsics", c_intrinsics); // Color intrinsics matrix in an array (column major)
            GetComponent<MeshRenderer>().material.SetFloatArray("ColorExtrinsics", c_extrinsics); // Color extrinsics matrix in an array (column major)
            //stopWatch1.Stop();
            //renderingTime.Add(stopWatch1.ElapsedMilliseconds);
            //if(renderingTime.Count == 1000)
            //    Debug.Log("Rendering time for 1000 frames -> Mean: " + renderingTime.Average() + ", " + "Std: " + CalculateStdDev(renderingTime));
        }
        catch (Exception ex) {
            received_new_frame = false;
            Debug.Log(ex);
            return;
        }

        received_new_frame = false;
    }

    // Defining textures of the shader
    void DefineTexture(DllFunctions.Mesh mesh) {
        //Marshaling for the texture images
        IntPtr[] texturePtr = new IntPtr[mesh.numDevices];
        byte[][] textures = new byte[mesh.numDevices][];

        Marshal.Copy(mesh.textures, texturePtr, 0, mesh.numDevices);

        for (int i = 0; i < mesh.numDevices; i++) {
            textures[i] = new byte[mesh.width * mesh.height * 3];
            Marshal.Copy(texturePtr[i], textures[i], 0, mesh.width * mesh.height * 3);
        }

        // Lock the byte arrays to feed the game object
        lock (m_lockobj) {
            m_textures = textures;
        }
    }

    // Defining the rest of the shader 's parameters
    void DefineShaderParams(DllFunctions.Mesh mesh) {
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

        // Lock the arrays in order to feed the game object
        lock (m_lockobj) {
            m_numDevs = mesh.numDevices;
            m_width = mesh.width;
            m_height = mesh.height;
            m_participatingCams.AddRange(camParticipation);
            m_colorExts.AddRange(colorExtrinsics);
            m_colorInts.AddRange(colorIntrinsics);
        }
    }

    //Creating the mesh
    void CreateShape(DllFunctions.Mesh mesh) {
        // Marshaling for the faces
        int[] faces = new int[mesh.numFaces * 3];
        Marshal.Copy(mesh.faces, faces, 0, mesh.numFaces * 3);

        // Marshaling for the vertices
        float[] vertexArray = new float[mesh.numVertices * 3];
        Vector3[] vertices = new Vector3[mesh.numVertices];
        Marshal.Copy(mesh.vertices, vertexArray, 0, mesh.numVertices * 3);

        for (int i = 0; i < mesh.numVertices; i++)
            vertices[i] = (new Vector3(vertexArray[3 * i], vertexArray[3 * i + 1], vertexArray[3 * i + 2]));

        // Lock the arrays in order to feed the game object
        lock (m_lockobj) {
            m_vertices.AddRange(vertices);
            m_faces.AddRange(faces);
        }
    }
}
