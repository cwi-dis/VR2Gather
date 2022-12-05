using UnityEngine;
using System.Collections;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Button_Settings : ViveSR_Experience_IButton
    {
        ViveSR_Experience_SettingsPanel SettingsPanelScript;
        bool isTriggerDown, isTouchpadDown;

        protected override void AwakeToDo()
        {     
            ButtonType = MenuButton.Settings;

            SettingsPanelScript = ViveSR_Experience_Demo.instance.SettingsPanelScript;
            SettingsPanelScript.ResetPanelPos();
        }                                                      
        void HandleTrigger_AdjustCameraControlSliders(ButtonStage buttonStage, Vector2 axis)
        {
            if (!isOn) return;
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    isTriggerDown = true;
                    ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "[Settings]\nAdjust Values", false);
                    ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_ResetSettingsPanel;
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);

                    break;  

                case ButtonStage.PressUp:
                    isTriggerDown = false;

                    ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_ResetSettingsPanel;
                    if (!isTouchpadDown)
                    {
                        ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "", false);
                        ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
                    }
                    break;
            }
        } 

        void HandleTouchpad_ResetSettingsPanel(ButtonStage buttonStage, Vector2 axis)
        {
            if (!isOn) return;
            TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, true);

            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    HandleTouchpad_ResetSettingsPanel_PressDown(touchpadDirection);
                    break;
                case ButtonStage.PressUp:
                    HandleTouchpad_ResetSettingsPanel_PressUp(touchpadDirection);
                    break;
            }
        }
        void HandleTouchpad_ResetSettingsPanel_PressDown(TouchpadDirection touchpadDirection)
        {
            switch (touchpadDirection)
            {
                case TouchpadDirection.Up:
                    isTouchpadDown = true;
                    ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "[Settings]\nMove the Panel", false);
                    ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger_AdjustCameraControlSliders;
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                    StartCoroutine(ResetPanelPos());
                    break;
            }
        }
        void HandleTouchpad_ResetSettingsPanel_PressUp(TouchpadDirection touchpadDirection)
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
                SettingsPanelScript.ResetPanelPos();
                yield return new WaitForEndOfFrame();
            }
        }

        public override void ActionToDo()
        {
            if (isOn)
            {
                ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_ResetSettingsPanel;
                ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_AdjustCameraControlSliders;
            }
            else
            {
                ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_ResetSettingsPanel;
                ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger_AdjustCameraControlSliders;
            }

            SettingsPanelScript.gameObject.SetActive(isOn);
            PlayerHandUILaserPointer.EnableLaserPointer(isOn);
        }
    }
}
