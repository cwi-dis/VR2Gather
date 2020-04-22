using UnityEngine;
using Utils;
using DataProviders;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MetaDataProvider))]
[RequireComponent(typeof(PCLDataProvider))]
[RequireComponent(typeof(AdjustTVMesh))]

public class PointCloudConstructor : MonoBehaviour
{
    public int pcl_id;
    private bool metadataReceived = false;
    private static bool received_new_pcl = false;
    private object m_lockobj = new object();
    private int ind = 0;
    private PCLIdataProvider m_DataProvider;
    private PCLIdataProvider p_DataProvider;
    private static float[] minAndMaxCoords = { -0.6f, 0.6f, -2.0f, 5.0f, -0.5f, 0.5f }; // Bounding box 
    private System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch stopWatch1 = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch stopWatch2 = new System.Diagnostics.Stopwatch();
    private List<long> deserializeTime = new List<long>();
    private List<long> renderingTime = new List<long>();

    private int[] cameraVertices;
    private int[] vertexChannels;
    private int[] normalChannels;
    private int[] colorChannels;
    private float[][] vertices;
    private float[][] normals;
    private byte[][] colors;
    private string[] cameraNames;

    private List<int> m_vertsPerCamera = new List<int>();
    private List<int> m_vertChannels = new List<int>();
    private List<int> m_normChannels = new List<int>();
    private List<int> m_colChannels = new List<int>();
    private List<string> m_camNames = new List<string>();
    private List<Vector3> m_vertices = new List<Vector3>();
    private List<Vector3> m_normals = new List<Vector3>();
    private List<Color> m_colors = new List<Color>();
    private List<int> m_indices = new List<int>();

    // Calculating standard deviation of a list
    private double CalculateStdDev(IEnumerable<long> values)
    {
        double ret = 0;
        if (values.Count() > 0)
        {
            //Compute the Average      
            double avg = values.Average();
            //Perform the Sum of (value-avg)_2_2      
            double sum = values.Sum(d => Math.Pow(d - avg, 2));
            //Put it all together      
            ret = Math.Sqrt((sum) / (values.Count() - 1));
        }
        return ret;
    }

    // Informing that the metadata were received
    private void DataProvider_OnNewMetaData(object sender, EventArgs<byte[]> e)
    {
        lock (e)
        {
            if (e.Value != null && !metadataReceived)
            {
                var buffer = e.Value; // Buffer 's data
                var gcRes = GCHandle.Alloc(buffer, GCHandleType.Pinned); // GCHandler for the buffer
                var pnt = gcRes.AddrOfPinnedObject(); // Buffer 's address
                metadataReceived = DllFunctions.received_metadata(pnt, buffer.Length, pcl_id);
            }
        }
    }

    // Updating the pointcloud every time a new buffer is received from the network
    private void DataProvider_OnNewPCLData(object sender, EventArgs<byte[]> e)
    {
        lock (e)
        {
            if (e.Value != null && metadataReceived && !received_new_pcl)
            {
                // Starting the stopwatch which counts the time needed to process a buffer until the deserialization of the pointcloud data 
                stopWatch = System.Diagnostics.Stopwatch.StartNew();
                stopWatch.Start();

                // Flaging that a new buffer is received
                var buffer = e.Value; // Buffer 's data
                var gcRes = GCHandle.Alloc(buffer, GCHandleType.Pinned); // GCHandler for the buffer
                var pnt = gcRes.AddrOfPinnedObject(); // Buffer 's address
                IntPtr pclPtr = DllFunctions.callColorizedPCloudFrameDLL(pnt, buffer.Length, pcl_id); // Pointer of the returned structure

                DllFunctions.PointCloud currentPcl = (DllFunctions.PointCloud)Marshal.PtrToStructure(pclPtr, typeof(DllFunctions.PointCloud)); // C# struct equivalent of the one produced by the native C++ DLL
                stopWatch.Stop();

                // Clearing the lists of the deserialized buffer 's data
                m_vertices.Clear();
                m_normals.Clear();
                m_colors.Clear();
                m_vertsPerCamera.Clear();
                m_vertChannels.Clear();
                m_normChannels.Clear();
                m_colChannels.Clear();
                m_camNames.Clear();
                m_indices.Clear();

                try
                {
                    if (ind == 0) { 
                        stopWatch2 = System.Diagnostics.Stopwatch.StartNew();
                        stopWatch2.Start();
                    }
                    // Allocating memory for the parameters of the pointcloud 
                    if (ind == 0)
                        AllocateMemForParams(currentPcl);
                    
                    // Defining the parameters (number of vertices, channels, etc) from the returned struct
                    DefineParams(currentPcl);

                    //Allocating memory for the pointcloud data
                    if (ind ==0)
                        AllocateMemForData(currentPcl);

                    // Defining the vertices, normals and colors of the pointcloud from the returned struct
                    DefineVertsNormsColors(currentPcl);

                    // Freeing the GCHandler
                    gcRes.Free();

                    received_new_pcl = true;
                }
                catch (UnityException ex)
                {
                    Debug.Log(ex.Message);
                    return;
                }

                deserializeTime.Add(stopWatch.ElapsedMilliseconds);
                if ((deserializeTime.Count % 100) == 0 && ind != 0)
                    Debug.Log("Buffer Deserialization time for " + deserializeTime.Count + " pointclouds -> Mean: " + deserializeTime.Average() + " milliseconds, " + "Std: " + CalculateStdDev(deserializeTime) + "  milliseconds");
            }
        }
    }

