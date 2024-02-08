using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Cwipc;


public class HeadRotationPositionSave : MonoBehaviour
{
    private string logUrl;
    private static string fileName;
    private string state;


    // Start is called before the first frame update
    void Start()
    {
        InitializeLog();
    }

    // Update is called once per frame
    void Update()
    {
        RecordCameraPositionAndOrientation();
    }

    void InitializeLog()
    {
        // Set culture for consistent datetime formatting
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

        // Determine log file path based on platform
        logUrl = Application.persistentDataPath + "/Log_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";

        // Create a new log file with the current date
        using (StreamWriter sw = File.CreateText(logUrl))
        {
            sw.WriteLine("Log started on " + DateTime.Now.ToLongDateString());
        }
        Statistics.Output("HeadRotationPositionSave", $"logFile={logUrl},millis={DateTime.Now.TimeOfDay.TotalMilliseconds}");
    }

    void RecordCameraPositionAndOrientation()
    {
        Camera userCamera = Camera.main;
        
        if (userCamera == null)
        {
            Debug.LogError("No Main Camera is found");
        }

        else
        {
            // Record current state (Excercise scene) 
            //state = "ExcerciseScene"; 

            // Get camera position and orientation
            Vector3 head = userCamera.transform.eulerAngles;
            Vector3 position = userCamera.transform.position;

            // Append record to the log file
            using (StreamWriter sw = File.AppendText(logUrl))
            {
                string logEntry = $"{DateTime.Now.TimeOfDay.TotalMilliseconds};LOOK_AT;{head.x};{head.y};{head.z};POSITION;{position.x};{position.y};{position.z}";
                sw.WriteLine(logEntry);
            }
        }
    }
    void OnDestroy()
    {
        Debug.Log("Application has ended after " + Time.time + " seconds");
    } 
}
