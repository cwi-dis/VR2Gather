using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Button_Portal : ViveSR_Experience_IButton
    {
        ViveSR_Experience_Portal PortalScript;
       // [SerializeField] GameObject bg, realWorldFloor;
        ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr;

        protected override void AwakeToDo()
        {
            ButtonType = MenuButton.Portal;
        }

        protected override void StartToDo()
        {
            dartGeneratorMgr = ViveSR_Experience_Demo.instance.DartGeneratorMgrs[DartGeneratorIndex.ForPortal];
            PortalScript = ViveSR_Experience_Demo.instance.PortalScript;
            PortalScript.enabled = true;
            PortalScript.InitPortal();
        }


        public override void ActionToDo()
        {
            ViveSR_Experience_Demo.instance.bg.SetActive(isOn);
            ViveSR_Experience_Demo.instance.realWorldFloor.SetActive(isOn);

            PortalScript.SetPortal(isOn);

            if (isOn)
            {
                ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger;
                ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad;
            }
            else
            {
                ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger;
                ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad;
                dartGeneratorMgr.DestroyObjs();
            }
        }


        void HandleTrigger(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                    break;
                case ButtonStage.PressUp:
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
                    break;
            }
        }
        void HandleTouchpad(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:

                    TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, true);

                    switch (touchpadDirection)
                    {
                        case TouchpadDirection.Up:
                            PortalScript.ResetPortalPosition();
                            break;
                        case TouchpadDirection.Down:
                            dartGeneratorMgr.DestroyObjs();
                            break;            
                    }
                    break;
            }
        }
    }
}