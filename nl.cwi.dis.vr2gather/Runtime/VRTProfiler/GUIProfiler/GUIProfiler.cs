using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;

namespace VRT.Profiler
{
    public class GUIProfiler : MonoBehaviour
    {
        private int avgFPS;
        private int minFPS;
        private int maxFPS;
        private int vramUsage;
        private int ramUsage;

        private int numUsers;
        private int totalPackets;
        private int pps;

        private string cpuInfo;
        private string ramInfo;
        private string gpuInfo;
        private string vramInfo;
        private string fpsInfo;

        private string userInfo;
        private string packetsInfo;
        private string ppsInfo;

        private float timeCounter = 0.0f;
        private int frameCounter = 0;

        [SerializeField] private Text cpuText = null;
        [SerializeField] private Text ramText = null;
        [SerializeField] private Text gpuText = null;
        [SerializeField] private Text vramText = null;
        [SerializeField] private Text fpsText = null;
        [SerializeField] private Text userText = null;
        [SerializeField] private Text packetsText = null;
        [SerializeField] private Text ppsText = null;

        

        // Start is called before the first frame update
        void Start()
        {
            numUsers = 0;
            avgFPS = 0;
            minFPS = 10000;
            maxFPS = 0;
            totalPackets = 0;
            pps = 0;
            ramUsage = (int)(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
            vramUsage = (int)(UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f);

            cpuInfo = "CPU: " + SystemInfo.processorType + " [" +
                        SystemInfo.processorCount + " cores]";
            ramInfo = "RAM: " + ramUsage + " MB / " +
                        SystemInfo.systemMemorySize + " MB";
            gpuInfo = "GPU: " + SystemInfo.graphicsDeviceName +
                        " [" + SystemInfo.graphicsDeviceType + "]" +
                        " - Shader Level: " + SystemInfo.graphicsShaderLevel;
            vramInfo = "VRAM: " + vramUsage + " MB / " +
                        SystemInfo.graphicsMemorySize + " MB";
            fpsInfo = "FPS: " + avgFPS + "\n" +
                        "MAX FPS: " + maxFPS + "\n" +
                        "MIN FPS: " + minFPS;
            userInfo = "USERS: " + numUsers;
            ppsInfo = "PPS: " + pps;
            packetsInfo = "TOTAL PACKETS: " + totalPackets;

        }

        // Update is called once per frame
        void Update()
        {
            //CalculateUsers();
            CalculatePerformance();

            ramInfo = "RAM: " + ramUsage + " MB / " +
                        SystemInfo.systemMemorySize + " MB";
            vramInfo = "VRAM: " + vramUsage + " MB / " +
                        SystemInfo.graphicsMemorySize + " MB";
            fpsInfo = "FPS: " + avgFPS + "\n" +
                        "MAX FPS: " + maxFPS + "\n" +
                        "MIN FPS: " + minFPS;

            userInfo = "USERS: " + numUsers;
            ppsInfo = "PPS: " + pps;
            packetsInfo = "TOTAL PACKETS: " + totalPackets;

            TextUpdate();
        }

        void CalculatePerformance()
        {
            if (timeCounter <= 1.0f)
            {
                timeCounter += Time.deltaTime;
                ++frameCounter;
            }
            else
            {
                ramUsage = (int)(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
                vramUsage = (int)(UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f);
                avgFPS = frameCounter / (int)timeCounter;
                if (avgFPS > maxFPS) maxFPS = avgFPS;
                if (avgFPS < minFPS) minFPS = avgFPS;
                timeCounter = 0.0f;
                frameCounter = 0;
            }
        }

        void TextUpdate()
        {
            cpuText.text = cpuInfo;
            ramText.text = ramInfo;
            gpuText.text = gpuInfo;
            vramText.text = vramInfo;
            fpsText.text = fpsInfo;
            userText.text = userInfo;
            packetsText.text = packetsInfo;
            ppsText.text = ppsInfo;
        }
    }
}