    private void Awake()
    {
        // Assigning the function DataProvider_OnNewPCLData to NetworkDataProvider 
        //in order to update the pointcloud every time a new buffer is received from the network
        m_DataProvider = GetComponent<PCLDataProvider>();
        m_DataProvider.OnNewPCLData += DataProvider_OnNewPCLData;
        // Also assigning the function DataProvider_OnNewMetaData to another NetworkProvider in order to know when the metadata were received
        p_DataProvider = GetComponent<MetaDataProvider>();
        p_DataProvider.OnNewMetaData += DataProvider_OnNewMetaData;
        // Assigning a shader to the game object
        GetComponent<MeshRenderer>().material = new Material(Shader.Find("Custom/VertexColor"));
    }

    private void OnDestroy()
    {
        // Removing the function DataProvider_OnNewPCLData from its assignment to the NetworkDataProvider when the game object gets destroyed
        m_DataProvider.OnNewPCLData -= DataProvider_OnNewPCLData;
        // Removing the function DataProvider_OnNewMetaData from its assignment to the other NetworkDataProvider when the game object gets destroyed
        p_DataProvider.OnNewMetaData -= DataProvider_OnNewMetaData;
        stopWatch2.Stop();
        Debug.Log("Rendering time for " + renderingTime.Count + " pointclouds -> Mean: " + renderingTime.Average() + " milliseconds, " + "Std: " + CalculateStdDev(renderingTime) + " milliseconds");
        Debug.Log("Buffer Deserialization time for " + deserializeTime.Count + " pointclouds -> Mean: " + deserializeTime.Average() + " milliseconds, " + "Std: " + CalculateStdDev(deserializeTime) + "  milliseconds");
        Debug.Log("Total running time from first pointcloud: " + stopWatch2.ElapsedMilliseconds + " milliseconds");
    }

    private void Update()
    {
        // Checking if a new buffer is received
        if (!received_new_pcl)
            return;

        ind += 1;

        try
        {
            stopWatch1 = System.Diagnostics.Stopwatch.StartNew();
            stopWatch1.Start();

            List<Vector3> vert;
            List<Vector3> norm;
            List<Color> color;
            List<int> inds;

            // Locking all the variables that refer to the data that need to be fed to the shader
            lock (m_lockobj)
            {
                vert = m_vertices;
                norm = m_normals;
                inds = m_indices;
                color = m_colors;
            }

            // Defining the vertices, normals and colors of the pointcloud
            GetComponent<MeshFilter>().mesh.Clear();
            GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            GetComponent<MeshFilter>().mesh.vertices = vert.ToArray();
            GetComponent<MeshFilter>().mesh.normals = norm.ToArray();
            GetComponent<MeshFilter>().mesh.colors = color.ToArray();
            GetComponent<MeshFilter>().mesh.SetIndices(inds.ToArray(), MeshTopology.Points, 0);

            stopWatch1.Stop();
            renderingTime.Add(stopWatch1.ElapsedMilliseconds);
            if ((renderingTime.Count%100) == 0 && ind != 0)
                Debug.Log("Rendering time for " + renderingTime.Count + " pointclouds -> Mean: " + renderingTime.Average() + " milliseconds, " + "Std: " + CalculateStdDev(renderingTime) + " milliseconds");
        }
        catch (Exception ex)
        {
            received_new_pcl = false;
            Debug.Log(ex);
            return;
        }

        received_new_pcl = false;
    }

    // Allocating memory and marshalling for the metadata
    void AllocateMemForParams(DllFunctions.PointCloud pcl)
    {
        cameraVertices = new int[pcl.numDevices]; // vertices per camera
        vertexChannels = new int[pcl.numDevices]; // channels per vertex
        normalChannels = new int[pcl.numDevices]; // channels per normal
        colorChannels = new int[pcl.numDevices];  // channels per color

        // Marshalling for the above fixed parameters
        Marshal.Copy(pcl.verticesPerCamera, cameraVertices, 0, pcl.numDevices);
        Marshal.Copy(pcl.vertexChannels, vertexChannels, 0, pcl.numDevices);
        Marshal.Copy(pcl.normalChannels, normalChannels, 0, pcl.numDevices);
        Marshal.Copy(pcl.colorChannels, colorChannels, 0, pcl.numDevices);

        string cameraNamesString = Marshal.PtrToStringAnsi(pcl.deviceNames);
        string[] separator = { "_" };
        cameraNames = cameraNamesString.Split(separator, pcl.numDevices, StringSplitOptions.RemoveEmptyEntries); // camera names
    }

