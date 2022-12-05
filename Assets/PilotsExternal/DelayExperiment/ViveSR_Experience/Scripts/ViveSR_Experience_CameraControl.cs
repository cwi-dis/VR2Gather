using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_CameraControl : MonoBehaviour
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
            Brightness = 0,
            Contrast,
            Saturation,
            WhiteBalance,
            WhiteBalanceMode,
            Preset,
            MaxNum
        }

        [SerializeField] Text WarningText;

        bool isWhiteBalanceManual = false;
        bool isDefault = true;

        [Header("Slider UI")]
        [SerializeField] List<Slider> sliders;

        [Header("On/Off UI")]
        [SerializeField] List<Text> switches_status;

        List<int> Custom_Values = new List<int>();
        bool Custom_isWhiteBalanceManual;

        List<Button> whiteBalanceButtons = new List<Button>();

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

            if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_COSMOS)
            {
                WarningText.gameObject.SetActive(true);

                for(int i = 1; i< gameObject.transform.childCount; ++i)
                {
                    gameObject.transform.GetChild(i).gameObject.SetActive(false);
                }
                return;
            }

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
                        SetValue(controlmode, (int)x);
                    });

                    Left_Btn.onClick.AddListener(() => 
                    {
                        ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Click);
                        AdjustValue(controlmode, false); });

                    Right_Btn.onClick.AddListener(() => 
                    {
                        ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Click);
                        AdjustValue(controlmode, true);
                    });

                    if (controlmode == ControlMode.WhiteBalance)
                    {
                        whiteBalanceButtons.Add(Left_Btn);
                        whiteBalanceButtons.Add(Right_Btn);
                    }
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

        void SetWhiteBalanceMode(bool isManual)
        {
            if (isWhiteBalanceManual != isManual)
            {
                isWhiteBalanceManual = isManual;

                ViveSR_DualCameraImageCapture.CameraQualityInfo camInfo = new ViveSR_DualCameraImageCapture.CameraQualityInfo();
                ViveSR_DualCameraImageCapture.GetCameraQualityInfo(ViveSR_DualCameraImageCapture.CameraQuality.WHITE_BALANCE, ref camInfo);

                switches_status[0].text = isManual ? "Manual" : "Auto";
                sliders[(int)ControlMode.WhiteBalance].interactable = isManual;
                foreach (Button btn in whiteBalanceButtons) btn.interactable = isManual;
                camInfo.Mode = isManual ? 2 : 1;
                if (isManual)
                {
                    switches_status[(int)ControlMode.Preset - sliders.Count].text = "Custom";
                    isDefault = false;
                }
                ViveSR_DualCameraImageCapture.SetCameraQualityInfo(ViveSR_DualCameraImageCapture.CameraQuality.WHITE_BALANCE, camInfo);
            }
        }                                           

        public void AdjustValue(ControlMode controlMode, bool isAdd = false)
        {
            switch (controlMode)
            {                
                case ControlMode.WhiteBalanceMode:

                    SetWhiteBalanceMode(!isWhiteBalanceManual);

                    break;
                case ControlMode.Preset:
                    if (isDefault) LoadCustomValue();
                    else
                    {
                        SaveCustomValue();
                        Reset();
                    }                     
                  
                    break;

                default:

                    int add = controlMode == ControlMode.WhiteBalance ? 60 : 2;

                    sliders[(int)controlMode].value += isAdd ? add : -add;

                    if (!(controlMode == ControlMode.WhiteBalance && !isWhiteBalanceManual))
                    {
                        switches_status[(int)ControlMode.Preset - sliders.Count].text = "Custom";
                        isDefault = false;
                    }

                    break;
            }
        }

        void SetValue(ControlMode controlMode, int Value)
        {         
            if (!(controlMode == ControlMode.WhiteBalance && !isWhiteBalanceManual))
            {
                ViveSR_DualCameraImageCapture.CameraQualityInfo camInfo = new ViveSR_DualCameraImageCapture.CameraQualityInfo();
                int result = ViveSR_DualCameraImageCapture.GetCameraQualityInfo(ToCameraQuality(controlMode), ref camInfo);

                if (result != (int)Error.WORK) return;

                if (camInfo.Value != Value)
                {
                    isDefault = false;
                    switches_status[(int)ControlMode.Preset - sliders.Count].text = "Custom";
                    camInfo.Value = Value;

                    ViveSR_DualCameraImageCapture.SetCameraQualityInfo(ToCameraQuality(controlMode), camInfo);
                } 
            }     
        }

        int GetValue(ControlMode controlMode, ValueToGet ValueToGet)
        {
            ViveSR_DualCameraImageCapture.CameraQualityInfo camInfo = new ViveSR_DualCameraImageCapture.CameraQualityInfo();
            int result = ViveSR_DualCameraImageCapture.GetCameraQualityInfo(ToCameraQuality(controlMode), ref camInfo);

            if (result != (int)Error.WORK) return -1;

            switch (ValueToGet)
            {
                case ValueToGet.Max:
                    return camInfo.Max;
                case ValueToGet.Min:
                    return camInfo.Min;
                case ValueToGet.Value:
                    return camInfo.Value;
                case ValueToGet.DefaultValue:
                    return camInfo.DefaultValue;
                default: return -1;
            }
        }

        void SetDefaultSliderValue(ControlMode controlMode)
        {
            switch (controlMode)
            {
                case ControlMode.WhiteBalanceMode:
                    SetWhiteBalanceMode(false);
                    break;
                case ControlMode.Preset:
                    break;
                default:
                    ViveSR_DualCameraImageCapture.CameraQualityInfo camInfo = new ViveSR_DualCameraImageCapture.CameraQualityInfo();
                    ViveSR_DualCameraImageCapture.GetCameraQualityInfo(ToCameraQuality(controlMode), ref camInfo);

                    sliders[(int)controlMode].maxValue = GetValue(controlMode, ValueToGet.Max);
                    sliders[(int)controlMode].minValue = GetValue(controlMode, ValueToGet.Min);
                    sliders[(int)controlMode].value = camInfo.Value = GetValue(controlMode, ValueToGet.DefaultValue);

                    ViveSR_DualCameraImageCapture.SetCameraQualityInfo(ToCameraQuality(controlMode), camInfo);
                    break;
            }
        }

        ViveSR_DualCameraImageCapture.CameraQuality ToCameraQuality(ControlMode controlMode)
        {
            switch (controlMode)
            {
                case ControlMode.Brightness:
                    return ViveSR_DualCameraImageCapture.CameraQuality.BRIGHTNESS;
                case ControlMode.Contrast:
                    return ViveSR_DualCameraImageCapture.CameraQuality.CONTRAST;
                case ControlMode.Saturation:
                    return ViveSR_DualCameraImageCapture.CameraQuality.SATURATION;
                case ControlMode.WhiteBalance:
                    return ViveSR_DualCameraImageCapture.CameraQuality.WHITE_BALANCE;
                default:
                    return 0;
            }
        }

        void LoadCustomValue()
        {
            if (Custom_Values.Count > 0)
            {
                SetWhiteBalanceMode(Custom_isWhiteBalanceManual);
                for (int i = 0; i < sliders.Count; i++)
                {
                    sliders[i].value = Custom_Values[i];
                }
            }
            isDefault = false;
            switches_status[1].text = "Custom";
        }

        void SaveCustomValue()
        {
            Custom_Values.Clear();
            Custom_isWhiteBalanceManual = isWhiteBalanceManual;
            for (int i = 0; i < sliders.Count; i++) Custom_Values.Add(GetValue((ControlMode)i, ValueToGet.Value));
        }

        public void Reset()
        {
            for (int i = 0; i < (int)ControlMode.MaxNum; i++)
            {
                SetDefaultSliderValue((ControlMode)i);
            }
            isDefault = true;
            switches_status[1].text = "Default";
        }
    }
}