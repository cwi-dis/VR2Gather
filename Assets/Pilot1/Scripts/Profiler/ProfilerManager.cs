using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class ProfilerManager : MonoBehaviour {
    public string fileName = "profiler";
    public float SamplingRate = 1;
    public static ProfilerManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
    }

    public class BaseProfiler {
        public virtual void Flush() { }
        public virtual void AddFrameValues() { }
        public virtual void GetHeaders(StringBuilder sb) { }
        public virtual void GetFramesValues(StringBuilder sb, int frame) {  }
    }

    public class FPSProfiler : BaseProfiler {
        List<Vector2> data = new List<Vector2>();
        public override void Flush() {
            data.Clear();
        }
        public override void AddFrameValues() {
            data.Add(new Vector2(Time.frameCount, Time.frameCount / Time.time) );
        }
        public override void GetHeaders(StringBuilder sb) { sb.Append("FRAME;FPS;"); }
        public override void GetFramesValues(StringBuilder sb, int frame) {
            sb.AppendFormat("{0};{1:0.0000};", (int)data[frame].x, data[frame].y);
        }
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
        AddProfiler(new FPSProfiler());
    }

    float timeToNext = 0;
    int lineCount = 0;
    // Update is called once per frame
    void Update () {
        if (Time.time > 0)
        {
            timeToNext -= Time.deltaTime;
            if (timeToNext < 0)
            {
                timeToNext += SamplingRate;
                foreach (var profile in profiles)
                    profile.AddFrameValues();
                lineCount++;
            }
        }
    }

    
    private void OnApplicationQuit()
    {
        StringBuilder sb = new StringBuilder();
        
        foreach (var profile in profiles)
            profile.GetHeaders(sb);
        sb.Length--;
        sb.AppendLine();
        for (int i = 0; i < lineCount; i++)
        {
            foreach (var profile in profiles)
                profile.GetFramesValues(sb, i);
            sb.Length--;
            sb.AppendLine();
        }
        string time = System.DateTime.Now.ToString("yyyyMMddHmm");
        //System.IO.File.WriteAllText(string.Format("{0}/../{1}-{2}.csv", Application.dataPath, fileName, time), sb.ToString());

    }
}
