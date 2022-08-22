using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

public class GazeTracker : MonoBehaviour
{
    private static EyeData_v2 eyeData = new EyeData_v2();
    private bool eye_callback_registered = false;

    //stats
    public bool writeStats;
    public double interval = 0;
    static int instanceCounter = 0;
    int instanceNumber = instanceCounter++;
    protected Stats stats;
    private void Start()
    {
        //launch calibration?
        //SRanipal_Eye_API.LaunchEyeCalibration(System.IntPtr.Zero);
        if (!SRanipal_Eye_Framework.Instance.EnableEye)
        {
            enabled = false;
            return;
        }
        stats = new Stats(Name(), interval);
    }

    private void Update()
    {
        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
            SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
        {
            Debug.LogWarning(Name() + " Eye tracking framework not working or not supported");
            return;
        }

        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
        {
            SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = true;
        }
        else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }

        if (eye_callback_registered)
        {

            if (writeStats)
            {
                stats.statsUpdate(eyeData, Camera.main, 0);
            }
        }
    }

    public string Name()
    {
        return $"{GetType().Name}#{transform.parent.gameObject.name}.{instanceNumber}";
    }

    private void Release()
    {
        if (eye_callback_registered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }
    }
    private static void EyeCallback(ref EyeData_v2 eye_data)
    {
        eyeData = eye_data;
    }

    protected class Stats : VRT.Core.BaseStats
    {
        public Stats(string name, double interval) : base(name, interval)
        {
        }

        public void statsUpdate(EyeData_v2 eyeData, Camera camera, long pc_timestamp)
        {
            if (ShouldOutput())
            {
                //Output($"px={pos.x:f2}, py={pos.y:f2}, pz={pos.z:f2}, rx={dir.x:f2}, ry={dir.y:f2}, rz={dir.z:f2}, pc_timestamp={pc_timestamp}");
                Output(JsonUtility.ToJson(eyeData));
                Clear();
            }
        }
    }

}
