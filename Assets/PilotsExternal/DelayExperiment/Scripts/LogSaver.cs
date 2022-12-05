using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using VRT.Core;

public class LogSaver : MonoBehaviour {
    string LogUrl;
    string VideoUrl;
    string state;
    string segment;
    ExperimentController expCntrl;
    long duration;
    long start;
    Synchronizer synchronizer;
    Camera UserCamera;
    Vector3 head;
    Vector3 position;
    long delayValue;

    // Use this for initialization
    void Start () {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        Randomizer Rand = GetComponent<Randomizer>();
        expCntrl = GetComponent<ExperimentController>();
        synchronizer = GameObject.FindObjectOfType<Synchronizer>();
        BaseStats.Output("LogSaver",$"LogSaver_timestamp={DateTime.Now.TimeOfDay.TotalMilliseconds*1000000}"); // This is for mapping the times from logfiles.
        if (Rand.urlfile == null)
        {
#if UNITY_ANDROID
            LogUrl = "sdcard/Movies/Miro360Conf" + "/Result_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";

#else
         LogUrl = Application.persistentDataPath + "/Result_" + DateTime.Now.ToString("yyyyMMddHHmmss")+".txt";
#endif
            Rand.urlfile = LogUrl;
            Debug.Log(LogUrl);
        }
        else
        {
            LogUrl = Rand.urlfile;
        }
        VideoUrl = Rand.playlist[0];
        


        if (!File.Exists(LogUrl))
        {
            // Create a file to write to.
            
            using (StreamWriter sw = File.CreateText(LogUrl))
            {
                sw.WriteLine(DateTime.Now.ToLongDateString());
                
            }
            
        }
        segment =Rand.segment.ToString();
        duration = Rand.secuencias[0].duration;
        start = Rand.secuencias[0].start;
        delayValue = (long) Rand.secuencias[0].retardo_numerico;
        //StartCoroutine(updateGaze());


    }



    // Update is called once per frame
    void Update () {
        UserCamera = (UserCamera == null) ? GameObject.Find("TrackedCamera (Left)").GetComponentInChildren<Camera>(): UserCamera; // as the camera is created on a Start void of some class we will search for it in the void loop
        synchronizer = (synchronizer == null) ? GameObject.FindObjectOfType<Synchronizer>() : synchronizer;
        state = (synchronizer == null) ? "waiting" : "session";
        head = UserCamera.transform.eulerAngles;
        position = UserCamera.transform.position;
        delayValue = (state == "waiting") ? 0: synchronizer.currentLatency;

        //state = ExperimentController.;
        //state = (GetComponent<VideoPlayer>().isPlaying ? "Sync" : "IDLE");
        using (StreamWriter sw = File.AppendText(LogUrl))
        {
            sw.WriteLine((DateTime.Now.TimeOfDay.TotalMilliseconds*1000000).ToString() + ";"+segment+";" + delayValue.ToString() + ";" + state + ";LOOK_AT;" + head.x + ";" + head.y +";"+head.z + ";POSITION;" + position.x + ";" + position.y + ";" + position.z);
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("Application has ended after " + Time.time + " seconds");
    }












    /*void PrepareDone(VideoPlayer vp)
{


    string in_seq_method = "";
    if (( in_seq_method = Rand.secuencias[0].in_seq_method) != null){
        Secuencias in_seq = Rand.secuencias[0];
        if (in_seq_method == "sscqe")
        {
           GameObject.Find("Script").GetComponent<SSCQ>().enabled = true; SSQC to be developed
        }else if (in_seq_method == "ssdqe")
        {
            GameObject.Find("Script").GetComponent<Exit>().SetTimerOn(in_seq.ssdqe_duration, in_seq.ssdqe_start, in_seq.ssdqe_period, in_seq.ssdqe_total_number);
            GameObject.Find("Plane").SetActive(false);
        } else GameObject.Find("Plane").SetActive(false);
    }else GameObject.Find("Plane").SetActive(false);
    if (GameObject.Find("PlayListObject").GetComponent<Randomizer>().secuencias[0].stereo == "TopDown")
        skyboxmaterial.SetFloat("_Layout", 2.0f);

    else
        skyboxmaterial.SetFloat("_Layout", 0.0f);
    GetComponent<VideoPlayer>().Play();
}
public void WriteSSCQ(int points)
{
    using (StreamWriter sw = File.AppendText(LogUrl))
    {
        sw.WriteLine((DateTime.Now.TimeOfDay.TotalMilliseconds * 1000000).ToString() + "," + segment + "," + VideoUrl + "," + "questionnaire," + "SSCQE" + "," + points.ToString());

    }
}*/









}




