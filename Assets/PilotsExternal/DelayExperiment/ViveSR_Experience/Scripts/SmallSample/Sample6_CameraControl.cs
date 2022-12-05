using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class Sample6_CameraControl : MonoBehaviour
    {
        [SerializeField] ViveSR_Experience_CameraControl CameraControlScript;

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

            CameraControlScript.gameObject.SetActive(true);   

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
                    CameraControlScript.ResetPanelPos();
                    break;
            }
        }

        private void OnApplicationQuit()
        {
            CameraControlScript.Reset();
        }
    }
}