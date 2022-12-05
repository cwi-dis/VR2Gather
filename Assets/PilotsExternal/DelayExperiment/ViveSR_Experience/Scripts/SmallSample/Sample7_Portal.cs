using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    [RequireComponent(typeof(ViveSR_Experience))]
    public class Sample7_Portal : MonoBehaviour
    {    
        GameObject TriggerHint, LeftHint, RightHint;

        ViveSR_Experience_IDartGenerator dartGenerator;
        public ViveSR_Experience_Portal PortalScript;
        ViveSR_Experience_Effects EffectsScript;

        /// <summary>
        /// Register callbacks for SRWorks events.
        /// </summary>
        #pragma warning disable
        private ViveSR_Experience_ErrorCallbackRegistration ErrorCallbackRegistration;

        public void Init()
        {
            EffectsScript = GetComponent<ViveSR_Experience_Effects>();
            PortalScript = GetComponent<ViveSR_Experience_Portal>();
            dartGenerator = PortalScript.dartGeneratorMgr_portal.GetComponent<ViveSR_Experience_IDartGenerator>();

            GameObject attachPointCanvas = ViveSR_Experience.instance.AttachPoint.transform.GetChild(ViveSR_Experience.instance.AttachPointIndex).transform.gameObject;

            LeftHint = attachPointCanvas.transform.Find("TouchpadCanvas/LeftText").gameObject;
            RightHint = attachPointCanvas.transform.Find("TouchpadCanvas/RightText").gameObject;
            TriggerHint = attachPointCanvas.transform.Find("TriggerCanvas").gameObject;

            PortalScript.Init();
            PortalScript.InitPortal();
            PortalScript.SetPortal(true);

            ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_ThrowableItemUI;
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_ControlPortal;
            ViveSR_Experience_ControllerDelegate.gripDelegate += HandleGrip_SwitchEffects;


            // Register callbacks for SRWorks events.
            ErrorCallbackRegistration = new ViveSR_Experience_ErrorCallbackRegistration(ViveSR_Experience.instance.ErrorHandlerScript);
        }

        void HandleTrigger_ThrowableItemUI(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    TriggerHint.SetActive(false);
                    RightHint.SetActive(true);
                    LeftHint.SetActive(true);
                    break;

                case ButtonStage.PressUp:
                    TriggerHint.SetActive(true);
                    RightHint.SetActive(false);
                    LeftHint.SetActive(false);
                    break;
            }
        }

        void HandleGrip_SwitchEffects(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    EffectsScript.CurrentEffectNumber += 1;
                    if (EffectsScript.CurrentEffectNumber == (int)ImageEffectType.TOTAL_NUM) EffectsScript.CurrentEffectNumber = -1;
                   
                    EffectsScript.ChangeShader(EffectsScript.CurrentEffectNumber);
                break;
            }
        }

        void HandleTouchpad_ControlPortal(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {    
                case ButtonStage.PressDown: 
                    TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, false);
                    HandleTouchpad_ControlPortal_PressDown(touchpadDirection);   
                    break;
            } 
        }
        void HandleTouchpad_ControlPortal_PressDown(TouchpadDirection touchpadDirection)
        {
            switch (touchpadDirection)
            {
                case TouchpadDirection.Up:
                    PortalScript.ResetPortalPosition();
                    break;
                case TouchpadDirection.Down:
                    dartGenerator.DestroyObjs();
                    break;
            }
        }
    }
}
