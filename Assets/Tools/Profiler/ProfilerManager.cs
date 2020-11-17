using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class ProfilerManager : MonoBehaviour {
    public string fileName = "Profiler";
    public string csvOutputPathname;
    public float SamplingRate = 1/30f;
    public bool FPSActive = true;
    public bool HMDActive = Config.Instance.pilot3NavigationLogs;
    public bool TVMActive = false;
    public GameObject[] TVMs;
    private bool printedStatMessage = false;

    private float timeToNext = 0.0f;
    private uint lineCount = 0;
    private Transform HMD;

    public static ProfilerManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
        csvOutputPathname = string.Format("{0}/../{1}.csv", Application.persistentDataPath, fileName);

        //Moved to update for pilot 3
        //HMD = FindObjectOfType<Camera>().gameObject.transform;

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
        // xxxjack I don't like all this comment-edout code.... Why? Why not set the corresponding boolean to false?
        //if (FPSActive) AddProfiler(new FPSProfiler());
        //Moved to update for pilot 3
        //if (HMDActive) AddProfiler(new HMDProfiler(HMD));
        if (TVMActive) AddProfiler(new TVMProfiler(TVMs));
    }

    // Update is called once per frame
    void Update () {
        // xxxjack should bail out quickly if no profilers active
        if (!printedStatMessage)
        {
            Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}, component=ProfilerManager, started=1, csv_output={csvOutputPathname}, TimeSinceGameStart={Time.time}, FPSActive={FPSActive}, HMDActive={HMDActive}, TVMActive={TVMActive}");
            printedStatMessage = true;
        }
        if (HMDActive == true)
        {
            var cam = FindObjectOfType<Camera>().gameObject;
            if (cam!=null)
            {
                HMD = cam.transform;
                AddProfiler(new FPSProfiler());
                AddProfiler(new HMDProfiler(HMD));
                HMDActive = false;
            }
        }
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
        // xxxjack should bail out quickly if no profilers active
        // xxxjack seems pretty scary to not write before OnApplicationQuit....
        //UnityEngine.Debug.Log("<color=red>XXXShishir: </color> Writing nav logs to " + string.Format("{0}/../{1}.csv", Application.persistentDataPath, fileName));
        StringBuilder sb = new StringBuilder();
        Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}, component=ProfilerManager, finished=1, csv_output={csvOutputPathname}, TimeSinceGameStart={Time.time}");
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
            //string time = System.DateTime.Now.ToString("yyyyMMddHmm");
            System.IO.File.WriteAllText(csvOutputPathname, sb.ToString());
        }
    }
}
