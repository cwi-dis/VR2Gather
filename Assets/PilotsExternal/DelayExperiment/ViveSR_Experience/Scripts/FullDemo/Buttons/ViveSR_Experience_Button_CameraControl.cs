using UnityEngine;
using System.Collections;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Button_CameraControl : ViveSR_Experience_IButton
    {
        ViveSR_Experience_CameraControl CameraControlScript;
        bool isTriggerDown, isTouchpadDown;

        protected override void AwakeToDo()
        {     
            ButtonType = MenuButton.CameraControl;

            if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_COSMOS)
            {
                EnableButton(false);
                return;
            }


            CameraControlScript = ViveSR_Experience_Demo.instance.CameraControlScript;
            CameraControlScript.ResetPanelPos();
        }

        public override void ForceExcuteButton(bool on)
        {
            if (isOn != on) Action(on);
        }

        void HandleTrigger_AdjustCameraControlSliders(ButtonStage buttonStage, Vector2 axis)
        {
            if (!isOn) return;
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    isTriggerDown = true;
                    ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "[Camera Control]\nAdjust Values", false);
                    ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_ResetCameraControlPanel;
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);

                    break;  

                case ButtonStage.PressUp:
                    isTriggerDown = false;

                    ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_ResetCameraControlPanel;
                    if (!isTouchpadDown)
                    {
                        ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "", false);
                        ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
                    }
                    break;
            }
        } 

        void HandleTouchpad_ResetCameraControlPanel(ButtonStage buttonStage, Vector2 axis)
        {
            if (!isOn) return;
            TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, true);

            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    HandleTouchpad_ResetCameraControlPanel_PressDown(touchpadDirection);
                    break;
                case ButtonStage.PressUp:
                    HandleTouchpad_ResetCameraControlPanel_PressUp(touchpadDirection);
                    break;
            }
        }
        void HandleTouchpad_ResetCameraControlPanel_PressDown(TouchpadDirection touchpadDirection)
        {
            switch (touchpadDirection)
            {
                case TouchpadDirection.Up:
                    isTouchpadDown = true;
                    ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "[Camera Control]\nMove the Panel", false);
                    ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger_AdjustCameraControlSliders;
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                    StartCoroutine(ResetPanelPos());
                    break;
            }
        }
        void HandleTouchpad_ResetCameraControlPanel_PressUp(TouchpadDirection touchpadDirection)
        {
            isTouchpadDown = false;
            ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "", false);
            ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_AdjustCameraControlSliders;
            if (!isTriggerDown) ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
        }

        IEnumerator ResetPanelPos()
        {
            while (isTouchpadDown)
            {
                CameraControlScript.ResetPanelPos();
                yield return new WaitForEndOfFrame();
            }
        }

        public override void ActionToDo()
        {
            if (isOn)
            {
                ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_ResetCameraControlPanel;
                ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_AdjustCameraControlSliders;
            }
            else
            {
                ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_ResetCameraControlPanel;
                ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger_AdjustCameraControlSliders;
            }

            CameraControlScript.gameObject.SetActive(isOn);
            PlayerHandUILaserPointer.EnableLaserPointer(isOn);
        }

        private void OnApplicationQuit()
        {
            if(CameraControlScript)
                CameraControlScript.Reset();
        }
    }
}
