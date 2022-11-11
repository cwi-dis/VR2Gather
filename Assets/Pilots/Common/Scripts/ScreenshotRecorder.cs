﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using System.IO;

namespace VRT.Pilots.Common
{
    public class ScreenshotRecorder : MonoBehaviour
    {
        private bool takeScreenshot = false;
        private string screenshotTargetDirectory = "";
        private int width;
        private int height;

        string Name()
        {
            return "ScreenshotRecorder";
        }

        // Start is called before the first frame update
        void Start()
        {
            takeScreenshot = Config.Instance.ScreenshotTool.takeScreenshot;
            screenshotTargetDirectory = Config.Instance.ScreenshotTool.screenshotTargetDirectory;
            gameObject.SetActive(takeScreenshot);
            if (!takeScreenshot)
            {
                Debug.Log($"{Name()}: disabling, config.ScreenshotTool.takeScreenshot = false");
                return;
            }
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"output_dir={screenshotTargetDirectory}");
#endif
            width = Screen.width;
            height = Screen.height;
            if (!Directory.Exists(screenshotTargetDirectory))
            {
                Directory.CreateDirectory(screenshotTargetDirectory);
            }
        }

        // Update is called once per frame
        void Update()
        {
            StartCoroutine(captureScreenshot());
        }
        IEnumerator captureScreenshot()
        {
            yield return new WaitForEndOfFrame();

            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
            byte[] screenshotBytes = screenshot.EncodeToPNG();
            Destroy(screenshot);

            File.WriteAllBytes(screenshotTargetDirectory + "/Frame" + Time.frameCount + ".png",screenshotBytes);

            yield return null;
        }

    }
}