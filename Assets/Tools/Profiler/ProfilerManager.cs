using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class ProfilerManager : MonoBehaviour {
    public string fileName = "Profiler";
    public float SamplingRate = 1.0f;
    public bool FPSActive = false;
    //public bool HMDActive = Config.Instance.pilot3NavigationLogs;
    public bool HMDActive = true;
    public bool TVMActive = false;
    public GameObject[] TVMs;

    private float timeToNext = 0.0f;
    private uint lineCount = 0;
    private Transform HMD;

    public static ProfilerManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
        HMD = FindObjectOfType<Camera>().gameObject.transform;
    }       

    List<BaseProfiler> profiles = new List<BaseProfiler>();

    public void AddProfiler(BaseProfiler profiler) {
        profiles.Add(profiler);
        foreach (var profile in profiles)
            profile.Flush();
        timeToNext = 0;
        lineCount = 0;
    }

    // Use this for initialization
    void Start () {
        if (FPSActive) AddProfiler(new FPSProfiler());
        if (HMDActive) AddProfiler(new HMDProfiler(HMD));
        if (TVMActive) AddProfiler(new TVMProfiler(TVMs));
    }

    // Update is called once per frame
    void Update () {
        if (Time.time > 0) {
            timeToNext -= Time.deltaTime;
            if (timeToNext < 0.0f) {
                timeToNext += SamplingRate;
                foreach (var profile in profiles)
                    profile.AddFrameValues();
                lineCount++;
            }
        }
    }
    
    private void OnApplicationQuit() {
        UnityEngine.Debug.Log("<color=red>XXXShishir: </color> Writing nav logs to " + string.Format("{0}/../{1}.csv", Application.persistentDataPath, fileName));
        StringBuilder sb = new StringBuilder();
        if (profiles.Count > 0) {
            foreach (var profile in profiles)
                profile.GetHeaders(sb);
            sb.Length--;
            sb.AppendLine();
            for (int i = 0; i < lineCount; i++) {
                foreach (var profile in profiles)
                    profile.GetFramesValues(sb, i);
                sb.Length--;
                sb.AppendLine();
            }
            string time = System.DateTime.Now.ToString("yyyyMMddHmm");
            System.IO.File.WriteAllText(string.Format("{0}/../{1}-{2}.csv", Application.persistentDataPath, fileName, time), sb.ToString());
        }
    }
}
