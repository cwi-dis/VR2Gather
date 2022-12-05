using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_SettingsPanel : MonoBehaviour
    {
        enum ValueToGet
        {
            Max = 0,
            Min,
            Value,
            DefaultValue
        }

        public enum ControlMode
        {
            FOV_Vertical = 0,
            FOV_Horizontal,
            Preset,
            MaxNum
        }

        float FOV_Vertical_Max = 1f, FOV_Vertical_Min = 0f, FOV_Vertical_Default = 1f, FOV_Vertical;
        float FOV_Horizontal_Max = 1f, FOV_Horizontal_Min = 0f, FOV_Horizontal_Default = 1f, FOV_Horizontal;

        [SerializeField] Text WarningText;

        bool isDefault = true;

        [Header("Slider UI")]
        [SerializeField] List<Slider> sliders;

        [Header("On/Off UI")]
        [SerializeField] List<Text> switches_status;

        List<float> Custom_Values = new List<float>();

        public void ResetPanelPos()
        {
            Transform targethandTrans = ViveSR_Experience.instance.targetHand.transform;
            transform.position = targethandTrans.position + targethandTrans.forward * 0.4f;
            transform.forward = targethandTrans.forward;
        }

        private void Awake()
        {
            ResetPanelPos();
            Reset();
            SetListener();
        }

        private void OnEnable()
        {
            Transform PlayerHeand = ViveSR_Experience.instance.PlayerHeadCollision.transform;
            transform.position = PlayerHeand.position + PlayerHeand.forward * 0.8f;
            transform.forward = PlayerHeand.forward;
        }

        void SetListener()
        {
            for (int modeNum = 0; modeNum < (int)ControlMode.MaxNum; modeNum++)
            {
                ControlMode controlmode = (ControlMode)modeNum; //prevents listener reference error

                if (modeNum < sliders.Count)
                {
                    Button Left_Btn, Right_Btn;
                    Left_Btn = sliders[modeNum].transform.Find("Left_Btn").GetComponent<Button>();
                    Right_Btn = sliders[modeNum].transform.Find("Right_Btn").GetComponent<Button>();
                    sliders[modeNum].onValueChanged.AddListener(x =>
                    {
                        ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Drag);
                        SetValue(controlmode, x);
                    });

                    Left_Btn.onClick.AddListener(() =>
                    {
                        ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Click);
                        AdjustValue(controlmode, false);
                    });

                    Right_Btn.onClick.AddListener(() =>
                    {
                        ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Click);
                        AdjustValue(controlmode, true);
                    });               
                }
                else
                {
                    switches_status[modeNum - sliders.Count].GetComponent<Button>().onClick.AddListener(() =>
                    {
                        ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Click);
                        AdjustValue(controlmode);
                    });
                }
            }
        }


        public void AdjustValue(ControlMode controlMode, bool isAdd = false)
        {
            switch (controlMode)
            {
                case ControlMode.Preset:
                    if (isDefault) LoadCustomValue();
                    else
                    {
                        SaveCustomValue();
                        Reset();
                    }

                    break;

                default:

                    float add = 0.05f;

                    sliders[(int)controlMode].value += isAdd ? add : -add;

                    switches_status[(int)ControlMode.Preset - sliders.Count].text = "Custom";
                    isDefault = false;

                    break;
            }
        }

        void SetValue(ControlMode controlMode, float Value)
        {
            switch(controlMode)
            {
                case ControlMode.FOV_Vertical:

                    FOV_Vertical = Value;
                    ViveSR_DualCameraRig.Instance.SetViewCameraFrame(FOV_Horizontal, FOV_Vertical);

                    if (FOV_Vertical != FOV_Vertical_Default)
                    {
                        isDefault = false;
                        switches_status[(int)ControlMode.Preset - sliders.Count].text = "Custom";
                    }
                    break;
                case ControlMode.FOV_Horizontal:

                    FOV_Horizontal = Value;            
                    ViveSR_DualCameraRig.Instance.SetViewCameraFrame(FOV_Horizontal, FOV_Vertical);

                    if (FOV_Horizontal != FOV_Horizontal_Default)
                    {
                        isDefault = false;
                        switches_status[(int)ControlMode.Preset - sliders.Count].text = "Custom";
                    }
                    break;
            }     
        }

        void SetDefaultSliderValue(ControlMode controlMode)
        {
            switch (controlMode)
            {
                case ControlMode.FOV_Vertical:
                    sliders[(int)ControlMode.FOV_Vertical].minValue = FOV_Vertical_Min;
                    sliders[(int)ControlMode.FOV_Vertical].maxValue = FOV_Vertical_Max;
                    sliders[(int)ControlMode.FOV_Vertical].value = FOV_Vertical = FOV_Vertical_Default;
                    ViveSR_DualCameraRig.Instance.SetViewCameraFrame(FOV_Horizontal, FOV_Vertical);
                    break;

                case ControlMode.FOV_Horizontal:
                    sliders[(int)ControlMode.FOV_Horizontal].minValue = FOV_Horizontal_Min;
                    sliders[(int)ControlMode.FOV_Horizontal].maxValue = FOV_Horizontal_Max;
                    sliders[(int)ControlMode.FOV_Horizontal].value = FOV_Horizontal = FOV_Horizontal_Default;
                    ViveSR_DualCameraRig.Instance.SetViewCameraFrame(FOV_Horizontal, FOV_Vertical);
                    break;
                case ControlMode.Preset:
                    break;
            }
        }

        void LoadCustomValue()
        {
            if (Custom_Values.Count > 0)
            {
                for (int i = 0; i < sliders.Count; i++)
                {
                    sliders[i].value = Custom_Values[i];
                }
                isDefault = false;
                switches_status[0].text = "Custom";
            }
        }
                 
        void SaveCustomValue()
        {
            Custom_Values.Clear();                                   
            for (int i = 0; i < sliders.Count; i++) Custom_Values.Add(GetValue((ControlMode)i, ValueToGet.Value));
        }

       float GetValue(ControlMode controlMode, ValueToGet ValueToGet)
        {
            switch (ValueToGet)
            {    
                case ValueToGet.Max:
                    return (controlMode == ControlMode.FOV_Vertical) ? FOV_Vertical_Max : FOV_Horizontal_Max;
                case ValueToGet.Min:
                    return (controlMode == ControlMode.FOV_Vertical) ? FOV_Vertical_Min : FOV_Horizontal_Min;
                case ValueToGet.Value:
                    return (controlMode == ControlMode.FOV_Vertical) ? FOV_Vertical : FOV_Horizontal;
                case ValueToGet.DefaultValue:
                    return (controlMode == ControlMode.FOV_Vertical) ? FOV_Vertical_Default : FOV_Horizontal_Default;
                default: return -1;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < (int)ControlMode.MaxNum; i++)
            {      
                SetDefaultSliderValue((ControlMode)i);
            } 

            isDefault = true;
            switches_status[0].text = "Default";
        }
    }
}