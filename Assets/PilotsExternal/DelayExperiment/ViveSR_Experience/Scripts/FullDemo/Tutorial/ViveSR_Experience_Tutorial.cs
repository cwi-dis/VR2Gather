using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Vive.Plugin.SR.Experience
{    
    public class ViveSR_Experience_Tutorial : MonoBehaviour
    {     
        [SerializeField] bool IsTutorialOn;

        //For storing SteamVR touchpad gameobject so as to position Tutorial canvases
        GameObject touchpadGameObj;
        bool isTouchpadUISet;

        [SerializeField] List<GameObject> LineManagers = new List<GameObject>();
        [SerializeField] List<ViveSR_Experience_Tutorial_IInputHandler> _InputHandlers; //For handling inputs for differernt buttons

        public Dictionary<MenuButton, ViveSR_Experience_Tutorial_MainLineManager> MainLineManagers = new Dictionary<MenuButton, ViveSR_Experience_Tutorial_MainLineManager>();
        public Dictionary<MenuButton, ViveSR_Experience_Tutorial_SubLineManager> SubLineManagers = new Dictionary<MenuButton, ViveSR_Experience_Tutorial_SubLineManager>();
        public Dictionary<MenuButton, ViveSR_Experience_Tutorial_IInputHandler> InputHandlers = new Dictionary<MenuButton, ViveSR_Experience_Tutorial_IInputHandler>();

        public ControllerInputIndex currentInput = ControllerInputIndex.none;
        public ControllerInputIndex previosuInput = ControllerInputIndex.none;
        public ControllerInputIndex previousSprite = ControllerInputIndex.none;
        public GameObject touchpadImageGroup;
        List<bool> touchpadImages_isFocused = new List<bool>() { false, false, false, false, false};

        public ViveSR_Experience_Tutorial_TouchpadImage touchpadScript;
        IEnumerator currentTouchPadCoroutine;

        [Header("Canvases")]
        [SerializeField] public List<GameObject> tutorialCanvases;
        [SerializeField] List<Text> tutorialTexts;

        public bool isTriggerPressed, isTouchpadPressed, wasTriggerCanvasActive;

        public void SetTutorialTransform()
        {
            switch (ViveSR_Experience.instance.CurrentDevice)
            {
                #region VIVE PRO
                case DeviceType.VIVE_PRO:
                    tutorialCanvases[(int)TextCanvas.onGrip].transform.localEulerAngles = new Vector3(20f, 0f, 0f);
                    tutorialCanvases[(int)TextCanvas.onGrip].transform.localPosition = new Vector3(-0.058f, -0.13f, -0.06f);
                    tutorialCanvases[(int)TextCanvas.onTouchPad].transform.localPosition = new Vector3(0.08f, -0.085f, -0.056f);
                    tutorialCanvases[(int)TextCanvas.onRotator].transform.localPosition = new Vector3(0f, -0.035f, -0.11f);
                    tutorialCanvases[(int)TextCanvas.onTrigger].transform.localEulerAngles = new Vector3(20f, 0f, 0f);
                    tutorialCanvases[(int)TextCanvas.onTrigger].transform.localPosition = new Vector3(-0.05f, -0.1f, -0.03f);
                    touchpadImageGroup.transform.localPosition = new Vector3(0f, -0.085f, -0.059f);
                    touchpadImageGroup.transform.localEulerAngles = new Vector3(-26f, 180f, -0.053f);
                    touchpadImageGroup.transform.localScale = new Vector3(0.0004f, 0.0004f, 0.0004f);
                    break;

                #endregion
                #region VIVE COSMOS
                case DeviceType.VIVE_COSMOS:

                    float reverse = 1;

                    if (ViveSR_Experience.instance.targetHand.handType == Valve.VR.SteamVR_Input_Sources.LeftHand)
                    {
                        tutorialCanvases[(int)TextCanvas.onTouchPad].transform.localPosition = new Vector3(-0.175f, -0.12f, -0.056f);
                        tutorialCanvases[(int)TextCanvas.onTouchPad].transform.GetChild(0).transform.localScale = new Vector3(-1f, 1f, 1f);

                        Transform gripBGImage = tutorialCanvases[(int)TextCanvas.onGrip].transform.GetChild(0);
                        gripBGImage.transform.localScale = new Vector3(-1f, 1f, 1f);
                        gripBGImage.transform.localPosition = new Vector3(-128f, gripBGImage.localPosition.y, gripBGImage.localPosition.z);

                        tutorialCanvases[(int)TextCanvas.onTrigger].transform.localPosition = new Vector3(-0.065f, -0.09f, 0.01f);
                        tutorialTexts[0].alignment = TextAnchor.MiddleRight;
                    }
                    else if (ViveSR_Experience.instance.targetHand.handType == Valve.VR.SteamVR_Input_Sources.RightHand)
                    {
                        reverse = -1;

                        tutorialCanvases[(int)TextCanvas.onTouchPad].transform.localPosition = new Vector3(0.165f, -0.12f, -0.056f);
                        tutorialCanvases[(int)TextCanvas.onTrigger].transform.localPosition = new Vector3(-0.08f, -0.11f, 0.01f);
                        tutorialTexts[0].alignment = TextAnchor.MiddleLeft;
                    }

                    tutorialCanvases[(int)TextCanvas.onRotator].transform.localPosition = new Vector3(0f, -0.015f, -0.11f);
                    tutorialCanvases[(int)TextCanvas.onGrip].transform.localPosition = new Vector3(0.085f * reverse, -0.138f, -0.03f);
                    tutorialCanvases[(int)TextCanvas.onGrip].transform.localEulerAngles = new Vector3(20f, 0f, 0f);
                    tutorialCanvases[(int)TextCanvas.onTrigger].transform.localEulerAngles = new Vector3(20f, 0f, 0f);

                    touchpadImageGroup.transform.localPosition = new Vector3(-0.07f * reverse, -0.12f, -0.059f);
                    touchpadImageGroup.transform.localEulerAngles = new Vector3(-26f, 180f, -0.053f);
                    touchpadImageGroup.transform.localScale = Vector3.one * 0.0008f;

                    break;

                #endregion
                default:
                    goto case DeviceType.VIVE_PRO;
            }
        }

        public void Init()
        {
            SetTutorialTransform();

            touchpadScript.SetColor(TouchpadDirection.Up, ViveSR_Experience_Demo.instance.DisableColor);
            touchpadScript.SetColor(TouchpadDirection.Down, ViveSR_Experience_Demo.instance.DisableColor);
            touchpadScript.SetColor(TouchpadDirection.Left, ViveSR_Experience_Demo.instance.BrightFrameColor);
            touchpadScript.SetColor(TouchpadDirection.Right, ViveSR_Experience_Demo.instance.BrightFrameColor);
            touchpadScript.SetColor(TouchpadDirection.Mid, ViveSR_Experience_Demo.instance.BrightFrameColor);

            for (int i = 0; i < (int)MenuButton.MaxNum; i++)
            {
                MainLineManagers[(MenuButton)i] = LineManagers[i].GetComponent<ViveSR_Experience_Tutorial_MainLineManager>();
                SubLineManagers[(MenuButton)i] = LineManagers[i].GetComponent<ViveSR_Experience_Tutorial_SubLineManager>();
                InputHandlers[(MenuButton)i] = _InputHandlers[i];

            }

            ToggleTutorial(IsTutorialOn);
            SetMainMessage();
            ViveSR_Experience_ControllerDelegate.touchpadDelegate_Late += HandleTouchpad_Tutorial;
            ViveSR_Experience_ControllerDelegate.triggerDelegate_Late += HandleTrigger_Tutorial;

            for (int i = 0; i < (int)ViveSR_Experience_Demo.instance.Rotator.IncludedBtns.Count; i++)
            {
                InputHandlers[(MenuButton)i].Init_Awake();
                InputHandlers[(MenuButton)i].Init_Start();
            }
        }

        void HandleTrigger_Tutorial(ButtonStage buttonStage, Vector2 axis)
        {
            ViveSR_Experience_IButton CurrentButton = ViveSR_Experience_Demo.instance.Rotator.CurrentButton;
            if (IsTutorialOn && CurrentButton.isOn)
            {
                switch (buttonStage)
                {
                    case ButtonStage.PressDown:
                        InputHandlers[CurrentButton.ButtonType].TriggerDown();
                        break;
                    case ButtonStage.PressUp:
                        InputHandlers[CurrentButton.ButtonType].TriggerUp();
                        break;
                }
            }
        }

        public void RunSpriteAnimation()
        {                                  
            if (currentInput != previousSprite)
            {
                //Previous...
                if (previousSprite != ControllerInputIndex.none)
                {   
                    if (touchpadScript.GetColor(previousSprite.ToTouchpadDirection()) != ViveSR_Experience_Demo.instance.DisableColor)
                        touchpadScript.SetColor(previousSprite.ToTouchpadDirection(), touchpadImages_isFocused[(int)previousSprite] ? ViveSR_Experience_Demo.instance.BrightFrameColor : ViveSR_Experience_Demo.instance.BrightFrameColor);
                }

                //Current...
                if (currentInput != ControllerInputIndex.none)
                {
                    SetCanvas(TextCanvas.onTouchPad, true);
                    touchpadScript.StartAnimate(currentInput.ToTouchpadDirection());
                }
                else
                {
                    SetCanvasText(TextCanvas.onTouchPad, "");
                    SetCanvas(TextCanvas.onTouchPad, false);
                    touchpadScript.StopAnimate();
                }
                previousSprite = currentInput;
            }
        }

        public ControllerInputIndex GetCurrentSprite(Vector2 axis)
        {               
            TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, true);
          
            switch (touchpadDirection)
            {
                case TouchpadDirection.Mid:
                    currentInput = ControllerInputIndex.mid;
                    break;
                case TouchpadDirection.Right:
                    currentInput = ControllerInputIndex.right;
                    break;
                case TouchpadDirection.Left:
                    currentInput = ControllerInputIndex.left;
                    break;
                case TouchpadDirection.Up:
                    currentInput = ControllerInputIndex.up;
                    break;
                case TouchpadDirection.Down:
                    currentInput = ControllerInputIndex.down;
                    break;
            }

            return currentInput;
        }   

        void HandleTouchpad_Tutorial(ButtonStage buttonStage, Vector2 axis)
        {
            if (IsTutorialOn)
            {
                previosuInput = currentInput;
                currentInput = GetCurrentSprite(axis);
                ViveSR_Experience_IButton CurrentButton = ViveSR_Experience_Demo.instance.Rotator.CurrentButton;

                #region VIVE PRO
                if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_PRO)
                {

                    switch (buttonStage)
                    {
                        case ButtonStage.Press:
                            InputHandlers[CurrentButton.ButtonType].MatchRotator();
                            break;
                        case ButtonStage.PressUp:
                            InputHandlers[CurrentButton.ButtonType].MatchRotatorUp();
                            break;
                        case ButtonStage.PressDown:
                            InputHandlers[CurrentButton.ButtonType].ConfirmSelection();
                            break;
                        case ButtonStage.TouchUp:
                            InputHandlers[CurrentButton.ButtonType].ResetTouchpadSprite();
                            break;
                        case ButtonStage.Touch:
                            InputHandlers[CurrentButton.ButtonType].SetTouchpadText(axis);
                            break;
                    }
                }
                #endregion
                #region VIVE COSMOS
                else if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_COSMOS)
                {
                    switch (buttonStage)
                    {
                        case ButtonStage.Press:
                            InputHandlers[CurrentButton.ButtonType].MatchRotator();
                            break;
                        case ButtonStage.PressUp:
                            InputHandlers[CurrentButton.ButtonType].MatchRotatorUp();
                            break;
                        case ButtonStage.PressDown:
                            InputHandlers[CurrentButton.ButtonType].ConfirmSelection();
                            break;
                        case ButtonStage.TouchUp:
                            InputHandlers[CurrentButton.ButtonType].ResetTouchpadSprite();
                            break;
                        case ButtonStage.Touch:
                            InputHandlers[CurrentButton.ButtonType].SetTouchpadText(axis);
                            if (currentInput != previosuInput)
                                InputHandlers[CurrentButton.ButtonType].MatchRotator();
                            break;
                    }
                }
                #endregion
            }
        }

        public void SetTouchpadSprite(bool isAvailable, params ControllerInputIndex[] indexes)
        {        
            foreach (ControllerInputIndex index in indexes)
            {
                touchpadScript.SetColor(index.ToTouchpadDirection(), isAvailable ? ViveSR_Experience_Demo.instance.BrightFrameColor : ViveSR_Experience_Demo.instance.DisableColor);
                touchpadScript.SetEnable(index.ToTouchpadDirection(), isAvailable);
            }
        }

        public void SetTouchpadSprite(bool isAvailable, bool isFocused, params ControllerInputIndex[] indexes)
        {
            SetTouchpadSpriteFocused(isFocused, indexes);
            SetTouchpadSprite(isAvailable, indexes);
        }

        void SetTouchpadSpriteFocused(bool isFocused, params ControllerInputIndex[] indexes)
        {
            foreach (ControllerInputIndex index in indexes)
            {
                touchpadImages_isFocused[(int)index] = isFocused;
            }
        }

        public void SetMainMessage()
        {
            string msgType;

            ViveSR_Experience_IButton CurrentButton = ViveSR_Experience_Demo.instance.Rotator.CurrentButton;

            if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_COSMOS && CurrentButton.ButtonType== MenuButton.CameraControl)
                msgType = "NotSupported_Cosmos";
            else if (ViveSR_Experience.instance.IsAMD && !CurrentButton.isAMDCompatible)
                msgType = "NotSupported_AMD";
            else if (CurrentButton.disabled)
                msgType = "Disabled";
            else if (CurrentButton.isOn)
                msgType = "On";
            else msgType = "Available";

            ViveSR_Experience_Tutorial_Line TextLineFound = MainLineManagers[CurrentButton.ButtonType].mainLines.FirstOrDefault(x => x.messageType == msgType);

            if (TextLineFound != null) SetCanvasText(TextCanvas.onRotator, MainLineManagers[CurrentButton.ButtonType].mainLines.First(x => x.messageType == msgType).text);
            else SetCanvasText(TextCanvas.onRotator, "Message Not Found.");
        }

        public void ToggleTutorial(bool isOn)
        {
            ViveSR_Experience_IButton CurrentButton = ViveSR_Experience_Demo.instance.Rotator.CurrentButton;

            IsTutorialOn = isOn;                                   
            if(!isOn) SetCanvas(TextCanvas.onTouchPad, false);
            SetCanvas(TextCanvas.onRotator, isOn);
            SetTouchpadImage(isOn);
            SetTouchpadSprite(!CurrentButton.disabled, ControllerInputIndex.mid);
            SetTouchpadSprite(isOn, ControllerInputIndex.left, ControllerInputIndex.right);

            if (!isOn)
            {
                currentInput = ControllerInputIndex.none;
                RunSpriteAnimation(); //stop previous coroutine;
            }
        }
                                                           
        public void SetCanvas(TextCanvas textCanvas, bool on)
        {
            tutorialCanvases[(int)textCanvas].SetActive(on);
        }
        public bool IsCanvasActive(TextCanvas textCanvas)
        {
            return tutorialCanvases[(int)textCanvas].activeSelf;
        }

        public void SetCanvasText(TextCanvas textCanvas, string text)
        {
            tutorialTexts[(int)textCanvas].text = text;
        }
        public void SetCanvasText(TextCanvas textCanvas, string text, Color color)
        {
            tutorialTexts[(int)textCanvas].text = text;
            tutorialTexts[(int)textCanvas].color = color;
        }
        public void SetTouchpadImage(bool on)
        {
            touchpadImageGroup.SetActive(on);
        }

        public void ResetTouchpadImageSprite()
        {
            touchpadScript.ResetSprite();
        }
    }
}