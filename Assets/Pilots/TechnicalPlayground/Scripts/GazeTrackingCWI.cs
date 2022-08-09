using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

public class GazeTrackingCWI : MonoBehaviour
{
    public int LengthOfRay = 25;
    [SerializeField] private LineRenderer GazeRayRenderer;
    private static EyeData_v2 eyeData = new EyeData_v2();
    private bool eye_callback_registered = false;
    public GameObject EyeR;
    public GameObject EyeL;
    public GameObject EyeCombi;

    //stats
    public bool writeStats;
    public double interval = 0;
    static int instanceCounter = 0;
    int instanceNumber = instanceCounter++;
    private void Start()
    {
        if (!SRanipal_Eye_Framework.Instance.EnableEye)
        {
            enabled = false;
            return;
        }
        Assert.IsNotNull(GazeRayRenderer);

        stats = new Stats(Name(), interval);
    }

    private void Update()
    {
        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
            SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;

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

        EyeR.gameObject.transform.position = Camera.main.transform.TransformPoint(eyeData.verbose_data.right.gaze_origin_mm / 1000.0f);
        EyeL.gameObject.transform.position = Camera.main.transform.TransformPoint(eyeData.verbose_data.left.gaze_origin_mm / 1000.0f);
        Vector3 combinedEyePosition = Camera.main.transform.TransformPoint(eyeData.verbose_data.combined.eye_data.gaze_origin_mm / 1000.0f);
        EyeCombi.gameObject.transform.position = combinedEyePosition;


        Vector3 GazeOriginCombinedLocal, GazeDirectionCombinedLocal;

        if (eye_callback_registered)
        {
            if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
            else return;
        }
        else
        {
            if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
            else return;
        }

        Vector3 combinedGazeDirection = Camera.main.transform.TransformDirection(GazeDirectionCombinedLocal);
        //GazeRayRenderer.SetPosition(0, Camera.main.transform.position - Camera.main.transform.up * 0.05f); //- Camera.main.transform.up * 0.05f
        //GazeRayRenderer.SetPosition(1, Camera.main.transform.position + GazeDirectionCombined * LengthOfRay);
        GazeRayRenderer.SetPosition(0, combinedEyePosition);
        GazeRayRenderer.SetPosition(1, combinedEyePosition + combinedGazeDirection * LengthOfRay);

        if (writeStats)
        {
            stats.statsUpdate(combinedEyePosition, combinedGazeDirection, 0);
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

        public void statsUpdate(Vector3 pos, Vector3 dir, long pc_timestamp)
        {
            if (ShouldOutput())
            {
                Output($"px={pos.x:f2}, py={pos.y:f2}, pz={pos.z:f2}, rx={dir.x:f2}, ry={dir.y:f2}, rz={dir.z:f2}, pc_timestamp={pc_timestamp}");
                Clear();
            }
        }
    }

    protected Stats stats;
}
