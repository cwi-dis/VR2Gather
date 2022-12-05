using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{
    [RequireComponent(typeof(ViveSR_Experience))]
    public class Sample1_Effects_SwitchMode : MonoBehaviour
    {
        [SerializeField] ViveSR_Experience_Effects EffectsScript;
        ViveSR_Experience_SwitchMode SwitchModeScript;

        GameObject attachPointCanvas, triggerCanvas;
        protected Text EffectText;

        bool isTriggerDown;

        /// <summary>
        /// Register callbacks for SRWorks events.
        /// </summary>
        #pragma warning disable
        private ViveSR_Experience_ErrorCallbackRegistration ErrorCallbackRegistration;

        public void Init()
        {
            SwitchModeScript = GetComponent<ViveSR_Experience_SwitchMode>();

            attachPointCanvas = ViveSR_Experience.instance.AttachPoint.transform.GetChild(ViveSR_Experience.instance.AttachPointIndex).transform.gameObject;

            EffectText = attachPointCanvas.transform.Find("TriggerCanvas/TriggerText").GetComponent<Text>();
            triggerCanvas = attachPointCanvas.transform.Find("TriggerCanvas").gameObject;

            ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger;
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad;

            // Register callbacks for SRWorks events.
            ErrorCallbackRegistration = new ViveSR_Experience_ErrorCallbackRegistration(ViveSR_Experience.instance.ErrorHandlerScript);
        }
        void HandleTrigger(ButtonStage buttonStage, Vector2 axis)
        {
            if (SwitchModeScript.currentMode == DualCameraDisplayMode.MIX)
            {
                switch (buttonStage)
                {
                    case ButtonStage.PressDown:
                        EffectsScript.GenerateEffectBall();
                        attachPointCanvas.SetActive(false);
                        isTriggerDown = true;
                        break;
                    case ButtonStage.PressUp:
                        EffectsScript.HideEffectBall();
                        attachPointCanvas.SetActive(true);
                        isTriggerDown = false;
                        break;
                }
            }
        }

        void HandleTouchpad(ButtonStage buttonStage, Vector2 axis)
        {
            if (!isTriggerDown)
            {
                switch (buttonStage)
                {
                    case ButtonStage.PressDown:
                        SwitchModeScript.SwitchMode(SwitchModeScript.currentMode == DualCameraDisplayMode.MIX ? DualCameraDisplayMode.VIRTUAL : DualCameraDisplayMode.MIX);
                        EffectsScript.ChangeShader(-1);

                        triggerCanvas.SetActive(SwitchModeScript.currentMode == DualCameraDisplayMode.MIX);
                        break;
                }
            }
        }    
    }
}