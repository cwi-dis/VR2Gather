using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Profiler : MonoBehaviour
{
    private int     numUsers;
    private int     avgFPS;
    private int     minFPS;
    private int     maxFPS;
    private int     vramUsage;
    private int     ramUsage;

    private string  cpuInfo;
    private string  ramInfo;
    private string  gpuInfo;
    private string  vramInfo;
    private string  fpsInfo;

    private float   timeCounter = 0.0f;
    private int     frameCounter = 0;


    // Start is called before the first frame update
    void Start() {
        numUsers = 0;
        avgFPS = 0;
        minFPS = 10000;
        maxFPS = 0;
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
    }

    // Update is called once per frame
    void Update() {
        CalculatePerformance();

        ramInfo =   "RAM: " + ramUsage + " MB / " +
                    SystemInfo.systemMemorySize + " MB";
        vramInfo =  "VRAM: " + vramUsage + " MB / " +
                    SystemInfo.graphicsMemorySize + " MB";
        fpsInfo =   "FPS: " + avgFPS + "\n" +
                    "MAX FPS: " + maxFPS + "\n" +
                    "MIN FPS: " + minFPS;
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

    private void OnGUI()
    {
        GUI.Label(new Rect(5, 10, 1000, 25), cpuInfo);
        GUI.Label(new Rect(5, 40, 1000, 25), ramInfo);
        GUI.Label(new Rect(5, 70, 1000, 25), gpuInfo);
        GUI.Label(new Rect(5, 100, 1000, 25), vramInfo);
        GUI.Label(new Rect(5, 130, 1000, 50), fpsInfo);
    }
}
