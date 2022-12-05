using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{
    [RequireComponent(typeof(ViveSR_Experience))]
    public class Sample4_StaticMesh : MonoBehaviour
    {
        enum MeshDisplayMode
        {
            None = 0,
            Collider,
            VRMode,
            MaxNum
        }

        [SerializeField] ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr;

        protected Text ScanText, StopText, SaveText, LoadText, HintText, DartText, GripText;
        GameObject TriggerCanvas, GripCanvas;
        [SerializeField] protected Color BrightColor, OriginalColor;

        bool isTriggerDown;
        MeshDisplayMode meshDisplayMode;
        ViveSR_Experience_StaticMeshToolManager StaticMeshTools;

        DartPlacementMode dartPlaceMentmode;

        /// <summary>
        /// Register callbacks for SRWorks events.
        /// </summary>
        #pragma warning disable
        private ViveSR_Experience_ErrorCallbackRegistration ErrorCallbackRegistration;

        public void Init()
        {
            StaticMeshTools = FindObjectOfType<ViveSR_Experience_StaticMeshToolManager>();
            GameObject attachPointCanvas = ViveSR_Experience.instance.AttachPoint.transform.GetChild(ViveSR_Experience.instance.AttachPointIndex).transform.gameObject;

            ScanText = attachPointCanvas.transform.Find("TouchpadCanvas/ScanText").GetComponent<Text>();
            StopText = attachPointCanvas.transform.Find("TouchpadCanvas/StopText").GetComponent<Text>();
            SaveText = attachPointCanvas.transform.Find("TouchpadCanvas/SaveText").GetComponent<Text>();
            LoadText = attachPointCanvas.transform.Find("TouchpadCanvas/LoadText").GetComponent<Text>();
            HintText = attachPointCanvas.transform.Find("HintText").GetComponent<Text>();
            DartText = attachPointCanvas.transform.Find("TriggerCanvas/TriggerText").GetComponent<Text>();
            GripText = attachPointCanvas.transform.Find("GripCanvas/GripText").GetComponent<Text>();
            TriggerCanvas = attachPointCanvas.transform.Find("TriggerCanvas").gameObject;
            GripCanvas = attachPointCanvas.transform.Find("GripCanvas").gameObject;

            LoadText.color = StaticMeshTools.StaticMeshScript.CheckModelFileExist() ? BrightColor : OriginalColor;

            ViveSR_RigidReconstructionRenderer.LiveMeshDisplayMode = ReconstructionDisplayMode.ADAPTIVE_MESH;
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_MeshOperation;
            ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_SetDartControl;

            dartPlaceMentmode = dartGeneratorMgr.dartPlacementMode;

            // Register callbacks for SRWorks events.
            ErrorCallbackRegistration = new ViveSR_Experience_ErrorCallbackRegistration(ViveSR_Experience.instance.ErrorHandlerScript);
        }

        #region Mesh Control
        private void HandleTouchpad_MeshOperation(ButtonStage buttonStage, Vector2 axis)
        {
            if (isTriggerDown) return;

            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, false);
                    HandleTouchpad_MeshOperation_PressDown(touchpadDirection);
                    break;
            } 
        }
        void HandleTouchpad_MeshOperation_PressDown(TouchpadDirection touchpadDirection)
        {
            switch (touchpadDirection)
            {
                case TouchpadDirection.Up: Scan(); break;
                case TouchpadDirection.Left: StopAndReset(); break;
                case TouchpadDirection.Right: Save(); break;
                case TouchpadDirection.Down: Load(); break;
            }
        }

        void Scan()
        {
            bool allowScan = !ViveSR_RigidReconstruction.IsScanning 
                && !ViveSR_RigidReconstruction.IsExporting
                && !StaticMeshTools.StaticMeshScript.ModelIsLoading;

            if (!allowScan) return;

            SwitchMeshDisplayMode(0);
            dartGeneratorMgr.DestroyObjs();

            if (StaticMeshTools.StaticMeshScript.CheckModelLoaded())
            {
                StaticMeshTools.StaticMeshScript.LoadMesh(false);
                LoadText.color = BrightColor;
            }

            GripCanvas.SetActive(false);
            TriggerCanvas.SetActive(false);
            HintText.text = "";

            ViveSR_RigidReconstruction.RegisterDataErrorHandler((int)Error.GPU_MEMORY_FULL, GPUMemoryFull);
            StaticMeshTools.StaticMeshScript.EnableDepthProcessingAndScanning(true);

            LoadText.color = OriginalColor;
            ScanText.color = OriginalColor;
            SaveText.color = BrightColor;
            StopText.color = BrightColor;
        }

        void StopAndReset()
        {
            if (!ViveSR_RigidReconstruction.IsScanning) return;

            Stop();
            StaticMeshTools.StaticMeshScript.ResetScannedData();
        }
        void Stop()
        {
            if (!ViveSR_RigidReconstruction.IsScanning) return;

            StaticMeshTools.StaticMeshScript.EnableDepthProcessingAndScanning(false);

            TriggerCanvas.SetActive(true);

            if (StaticMeshTools.StaticMeshScript.CheckModelFileExist() && !StaticMeshTools.StaticMeshScript.CheckModelLoaded()) LoadText.color = BrightColor;
            if (StaticMeshTools.StaticMeshScript.CheckModelLoaded()) StaticMeshTools.StaticMeshScript.LoadMesh(true);

            ScanText.color = BrightColor;
            StopText.color = OriginalColor;
            SaveText.color = OriginalColor;
            GripCanvas.SetActive(StaticMeshTools.StaticMeshScript.CheckModelLoaded());

            ViveSR_RigidReconstruction.UnregisterDataErrorHandler((int)Error.GPU_MEMORY_FULL);
        }

        void Save()
        {
            if (!ViveSR_RigidReconstruction.IsScanning) return;

            ViveSR_Experience_ControllerDelegate.gripDelegate -= HandleGrip_SwitchMeshDisplay;

            LoadText.color = OriginalColor;
            ScanText.color = OriginalColor;
            StopText.color = OriginalColor;
            SaveText.color = OriginalColor;

            StaticMeshTools.StaticMeshScript.ExportModel(Save_UpdatePercentage, Save_Done);
        }
        void Save_UpdatePercentage(int percentage)
        {
            HintText.text = "Saving..." + percentage + "%";
        }
        void Save_Done()
        {
            TriggerCanvas.SetActive(true);
            GripCanvas.SetActive(StaticMeshTools.StaticMeshScript.CheckModelLoaded());

            HintText.text = "Mesh Saved!";
            ScanText.color = BrightColor;
            LoadText.color = BrightColor;
        }

        void Load()
        {
            bool allowLoad = !ViveSR_RigidReconstruction.IsScanning
                && !ViveSR_RigidReconstruction.IsExporting
                && !StaticMeshTools.StaticMeshScript.ModelIsLoading
                && StaticMeshTools.StaticMeshScript.CheckModelFileExist()
                && !StaticMeshTools.StaticMeshScript.CheckModelLoaded();

            if (allowLoad) StaticMeshTools.StaticMeshScript.LoadMesh(true, false, Load_Before, Load_After);
        }
        void Load_Before()
        {
            HintText.text = "Loading...";
            LoadText.color = OriginalColor;
        }
        void Load_After()
        {
            HintText.text = "Mesh Loaded!";
            ScanText.color = BrightColor;
            LoadText.color = OriginalColor;

            GripCanvas.SetActive(true);
            ViveSR_Experience_ControllerDelegate.gripDelegate += HandleGrip_SwitchMeshDisplay;
        }
        #endregion

        #region Mesh Display
        public void HandleGrip_SwitchMeshDisplay(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    bool allowSwitch = StaticMeshTools.StaticMeshScript.CheckModelFileExist()
                        && !ViveSR_RigidReconstruction.IsExporting
                        && !ViveSR_RigidReconstruction.IsScanning;

                    if (!allowSwitch) break;
                    
                    SwitchMeshDisplayMode((MeshDisplayMode)(((int)meshDisplayMode + 1) % (int)MeshDisplayMode.MaxNum));

                    break;
            }
        }
        void SwitchMeshDisplayMode(MeshDisplayMode meshDisplayMode)
        {
            this.meshDisplayMode = meshDisplayMode;

            switch (meshDisplayMode)
            {
                case MeshDisplayMode.None:  //Hidden
                    StaticMeshTools.SwitchModeScript.SwitchMode(DualCameraDisplayMode.MIX);
                    StaticMeshTools.StaticMeshScript.RenderModelMesh(false);
                    HintText.text = "See-Through";
                    break;
                case MeshDisplayMode.Collider:
                    StaticMeshTools.StaticMeshScript.SwitchShowCollider(ShowMode.All);
                    HintText.text = "View Colliders";
                    break;
                case MeshDisplayMode.VRMode:
                    StaticMeshTools.StaticMeshScript.SwitchShowCollider(ShowMode.None);
                    StaticMeshTools.SwitchModeScript.SwitchMode(DualCameraDisplayMode.VIRTUAL);
                    StaticMeshTools.StaticMeshScript.RenderModelMesh(true);
                    HintText.text = "View Texture";
                    break;
            }
        }
        #endregion

        #region Dart
        void HandleTrigger_SetDartControl(ButtonStage buttonStage, Vector2 axis)
        {
            bool allowDartControl = !ViveSR_RigidReconstruction.IsExporting
                && !ViveSR_RigidReconstruction.IsScanning;

            if (!allowDartControl) return;

            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    isTriggerDown = true;
                    EnableDartControl();
                    break;
                case ButtonStage.PressUp:
                    isTriggerDown = false;
                    DisableDartControl();
                    break;   
            }   
        }
        void EnableDartControl()
        {
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_DartControl;
            ViveSR_Experience_ControllerDelegate.gripDelegate -= HandleGrip_SwitchMeshDisplay;

            SetUI_ToDartControl();
        }
        void DisableDartControl()
        {
            ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_DartControl;
            ViveSR_Experience_ControllerDelegate.gripDelegate += HandleGrip_SwitchMeshDisplay;

            SetUI_ToMeshControl();
        }

        void HandleTouchpad_DartControl(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:                                                                                  
                    TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, false);
                    HandleTouchpad_DartControl_PressDown(touchpadDirection);
                    break;
            }
        }
        void HandleTouchpad_DartControl_PressDown(TouchpadDirection touchpadDirection)
        {
            switch (touchpadDirection)
            {
                case TouchpadDirection.Up:
                    dartPlaceMentmode = dartPlaceMentmode == DartPlacementMode.Raycast ? DartPlacementMode.Throwable : DartPlacementMode.Raycast;
                    SetUI_DartGeneratorType();
                    break;
                case TouchpadDirection.Down:
                    dartGeneratorMgr.DestroyObjs();
                    break;
            }
        }
        #endregion

        #region UI
        void SetUI_ToDartControl()
        {
            StopText.color = ScanText.color = SaveText.color = LoadText.color = BrightColor;

            SetUI_DartGeneratorType();

            StopText.text = "<";
            SaveText.text = ">";
            LoadText.text = "[Clear]";

            DartText.text = "Throw Item";
            TriggerCanvas.SetActive(false);
            GripCanvas.SetActive(StaticMeshTools.StaticMeshScript.CheckModelLoaded());
        }
        void SetUI_DartGeneratorType()
        {
            switch (dartPlaceMentmode)
            {
                case DartPlacementMode.Throwable:
                    HintText.text = HintText.text = "Throw";
                    ScanText.text = ScanText.text = "[Raycast]";
                    break;
                case DartPlacementMode.Raycast:
                    HintText.text = HintText.text = "Raycast";
                    ScanText.text = ScanText.text = "[Throw]";
                    break;
            }
        }
        void SetUI_ToMeshControl()
        {
            StopText.color = OriginalColor;
            SaveText.color = OriginalColor;
            LoadText.color = StaticMeshTools.StaticMeshScript.CheckModelLoaded() ? OriginalColor : BrightColor;
            ScanText.color = BrightColor;

            HintText.text = "Static Mesh";
            StopText.text = "[Stop]";
            SaveText.text = "[Save]";
            LoadText.text = "[Load]";
            ScanText.text = "[Scan]";

            TriggerCanvas.SetActive(true);
            GripCanvas.SetActive(StaticMeshTools.StaticMeshScript.CheckModelLoaded());
        }
        #endregion


        #region GPUMemoryFullError
        void GPUMemoryFull()
        {
            ViveSR_Experience.instance.ErrorHandlerScript.EnablePanel("GPU memory is full. Save the existing mesh?", "[Abort]", GPUMemoryFull_Abort, "[Save]", Save);

            Stop();
            ViveSR_RigidReconstruction.UnregisterDataErrorHandler((int)Error.GPU_MEMORY_FULL);
        }
        void GPUMemoryFull_Abort()
        {
            ViveSR_Experience.instance.ErrorHandlerScript.DisableAllErrorPanels();
            StaticMeshTools.StaticMeshScript.ResetScannedData();
        }
        void GPUMemoryFull_Save()
        {
            ViveSR_Experience.instance.ErrorHandlerScript.DisableAllErrorPanels();
            Save();
        }
        #endregion
    }
}