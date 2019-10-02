using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;

public class Profiler : MonoBehaviour
{
    private int     avgFPS;
    private int     minFPS;
    private int     maxFPS;
    private int     vramUsage;
    private int     ramUsage;

    private int     numUsers;
    private int     totalPackets;
    private int     pps;

    private string  cpuInfo;
    private string  ramInfo;
    private string  gpuInfo;
    private string  vramInfo;
    private string  fpsInfo;

    private string  userInfo;
    private string  packetsInfo;
    private string  ppsInfo;

    private float   timeCounter = 0.0f;
    private int     frameCounter = 0;

    [SerializeField]
    private Text cpuText;
    [SerializeField]
    private Text ramText;
    [SerializeField]
    private Text gpuText;
    [SerializeField]
    private Text vramText;
    [SerializeField]
    private Text fpsText;
    [SerializeField]
    private Text userText;
    [SerializeField]
    private Text packetsText;
    [SerializeField]
    private Text ppsText;

    //[SerializeField]
    //private ShowTVMs[] playersTVM;


    // Start is called before the first frame update
    void Start() {
        numUsers = 0;
        avgFPS = 0;
        minFPS = 10000;
        maxFPS = 0;
        totalPackets = 0;
        pps = 0;
        ramUsage = (int)(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
        vramUsage = (int)(UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f);

        cpuInfo =   "CPU: " + SystemInfo.processorType + " [" +
                    SystemInfo.processorCount + " cores]";
        ramInfo =   "RAM: " + ramUsage + " MB / " +
                    SystemInfo.systemMemorySize + " MB";
        gpuInfo =   "GPU: " + SystemInfo.graphicsDeviceName +
                    " [" + SystemInfo.graphicsDeviceType + "]" +
                    " - Shader Level: " + SystemInfo.graphicsShaderLevel;
        vramInfo =  "VRAM: " + vramUsage + " MB / " +
                    SystemInfo.graphicsMemorySize + " MB";
        fpsInfo =   "FPS: " + avgFPS + "\n" +
                    "MAX FPS: " + maxFPS + "\n" +
                    "MIN FPS: " + minFPS;
        userInfo =  "USERS: " + numUsers;
        ppsInfo =   "PPS: " + pps;
        packetsInfo = "TOTAL PACKETS: " + totalPackets;

    }

    // Update is called once per frame
    void Update() {
        //CalculateUsers();
        CalculatePerformance();

        ramInfo =   "RAM: " + ramUsage + " MB / " +
                    SystemInfo.systemMemorySize + " MB";
        vramInfo =  "VRAM: " + vramUsage + " MB / " +
                    SystemInfo.graphicsMemorySize + " MB";
        fpsInfo =   "FPS: " + avgFPS + "\n" +
                    "MAX FPS: " + maxFPS + "\n" +
                    "MIN FPS: " + minFPS;

        userInfo =  "USERS: " + numUsers;
        ppsInfo =   "PPS: " + pps;
        packetsInfo = "TOTAL PACKETS: " + totalPackets;

        TextUpdate();
    }

    void CalculatePerformance() {
        if (timeCounter <= 1.0f) {
            timeCounter += Time.deltaTime;
            ++frameCounter;
        }
        else {
            ramUsage = (int)(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
            vramUsage = (int)(UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f);
            avgFPS = frameCounter / (int)timeCounter;
            if (avgFPS > maxFPS) maxFPS = avgFPS;
            if (avgFPS < minFPS) minFPS = avgFPS;
            timeCounter = 0.0f;
            frameCounter = 0;
        }
    }

    void TextUpdate(){
        cpuText.text = cpuInfo;
        ramText.text = ramInfo;
        gpuText.text = gpuInfo;
        vramText.text = vramInfo;
        fpsText.text = fpsInfo;
        userText.text = userInfo;
        packetsText.text = packetsInfo;
        ppsText.text = ppsInfo;
    }

    //void CalculateUsers() {
    //    numUsers = 0;
    //    pps = 0;
    //    totalPackets = 0;
    //    foreach(ShowTVMs o in playersTVM) {
    //        if (o.isActiveAndEnabled) {
    //            ++numUsers;
    //            pps += o.pps;
    //            totalPackets += o.Packets;
    //        }
    //    }
    //    if (numUsers > 0) pps /= numUsers;
    //}

    //private void OnGUI()
    //{
    //    GUI.Label(new Rect(5, 10, 1000, 25), cpuInfo);
    //    GUI.Label(new Rect(5, 40, 1000, 25), ramInfo);
    //    GUI.Label(new Rect(5, 70, 1000, 25), gpuInfo);
    //    GUI.Label(new Rect(5, 100, 1000, 25), vramInfo);
    //    GUI.Label(new Rect(5, 130, 1000, 50), fpsInfo);
    //}
}
