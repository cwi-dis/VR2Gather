using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{
    [RequireComponent(typeof(ViveSR_Experience))]
    public class Sample3_DynamicMesh : MonoBehaviour
    {       
        protected Text LeftText, RightText, ThrowableText, DisplayMesh;
        GameObject TriggerCanvas;
        [SerializeField] ViveSR_Experience_IDartGenerator dartGenerator;

        ViveSR_Experience_DynamicMesh DynamicMeshScript;

        /// <summary>
        /// Register callbacks for SRWorks events.
        /// </summary>
        #pragma warning disable
        private ViveSR_Experience_ErrorCallbackRegistration ErrorCallbackRegistration;

        public void Init()
        {
            DynamicMeshScript = GetComponent<ViveSR_Experience_DynamicMesh>();

            GameObject attachPointCanvas = ViveSR_Experience.instance.AttachPoint.transform.GetChild(ViveSR_Experience.instance.AttachPointIndex).transform.gameObject;

            DisplayMesh = attachPointCanvas.transform.Find("TouchpadCanvas/DisplayText").GetComponent<Text>();
            LeftText = attachPointCanvas.transform.Find("TouchpadCanvas/LeftText").GetComponent<Text>();
            RightText = attachPointCanvas.transform.Find("TouchpadCanvas/RightText").GetComponent<Text>();
            ThrowableText = attachPointCanvas.transform.Find("TriggerCanvas/TriggerText").GetComponent<Text>();
            TriggerCanvas = attachPointCanvas.transform.Find("TriggerCanvas").gameObject;
            RightText.enabled = true;

            DynamicMeshScript.SetDynamicMesh(true);
            ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger;
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad;

            // Register callbacks for SRWorks events.
            ErrorCallbackRegistration = new ViveSR_Experience_ErrorCallbackRegistration(ViveSR_Experience.instance.ErrorHandlerScript);
        }

        void HandleTrigger(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    LeftText.enabled = true;
                    RightText.text = "       >";
                    TriggerCanvas.SetActive(false);
                    break;
                case ButtonStage.PressUp:
                    LeftText.enabled = false;
                    SetRightText();
                    TriggerCanvas.SetActive(true);
                    break;
            }
        }

        void HandleTouchpad(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, false);
                    HandleTouchpad_PressDown(touchpadDirection);
                    break;
            }
        }

        void HandleTouchpad_PressDown(TouchpadDirection touchpadDirection)
        {        
            switch (touchpadDirection)
            {
                case TouchpadDirection.Up:
                    DynamicMeshScript.SetMeshDisplay(!DynamicMeshScript.ShowDynamicCollision);
                    DisplayMesh.text = DynamicMeshScript.ShowDynamicCollision ? "[Hide Mesh]" : "[Show Mesh]";
                    SetRightText();
                    break;
                case TouchpadDirection.Down:
                    dartGenerator.DestroyObjs();
                    break;
                case TouchpadDirection.Right:
                    DynamicMeshScript.SetWireframeDisplay(!DynamicMeshScript.ShowWireframe);
                    SetRightText();

                    break;
            }
        }

        void SetRightText()
        {
            if (DynamicMeshScript.ShowDynamicCollision)
            {
                RightText.text = DynamicMeshScript.ShowWireframe ? "[Hide Wireframe]" : "[Show Wireframe]";
            }
            else RightText.text = "";
        }
    }
}