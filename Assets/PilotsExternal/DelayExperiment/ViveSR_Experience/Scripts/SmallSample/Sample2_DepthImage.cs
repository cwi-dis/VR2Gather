using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{
    public class Sample2_DepthImage : MonoBehaviour
    {
        [SerializeField] ViveSR_Experience_DepthControl DepthControlScript;

        /// <summary>
        /// Register callbacks for SRWorks events.
        /// </summary>
        #pragma warning disable
        private ViveSR_Experience_ErrorCallbackRegistration ErrorCallbackRegistration;
                                                    
        public void Init()
        {
            PlayerHandUILaserPointer.CreateLaserPointer();
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad;
            ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger;

            DepthControlScript.gameObject.SetActive(true);

            // Register callbacks for SRWorks events.
            ErrorCallbackRegistration = new ViveSR_Experience_ErrorCallbackRegistration(ViveSR_Experience.instance.ErrorHandlerScript);
        }

        void HandleTrigger(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:                
                    ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad;
                    break;
                case ButtonStage.PressUp:
                    ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad;
                    break;
            }
        }

        void HandleTouchpad(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.Press:
                    DepthControlScript.ResetPanelPos();
                    break;
            }
        }

        private void OnApplicationQuit()
        {
            DepthControlScript.LoadDefaultValue();
        }
    }
}