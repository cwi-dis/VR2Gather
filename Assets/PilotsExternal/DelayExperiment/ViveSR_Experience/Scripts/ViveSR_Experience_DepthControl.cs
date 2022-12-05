using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{
    enum ValueToGet
    {
        Max = 0,
        Min,
        Value,
        Add,
        DefaultValue
    }

    public enum ControlMode
    {
        ConfidenceThreshold = 0,
        DenoiseGuidedFilter,
        DenoiseMedianFilter,
        Refinement,
        EdgeEnhance,
        DepthCase,
        MaxNum,
    }

    public class ViveSR_Experience_DepthControl : MonoBehaviour
    {     
        [SerializeField] Material depthImageMaterial;

        [Header("Slider UI")]
        [SerializeField] List<Slider> sliders;
        [Header("On/Off UI")]
        [SerializeField] List<Text> switches_status;

        List<float> Default_Values = new List<float>();
        bool Default_isDepthRefinementOn;
        bool Default_isEdgeEnhanceOn;
        DepthCase Default_DepthCase;

        float ConfidenceThreshold_old;

        List<Button> Left_Btns = new List<Button>();
        List<Button> Right_Btns = new List<Button>();

        bool refinement_setting;

        public void ResetPanelPos()
        {
            Transform targethandTrans = ViveSR_Experience.instance.targetHand.transform;
            transform.position = targethandTrans.position + targethandTrans.forward * 0.4f;
            transform.forward = targethandTrans.forward;
        }

        void Awake()
        {
            //Assign depthImageMaterial to ViveSR.
            if (ViveSR_DualCameraRig.Instance.DualCameraImageRenderer.DepthMaterials.Count > 0)
                ViveSR_DualCameraRig.Instance.DualCameraImageRenderer.DepthMaterials[0] = depthImageMaterial;
            else
                ViveSR_DualCameraRig.Instance.DualCameraImageRenderer.DepthMaterials.Add(depthImageMaterial);

            SetListener(true);
        }

        private void OnEnable()
        {
            ViveSR_DualCameraImageCapture.SetDepthCase(DepthCase.CLOSE_RANGE);

            MatchUIWithSRWorksSetting();

            Transform PlayerHeand = ViveSR_Experience.instance.PlayerHeadCollision.transform;
            transform.position = PlayerHeand.position + new Vector3(0, -0.25f, 0) + PlayerHeand.forward * 0.8f;
            transform.forward = PlayerHeand.forward;
            ViveSR_DualCameraImageCapture.EnableDepthProcess(true);
            ViveSR_DualCameraImageRenderer.UpdateDepthMaterial = true;
        }
        private void OnDisable()
        {
            ViveSR_DualCameraImageRenderer.UpdateDepthMaterial = false;
            ViveSR_DualCameraImageCapture.EnableDepthProcess(false);
        }

        void SetListener(bool isOn)
        {
            for (int modeNum = 0; modeNum < (int)ControlMode.MaxNum; modeNum++)
            {
                ControlMode controlmode = (ControlMode)modeNum; //prevents listener reference error

                if (modeNum < sliders.Count)
                {
                    Left_Btns.Add(sliders[modeNum].transform.Find("Left_Btn").GetComponent<Button>());
                    Right_Btns.Add(sliders[modeNum].transform.Find("Right_Btn").GetComponent<Button>());
                    sliders[modeNum].onValueChanged.AddListener(x =>
                    {
                        ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Drag);
                        SetValue(controlmode, (int)x);
                    });
                    Left_Btns[modeNum].onClick.AddListener(() =>
                    {
                        ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Click);
                        AdjustValue(controlmode, false);
                    });
                    Right_Btns[modeNum].onClick.AddListener(() =>
                    {
                        ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Click);
                        AdjustValue(controlmode, true);
                    });
                }
                else
                {
                    switches_status[modeNum - sliders.Count].GetComponent<Button>().onClick.AddListener(() =>
                    {
                        ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Drag);
                        AdjustValue(controlmode);                    
                    });
                }
            }
        }

        public void AdjustValue(ControlMode controlMode, bool isAdd = false)
        {
            switch (controlMode)
            {
                case ControlMode.Refinement: 
                    SetRefinement(!ViveSR_DualCameraImageCapture.IsDepthRefinementEnabled);
                    break;

                case ControlMode.EdgeEnhance:
                    SetEdgeEnhance(!ViveSR_DualCameraImageCapture.IsDepthEdgeEnhanceEnabled);
                    break;

                case ControlMode.DepthCase:
                    SetDepthCase(ViveSR_DualCameraImageCapture.DepthCase == DepthCase.DEFAULT ? DepthCase.CLOSE_RANGE : DepthCase.DEFAULT);
                    break;

                default:
                    
                    float add = GetValue(controlMode, ValueToGet.Add);

                    sliders[(int)controlMode].value += isAdd ? add : -add;

                    break;
            }
        }

        void SetRefinement(bool isOn)
        {
            ViveSR_DualCameraImageCapture.EnableDepthRefinement(isOn);

            switches_status[(int)ControlMode.Refinement - sliders.Count].text = ViveSR_DualCameraImageCapture.IsDepthRefinementEnabled ? "On" : "Off";
        }

        void SetEdgeEnhance(bool isOn)
        {
            ViveSR_DualCameraImageCapture.EnableDepthEdgeEnhance(isOn);

            switches_status[(int)ControlMode.EdgeEnhance - sliders.Count].text = ViveSR_DualCameraImageCapture.IsDepthEdgeEnhanceEnabled ? "On" : "Off";
        }

        void SetDepthCase(DepthCase DepthCase)
        {
            ViveSR_DualCameraImageCapture.SetDepthCase(DepthCase);
            switches_status[(int)ControlMode.DepthCase - sliders.Count].text = ViveSR_DualCameraImageCapture.DepthCase == DepthCase.DEFAULT ? "Default" : "Close Range";
        }

        float GetValue(ControlMode controlMode, ValueToGet ValueToGet)
        {
            switch (controlMode)
            {
                case ControlMode.ConfidenceThreshold:
                    switch (ValueToGet)
                    {
                        case ValueToGet.Max:
                            return 9;
                        case ValueToGet.Min:
                            return 0;
                        case ValueToGet.Value:
                            return sliders[(int)controlMode].value;
                        case ValueToGet.Add:
                            return 1f;
                        case ValueToGet.DefaultValue:
                            return 3;
                        default:
                            return -1;
                    }
                case ControlMode.DenoiseGuidedFilter:
                    switch (ValueToGet)
                    {
                        case ValueToGet.Max:
                            return 7;
                        case ValueToGet.Min:
                            return 0;
                        case ValueToGet.Value:
                            return sliders[(int)controlMode].value;
                        case ValueToGet.Add:
                            return 1;
                        case ValueToGet.DefaultValue:
                            return 3;
                        default:
                            return -1;
                    }
                case ControlMode.DenoiseMedianFilter:
                    switch (ValueToGet)
                    {
                        case ValueToGet.Max:
                            return 2;
                        case ValueToGet.Min:
                            return 0;
                        case ValueToGet.Value:
                            return sliders[(int)controlMode].value;
                        case ValueToGet.Add:
                            return 1;
                        case ValueToGet.DefaultValue:
                            return 2;
                        default:
                            return -1;
                    }
                default:
                    return -1;
            }
        }

        void SetValue(ControlMode controlMode, float SliderValue) 
        {
            switch (controlMode)
            {
                case ControlMode.ConfidenceThreshold:

                    int A = (int)sliders[(int)controlMode].minValue;
                    int B = (int)sliders[(int)controlMode].maxValue;
                    int C = -1;
                    int D = 1;

                    float value = (SliderValue - A) / (B - A) * (D - C) + C;//map from from 0~9 to -1~1 

                    ViveSR_DualCameraImageCapture.DepthConfidenceThreshold = Mathf.Pow(10f, value) / 2 - 0.05f; //smaller numbers have more details

                    break;
                case ControlMode.DenoiseGuidedFilter:

                    ViveSR_DualCameraImageCapture.DepthDenoiseGuidedFilter = (int)SliderValue; //0-7

                    break;
                case ControlMode.DenoiseMedianFilter:

                    ViveSR_DualCameraImageCapture.DepthDenoiseMedianFilter = (int)SliderValue + (int)SliderValue + 1; //1, 3, 5 
                    break;
            }
        }

        public void MatchUIWithSRWorksSetting()
        {
            Default_Values.Clear();

            for (int i = 0; i < sliders.Count; i++) Default_Values.Add(GetValue((ControlMode)i, ValueToGet.Value));

            int result = SRWorkModule_API.GetDepthParameterBool((int)DepthCmd.ENABLE_REFINEMENT, ref refinement_setting);
            if (result == (int)Error.WORK) ViveSR_DualCameraImageCapture.IsDepthRefinementEnabled = refinement_setting;
            else Debug.LogWarning("Depth refinemenet call status: " + (Error)result);

            Default_isDepthRefinementOn = ViveSR_DualCameraImageCapture.IsDepthRefinementEnabled;
            Default_isEdgeEnhanceOn = ViveSR_DualCameraImageCapture.IsDepthEdgeEnhanceEnabled;
            Default_DepthCase = ViveSR_DualCameraImageCapture.DepthCase;
                                                                                                               
            for (int i = 0; i < sliders.Count; i++) sliders[i].value = GetValue((ControlMode)i, ValueToGet.Value);
            switches_status[(int)ControlMode.Refinement - sliders.Count].text = Default_isDepthRefinementOn ? "On" : "Off";
            switches_status[(int)ControlMode.EdgeEnhance - sliders.Count].text = Default_isEdgeEnhanceOn ? "On" : "Off";
            switches_status[(int)ControlMode.DepthCase - sliders.Count].text = Default_DepthCase == DepthCase.DEFAULT ? "Default" : "Close Range";
        }

        public void LoadDefaultValue()
        {
            if (Default_Values.Count == 0) return;

            for (int i = 0; i < sliders.Count; i++) sliders[i].value = Default_Values[i];
            SetRefinement(Default_isDepthRefinementOn);
            SetEdgeEnhance(Default_isEdgeEnhanceOn);
            SetDepthCase(Default_DepthCase);
        }
    }
}