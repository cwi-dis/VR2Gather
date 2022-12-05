using UnityEngine;
using System.Linq;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Tutorial_IInputHandler : MonoBehaviour
    {
        protected ViveSR_Experience_IButton Button;
        protected ViveSR_Experience_ISubMenu SubMenu;
        protected ViveSR_Experience_Tutorial tutorial;

        public void Init_Awake()
        {
             AwakeToDo();
        }

        private void Update()
        {
            UpdateToDo();

        }

        protected virtual void AwakeToDo() { }
        protected virtual void StartToDo() { }
        protected virtual void UpdateToDo() { }

        public void Init_Start()
        {
            tutorial = ViveSR_Experience_Demo.instance.Tutorial;

            if(!ReferenceEquals(Button.SubMenu, null)) SubMenu = Button.SubMenu;

            StartToDo();       
        }

        ViveSR_Experience_Tutorial_Line FindTutorialLine(bool isTriggerOn)
        {
            if(isTriggerOn) return tutorial.MainLineManagers[Button.ButtonType].triggerFunctionControllerTexts.FirstOrDefault(x => x.messageType == tutorial.currentInput._ToString());
            else return tutorial.MainLineManagers[Button.ButtonType].controllerTexts.FirstOrDefault(x => x.messageType == tutorial.currentInput._ToString());
        }

        ViveSR_Experience_Tutorial_Line FindTutorialLine_SubMenu(bool isTriggerOn)
        {
            if(isTriggerOn) return tutorial.SubLineManagers[Button.ButtonType].triggerFunctionControllerTexts[SubMenu.SelectedButton].lines.FirstOrDefault(x => x.messageType == tutorial.currentInput._ToString());
            else return tutorial.SubLineManagers[Button.ButtonType].controllerTexts[SubMenu.HoverredButton].lines.FirstOrDefault(x => x.messageType == tutorial.currentInput._ToString());
        }

        public virtual void SetTouchpadText(Vector2 touchpad)
        {
            if (!tutorial) return;

            tutorial.currentInput = tutorial.GetCurrentSprite(touchpad);

            if (tutorial.touchpadScript.IsDisabled(tutorial.currentInput.ToTouchpadDirection())) return;
            
            tutorial.RunSpriteAnimation();

            ViveSR_Experience_Tutorial_Line textLine = null;

            if (SubMenu == null && Button.isOn)
            {
                textLine = FindTutorialLine(tutorial.isTriggerPressed);
            }
            else if (SubMenu != null && SubMenu.subBtnScripts[SubMenu.HoverredButton].isOn)
            {
                if (SubMenu.HoverredButton < tutorial.SubLineManagers[Button.ButtonType].controllerTexts.Count)
                    textLine = FindTutorialLine_SubMenu(tutorial.isTriggerPressed);
            }

            if (textLine != null)
                tutorial.SetCanvasText(TextCanvas.onTouchPad, textLine.text, ViveSR_Experience_Demo.instance.AttentionColor);
            else
                tutorial.SetCanvasText(TextCanvas.onTouchPad, GetDefaultControllerTextsMessage(tutorial.currentInput), ViveSR_Experience_Demo.instance.OriginalEmissionColor);

            if (tutorial.isTriggerPressed) return;

            if (Button.isOn && SubMenu != null)
                tutorial.SetTouchpadSprite(!SubMenu.subBtnScripts[SubMenu.HoverredButton].disabled, ControllerInputIndex.mid);
            
        }

        public virtual void ResetTouchpadSprite()
        {
            tutorial.SetCanvas(TextCanvas.onTouchPad, false);
            tutorial.currentInput = ControllerInputIndex.none;
            tutorial.RunSpriteAnimation();  //Clean up after sprite is none.
            tutorial.touchpadScript.ResetSprite();
        }

        public virtual void MatchRotator()
        {
            if (tutorial.currentInput == ControllerInputIndex.left || tutorial.currentInput == ControllerInputIndex.right)
            {
                if (!ViveSR_Experience_Demo.instance.Rotator.isRotateOn)
                    LeftRightPressedDown();
            }
            else if (tutorial.currentInput == ControllerInputIndex.up || tutorial.currentInput == ControllerInputIndex.down)
            {   
                if (SubMenu != null) SetSubBtnMessage();
            }
        }

        public virtual void MatchRotatorUp()
        {
            tutorial.isTouchpadPressed = false;
        }

        public virtual void ConfirmSelection()
        {
            tutorial.isTouchpadPressed = true;
            if (!tutorial.isTriggerPressed)
            {
                if (tutorial.currentInput == ControllerInputIndex.mid)
                    MidPressedDown();        
            }
        }
        
        protected void SetSubBtnMessage()
        {
            string subMsgType = "";
            if (SubMenu.subBtnScripts[SubMenu.HoverredButton].disabled) subMsgType = "Disabled";
            else if (SubMenu.subBtnScripts[SubMenu.HoverredButton].isOn) subMsgType = "On";
            else subMsgType = "Available";

            ViveSR_Experience_Tutorial_Line TextLineFound = null;

            TextLineFound = tutorial.SubLineManagers[Button.ButtonType].SubBtns[SubMenu.HoverredButton].lines.FirstOrDefault(x => x.messageType == subMsgType);
            if (TextLineFound != null) tutorial.SetCanvasText(TextCanvas.onRotator, TextLineFound.text);
        }

        protected void SetSubBtnMessage(string subMsgType)
        {
            ViveSR_Experience_Tutorial_Line TextLineFound = null;

            TextLineFound = tutorial.SubLineManagers[Button.ButtonType].SubBtns[SubMenu.SelectedButton].lines.FirstOrDefault(x => x.messageType == subMsgType);
            if (TextLineFound != null) tutorial.SetCanvasText(TextCanvas.onRotator, TextLineFound.text);
        }

        protected virtual void LeftRightPressedDown()
        {
            if (tutorial.isTriggerPressed) return;
            if (!ViveSR_Experience_Demo.instance.Rotator.isRotateDown) return;
            ViveSR_Experience_IButton CurrentButton = ViveSR_Experience_Demo.instance.Rotator.CurrentButton;

            if (SubMenu == null || !CurrentButton.SubMenu.isSubMenuOn)
            {
                tutorial.SetMainMessage();

                tutorial.SetTouchpadSprite(!CurrentButton.disabled, ControllerInputIndex.mid);
                tutorial.SetTouchpadSprite(true, false, ControllerInputIndex.left, ControllerInputIndex.right);
                tutorial.SetTouchpadSprite(false, false, ControllerInputIndex.up, ControllerInputIndex.down);

                tutorial.SetCanvas(TextCanvas.onRotator, true);
                tutorial.SetCanvas(TextCanvas.onTrigger, false);
                tutorial.SetCanvas(TextCanvas.onGrip, false);
            }
        }
        protected virtual void MidPressedDown()
        {                                                                                               
            bool isDisabled = ViveSR_Experience_Demo.instance.ButtonScripts[Button.ButtonType].disabled;
            if (isDisabled) return;

            if (SubMenu == null) tutorial.SetMainMessage();
            else
            {
                tutorial.SetTouchpadSprite(true, ControllerInputIndex.up, ControllerInputIndex.down);
                SetSubBtnMessage();
            }
            SetTouchpadMessage();
        } 

        public void SetTouchpadMessage()
        {
            ViveSR_Experience_Tutorial_Line TextLineFound_Trigger = null;
            ViveSR_Experience_Tutorial_Line TextLineFound_Grip = null;
            if (SubMenu == null)
            {
                TextLineFound_Trigger = tutorial.MainLineManagers[Button.ButtonType].controllerTexts.FirstOrDefault(x => x.messageType == ControllerInputIndex.trigger._ToString());
                TextLineFound_Grip = tutorial.MainLineManagers[Button.ButtonType].controllerTexts.FirstOrDefault(x => x.messageType == ControllerInputIndex.trigger._ToString());
            }
            else if (SubMenu.subBtnScripts[SubMenu.SelectedButton].isOn)
            {
                if (SubMenu.SelectedButton < tutorial.SubLineManagers[Button.ButtonType].controllerTexts.Count)
                {
                    TextLineFound_Trigger = tutorial.SubLineManagers[Button.ButtonType].controllerTexts[SubMenu.SelectedButton].lines.FirstOrDefault(x => x.messageType == ControllerInputIndex.trigger._ToString());
                    TextLineFound_Grip = tutorial.SubLineManagers[Button.ButtonType].controllerTexts[SubMenu.SelectedButton].lines.FirstOrDefault(x => x.messageType == ControllerInputIndex.grip._ToString());
                }
            }

            if (TextLineFound_Trigger != null) tutorial.SetCanvasText(TextCanvas.onTrigger, TextLineFound_Trigger.text, ViveSR_Experience_Demo.instance.AttentionColor);
            else tutorial.SetCanvasText(TextCanvas.onTrigger, GetDefaultControllerTextsMessage(ControllerInputIndex.trigger), ViveSR_Experience_Demo.instance.OriginalEmissionColor);

            if (TextLineFound_Grip != null) tutorial.SetCanvasText(TextCanvas.onGrip, TextLineFound_Grip.text, ViveSR_Experience_Demo.instance.AttentionColor);
            else tutorial.SetCanvasText(TextCanvas.onGrip, GetDefaultControllerTextsMessage(ControllerInputIndex.grip), ViveSR_Experience_Demo.instance.OriginalEmissionColor);
        }

        public virtual void TriggerDown()
        {
            tutorial.isTriggerPressed = true;
            tutorial.wasTriggerCanvasActive = tutorial.IsCanvasActive(TextCanvas.onTrigger);
            tutorial.SetCanvas(TextCanvas.onTrigger, false);
        }
        public virtual void TriggerUp()
        {
            tutorial.isTriggerPressed = false;
            tutorial.SetCanvas(TextCanvas.onTrigger, tutorial.wasTriggerCanvasActive);
            tutorial.wasTriggerCanvasActive = false;
        }

        public string GetDefaultControllerTextsMessage(ControllerInputIndex defaultStringIndex)
        {
            switch (defaultStringIndex)
            {
                case ControllerInputIndex.right: return "[Click] Rotate right";
                case ControllerInputIndex.left: return "[Click] Rotate left";
                case ControllerInputIndex.up: return "[Click] Move up";
                case ControllerInputIndex.down: return "[Click] Move down";
                case ControllerInputIndex.mid: return "[Click] Confirm";
                case ControllerInputIndex.trigger: return "Hold Trigger";
                default: return "";
            }
        }
    }
}