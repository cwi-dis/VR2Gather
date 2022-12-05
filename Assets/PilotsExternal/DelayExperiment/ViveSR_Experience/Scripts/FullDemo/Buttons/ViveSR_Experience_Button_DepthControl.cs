using UnityEngine;
using System.Collections;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Button_DepthControl : ViveSR_Experience_IButton
    {
        [SerializeField]  ViveSR_Experience_DepthControl DepthControlScript;
        bool isTriggerDown, isTouchpadDown;

        public override void ForceExcuteButton(bool on)
        {
            if (isOn != on) Action(on);
        }

        protected override void AwakeToDo()
        {
            DepthControlScript = ViveSR_Experience_Demo.instance.DepthControlScript;

            DepthControlScript.ResetPanelPos();
        }

        void HandleTrigger_AdjustSliders(ButtonStage buttonStage, Vector2 axis)
        {
            if (isOn)
            {
                switch (buttonStage)
                {
                    case ButtonStage.PressDown:
                        isTriggerDown = true;

                        ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "[Depth Panel]\nAdjust Values", false);
                        ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_ResetDepthPanel;
                        ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                        break;

                    case ButtonStage.PressUp:
                        isTriggerDown = false;

                        ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_ResetDepthPanel;
                        if (!isTouchpadDown)
                        {
                            ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
                            ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "", false);
                        }
                        break;
                }
            }
        } 

        void HandleTouchpad_ResetDepthPanel(ButtonStage buttonStage, Vector2 axis)
        {
            if (isOn)
            {
                TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, true);

                switch (buttonStage)
                {
                    case ButtonStage.PressDown:

                        switch (touchpadDirection)
                        {
                            case TouchpadDirection.Up:

                                isTouchpadDown = true;
                                ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "[Depth Panel]\nMove the Panel", false);
                                ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger_AdjustSliders;
                                ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                                StartCoroutine(ResetPanelPos());

                                break;
                        }

                        break;
                    case ButtonStage.PressUp:
                        isTouchpadDown = false;
                                                 
                        ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_AdjustSliders;
                        if (!isTriggerDown)
                        {
                            ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "", false);
                            ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
                        }

                        break;
                }
            }
        }

        IEnumerator ResetPanelPos()
        {
            while (isTouchpadDown)
            {
                DepthControlScript.ResetPanelPos();
                yield return new WaitForEndOfFrame();
            }
        }

        public override void ActionToDo()
        {
            if (isOn)
            {
                ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_ResetDepthPanel;
                ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_AdjustSliders;
            }
            else
            {
                ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_ResetDepthPanel;
                ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger_AdjustSliders;
            }

            DepthControlScript.gameObject.SetActive(isOn);
            PlayerHandUILaserPointer.EnableLaserPointer(isOn);
        }
        private void OnApplicationQuit()
        {
            ViveSR_DualCameraImageCapture.EnableDepthProcess(false);
            ViveSR_DualCameraImageRenderer.UpdateDepthMaterial = false;
 
            DepthControlScript.LoadDefaultValue();
        }
    }
}