    // Allocating memory the pointcloud data
    void AllocateMemForData(DllFunctions.PointCloud pcl)
    {
        vertices = new float[pcl.numDevices][];
        normals = new float[pcl.numDevices][];
        colors = new byte[pcl.numDevices][];

        for (int i = 0; i < pcl.numDevices; i++)
        {
            vertices[i] = new float[m_vertsPerCamera[i] * m_vertChannels[i]];
            normals[i] = new float[m_vertsPerCamera[i] * m_normChannels[i]];
            colors[i] = new byte[m_vertsPerCamera[i] * m_colChannels[i]];
        }
    }

    // Defining pointcloud 's metadata
    void DefineParams(DllFunctions.PointCloud pcl)
    {
        lock (m_lockobj)
        {
            m_vertsPerCamera.AddRange(cameraVertices);
            m_vertChannels.AddRange(vertexChannels);
            m_normChannels.AddRange(normalChannels);
            m_colChannels.AddRange(colorChannels);
            m_camNames.AddRange(cameraNames);
        }
    }

    // Defining pointcloud 's data for the shader
    void DefineVertsNormsColors(DllFunctions.PointCloud pcl)
    {
        //Marshaling for the vertices
        IntPtr[] vertexPtr = new IntPtr[pcl.numDevices];
        List<Vector3> verticesList = new List<Vector3>();
        Marshal.Copy(pcl.vertexPtr, vertexPtr, 0, pcl.numDevices);

        //Marshaling for the normals
        IntPtr[] normalPtr = new IntPtr[pcl.numDevices];
        List<Vector3> normalsList = new List<Vector3>();
        Marshal.Copy(pcl.normalPtr, normalPtr, 0, pcl.numDevices);

        //Marshaling for the colors
        IntPtr[] colorPtr = new IntPtr[pcl.numDevices];
        List<Color> colorsList = new List<Color>();
        Marshal.Copy(pcl.colorPtr, colorPtr, 0, pcl.numDevices);

        List<int> indsList = new List<int>();

        for (int i = 0; i < pcl.numDevices; i++)
        {
            // Marshalling for the vertices, normals and colors per camera
            Marshal.Copy(vertexPtr[i], vertices[i], 0, m_vertsPerCamera[i] * m_vertChannels[i]);
            Marshal.Copy(normalPtr[i], normals[i], 0, m_vertsPerCamera[i] * m_normChannels[i]);
            Marshal.Copy(colorPtr[i], colors[i], 0, m_vertsPerCamera[i] * m_colChannels[i]);

            for (int j = 0; j < m_vertsPerCamera[i]; j++)
            {
                // Checking if the vertex is inside the defined bounding box and save its data
                if (vertices[i][j * m_vertChannels[i]] > minAndMaxCoords[0] && vertices[i][j * m_vertChannels[i]] < minAndMaxCoords[1] &&
                    vertices[i][j * m_vertChannels[i] + 1] > minAndMaxCoords[2] && vertices[i][j * m_vertChannels[i] + 1] < minAndMaxCoords[3] &&
                    vertices[i][j * m_vertChannels[i] + 2] > minAndMaxCoords[4] && vertices[i][j * m_vertChannels[i] + 2] < minAndMaxCoords[5])
                {
                    verticesList.Add(new Vector3(vertices[i][j * m_vertChannels[i]], vertices[i][j * m_vertChannels[i] + 1], vertices[i][j * m_vertChannels[i] + 2]));
                    indsList.Add(verticesList.Count - 1);
                    normalsList.Add(new Vector3(normals[i][j * m_normChannels[i]], normals[i][j * m_normChannels[i] + 1], normals[i][j * m_normChannels[i] + 2]));

                    if (m_colChannels[i] == 3)
                        colorsList.Add(new Color((float)colors[i][j * 3] / 255.0f, (float)colors[i][j * 3 + 1] / 255.0f, (float)colors[i][j * 3 + 2] / 255.0f));
                    else if (m_colChannels[i] == 4)
                        colorsList.Add(new Color((float)colors[i][j * 4] / 255.0f, (float)colors[i][j * 4 + 1] / 255.0f, (float)colors[i][j * 4 + 2] / 255.0f, (float)colors[i][j * 4 + 3] / 255.0f));
                }

            }
        }

        // Lock the list of vertices to feed the game object
        lock (m_lockobj)
        {
            m_vertices.AddRange(verticesList);
            m_indices.AddRange(indsList);
            m_normals.AddRange(normalsList);
            m_colors.AddRange(colorsList);
        }
    }
}