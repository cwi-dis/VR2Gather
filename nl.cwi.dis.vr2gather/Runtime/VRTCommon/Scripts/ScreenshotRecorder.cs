using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
using System;


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

        private System.TimeSpan minInterval = System.TimeSpan.Zero;
        private System.DateTime earliestNext = System.DateTime.MinValue;

        private int num = 0;
        private string filenameTemplate;
        string Name()
        {
            return "ScreenshotRecorder";
        }

        int getTs()
        {
            return (int)(System.DateTime.Now.TimeOfDay.TotalSeconds*1000);
        }

        // Start is called before the first frame update
        void Start()
        {
            var config = VRTConfig.Instance.ScreenshotTool;
            takeScreenshot = config.takeScreenshot;
            if (!takeScreenshot)
            {
                gameObject.SetActive(false);
                Debug.Log($"{Name()}: disabling, config.ScreenshotTool.takeScreenshot = false");
                return;
            }
            if (string.IsNullOrEmpty(config.screenshotTargetDirectory))
            {
                Debug.LogError($"{Name()}: config.screenshotTargetDirectory is empty");
                gameObject.SetActive(false);
                return;
            }
            filenameTemplate = config.filenameTemplate;

            if (config.fps != 0)
            {
                minInterval = System.TimeSpan.FromSeconds(1) / config.fps;
            }
            earliestNext = System.DateTime.Now;

            screenshotTargetDirectory = VRTConfig.ConfigFilename(config.screenshotTargetDirectory, label:"Screenshot target direcory");

            if (config.preDeleteTargetDirectory && Directory.Exists(screenshotTargetDirectory))
            {
                Debug.Log($"{Name()}: Deleting {screenshotTargetDirectory}");
                Directory.Delete(screenshotTargetDirectory, true);
            }
            if (!Directory.Exists(screenshotTargetDirectory))
            {
                Directory.CreateDirectory(screenshotTargetDirectory);
            }
#if VRT_WITH_STATS
                Statistics.Output(Name(), $"output_dir={screenshotTargetDirectory}");
#endif
            width = Screen.width;
            height = Screen.height;
        }

        // Update is called once per frame
        void Update()
        {
            System.DateTime now = System.DateTime.Now;
            if (now < earliestNext)
            {
                return;
            }
            earliestNext = now + minInterval;
            StartCoroutine(captureScreenshot());
        }

        IEnumerator captureScreenshot()
        {
            yield return new WaitForEndOfFrame();

            num++;
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
            byte[] screenshotBytes = screenshot.EncodeToPNG();
            Destroy(screenshot);
            var framenum = Time.frameCount;
            var ts = getTs();
            string curFilename = filenameTemplate;
            curFilename = curFilename.Replace("{ts}", $"{ts}");
            curFilename = curFilename.Replace("{num}", $"{num}");
            curFilename = curFilename.Replace("{framenum}", $"{framenum}");

            string fullFilename = Path.Join(screenshotTargetDirectory, curFilename);
            File.WriteAllBytes(fullFilename,screenshotBytes);
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"num={num}, frame={framenum}, file={curFilename}");
#endif

            yield return null;
        }

    }
}