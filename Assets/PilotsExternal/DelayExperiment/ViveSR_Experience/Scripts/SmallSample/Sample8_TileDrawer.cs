using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{
    [RequireComponent(typeof(ViveSR_Experience))]
    public class Sample8_TileDrawer : MonoBehaviour
    {
        enum ActionMode
        {
            MeshControl,
            TileControl,
            ColliderDisplay,
            MaxNum
        }

        ActionMode actionMode = ActionMode.MeshControl;

        [SerializeField] ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr;

        ViveSR_Experience_TileSpawner TileSpawnerScript;
        ViveSR_Experience_StaticMeshToolManager StaticMeshTools;

        Text ScanText, StopText, SaveText, LoadText, HintText, DartText, GripText;

        GameObject TriggerCanvas, GripCanvas;

        bool isTriggerDown;
        bool occlusionEnabled = false;
        
        int ActionModeNum = (int)ActionMode.MaxNum;

        [SerializeField] protected Color BrightColor, OriginalColor;

        DartPlacementMode dartPlaceMentmode;

        /// <summary>
        /// Register callbacks for SRWorks events.
        /// </summary>
        #pragma warning disable
        private ViveSR_Experience_ErrorCallbackRegistration ErrorCallbackRegistration;

        public void Init()
        {
            TileSpawnerScript = FindObjectOfType<ViveSR_Experience_TileSpawner>();
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

            TriggerCanvas.SetActive(false);
            GripCanvas.SetActive(false);

            LoadText.color = StaticMeshTools.StaticMeshScript.CheckModelFileExist() ? BrightColor : OriginalColor;
            TileSpawnerScript.RaycastStartPoint = ViveSR_Experience.instance.AttachPoint.transform.Find("RaycastStartPoint").gameObject;

            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_MeshOperation;

            dartPlaceMentmode = dartGeneratorMgr.dartPlacementMode;

            // Register callbacks for SRWorks events.
            ErrorCallbackRegistration = new ViveSR_Experience_ErrorCallbackRegistration(ViveSR_Experience.instance.ErrorHandlerScript);
        }

        #region Action Mode Control
        public void HandleGrip_SwitchMode(ButtonStage buttonStage, Vector2 axis)
        {
            if (isTriggerDown) return;

            switch (buttonStage)
            {
                case ButtonStage.PressDown:

                bool allowSwitch = StaticMeshTools.StaticMeshScript.texturedMesh != null
                        && !ViveSR_RigidReconstruction.IsExporting
                        && !ViveSR_RigidReconstruction.IsScanning;

                if(allowSwitch) MoveToNextActionMode(); 

                break;        
            }
        }

        void MoveToNextActionMode()
        {
            int mode_int = (int)actionMode;
            ActionMode mode = (ActionMode)((++mode_int) % ActionModeNum);
            SwitchActionMode(mode);
        }
        void SwitchActionMode(ActionMode mode)
        {
            actionMode = mode;

            switch (mode)
            {
                case ActionMode.MeshControl: SwitchActionMode_ToMeshControl(); break;
                case ActionMode.TileControl: SwitchActionMode_ToTileControl(); break;
                case ActionMode.ColliderDisplay: SwitchActionMode_ToColliderDisplay(); break;
            }
        }  
        void SwitchActionMode_ToMeshControl()
        {
            dartGeneratorMgr.gameObject.SetActive(true);
            ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_ColliderOperation;
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_MeshOperation;
            StaticMeshTools.SwitchModeScript.SwitchMode(DualCameraDisplayMode.MIX);
            StaticMeshTools.StaticMeshScript.SwitchShowCollider(ShowMode.None);
            SetUI_ToMeshControl();
        }
        void SwitchActionMode_ToTileControl()
        {
            TileSpawnerScript.enabled = true;
            TileSpawnerScript.SetCldPool(StaticMeshTools.StaticMeshScript.cldPool);

            ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger_SetDartControl;
            DartText.text = "Throw Item";
            dartGeneratorMgr.gameObject.SetActive(false);
            ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_MeshOperation;
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_TileOperation;
            ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_DrawTiles;
            StaticMeshTools.StaticMeshScript.SwitchShowCollider(ShowMode.None);
            SetUI_ToTileSpawn();
        }
        void SwitchActionMode_ToColliderDisplay()
        {
            TileSpawnerScript.enabled = false;
            ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_SetDartControl;
            DartText.text = "Throw Item";
            dartGeneratorMgr.gameObject.SetActive(true);
            ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger_DrawTiles;
            ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_TileOperation;
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_ColliderOperation;
            StaticMeshTools.StaticMeshScript.SwitchShowCollider(ShowMode.None);
            SetUI_ToColliderDisplay();

            EnableDepthOcclusion(false);
        }
        #endregion

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

            dartGeneratorMgr.DestroyObjs();
            TileSpawnerScript.ClearTiles();
            if (StaticMeshTools.StaticMeshScript.texturedMesh != null)
            {
                StaticMeshTools.StaticMeshScript.LoadMesh(false);
                LoadText.color = BrightColor;
            }

            GripCanvas.SetActive(false);
            TriggerCanvas.SetActive(false);
            HintText.text = "";
            ViveSR_Experience_ControllerDelegate.gripDelegate -= HandleGrip_SwitchMode;

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

            if (StaticMeshTools.StaticMeshScript.CheckModelLoaded()) ViveSR_Experience_ControllerDelegate.gripDelegate += HandleGrip_SwitchMode;
            StaticMeshTools.StaticMeshScript.EnableDepthProcessingAndScanning(false);

            TriggerCanvas.SetActive(StaticMeshTools.StaticMeshScript.CheckModelLoaded());
            GripCanvas.SetActive(StaticMeshTools.StaticMeshScript.CheckModelLoaded());

            if (StaticMeshTools.StaticMeshScript.CheckModelLoaded())
            {
                // show loaded mesh
                StaticMeshTools.StaticMeshScript.LoadMesh(true);
            }
            else if (StaticMeshTools.StaticMeshScript.CheckModelFileExist()) LoadText.color = BrightColor;

            ScanText.color = BrightColor;
            StopText.color = OriginalColor;
            SaveText.color = OriginalColor;
            LoadText.color = StaticMeshTools.StaticMeshScript.CheckModelFileExist() ? BrightColor : OriginalColor;
        }

        void Save()
        {
            bool allowSave = ViveSR_RigidReconstruction.IsScanning && !ViveSR_RigidReconstruction.IsExporting;

            if (!allowSave) return;                

            LoadText.color = OriginalColor;
            ScanText.color = OriginalColor;
            StopText.color = OriginalColor;
            SaveText.color = OriginalColor;
            ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_MeshOperation;

            StaticMeshTools.StaticMeshScript.ExportModel(Save_UpdatePercentage, Save_done);

        }
        void Save_UpdatePercentage(int percentage)
        {
            HintText.text = "Saving..." + percentage + "%";
        }
        void Save_done()
        {
            HintText.text = "Mesh Saved!";
            ScanText.color = BrightColor;
            LoadText.color = BrightColor;
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_MeshOperation;
        }

        void Load()
        {
            bool allowLoad = !ViveSR_RigidReconstruction.IsScanning
                && !ViveSR_RigidReconstruction.IsExporting
                && !StaticMeshTools.StaticMeshScript.ModelIsLoading
                && StaticMeshTools.StaticMeshScript.CheckModelFileExist();

            if (!allowLoad) return;

            ViveSR_Experience_ControllerDelegate.gripDelegate -= HandleGrip_SwitchMode;

            StaticMeshTools.StaticMeshScript.LoadMesh(true, false, Load_before, Load_after);
        }
        void Load_before()
        {
            LoadText.color = OriginalColor;
            HintText.text = "Loading...";
            TriggerCanvas.SetActive(false);
            GripCanvas.SetActive(false);   
        }
        void Load_after()
        {
            StaticMeshTools.StaticMeshScript.WaitForCldPool(Load_WaitForCldPool_done);
        }
        void Load_WaitForCldPool_done()
        {
            HintText.text = "Mesh Loaded!";
            ScanText.color = BrightColor;
            TriggerCanvas.SetActive(true);
            DartText.text = "Lay Tiles";
            GripCanvas.SetActive(true);
            ViveSR_Experience_ControllerDelegate.gripDelegate += HandleGrip_SwitchMode;

            SwitchActionMode(ActionMode.TileControl);
        }  

        #endregion

        private void HandleTouchpad_TileOperation(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, false);
                    HandleTouchpad_TileOperation_PressDown(touchpadDirection);  
                    break;
            }
        }                 
        void HandleTouchpad_TileOperation_PressDown(TouchpadDirection touchpadDirection)
        {                                        
            switch (touchpadDirection)
            {
                case TouchpadDirection.Up:
                    EnableDepthOcclusion(!occlusionEnabled);
                    SetUI_ToTileSpawn();
                    break;

                case TouchpadDirection.Left:
                    if (!isTriggerDown) TileSpawnerScript.RotateFloatingTile(10.0f);
                    break;

                case TouchpadDirection.Right:
                    if (!isTriggerDown) TileSpawnerScript.RotateFloatingTile(-10.0f); break;

                case TouchpadDirection.Down:
                    dartGeneratorMgr.DestroyObjs();
                    TileSpawnerScript.ClearTiles();
                    break;
            }
        }           
        private void EnableDepthOcclusion(bool enable)
        {
            ViveSR_DualCameraImageCapture.SetDepthCase(enable ? DepthCase.CLOSE_RANGE : DepthCase.DEFAULT);
            ViveSR_DualCameraImageCapture.EnableDepthProcess(enable);
            ViveSR_DualCameraImageCapture.EnableDepthRefinement(enable);
            ViveSR_DualCameraImageRenderer.UpdateDepthMaterial = enable;
            ViveSR_DualCameraImageRenderer.DepthImageOcclusion = enable;
            ViveSR_DualCameraImageRenderer.OcclusionNearDistance = 0.05f;
            occlusionEnabled = enable;
        }

        private void HandleTouchpad_ColliderOperation(ButtonStage buttonStage, Vector2 axis)
        {
            if (isTriggerDown) return;

            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, false);
                    HandleTouchpad_ColliderOperation_PressDown(touchpadDirection);
                    break;
            }
        }                                              
        void HandleTouchpad_ColliderOperation_PressDown(TouchpadDirection touchpadDirection)
        {
            switch (touchpadDirection)
            {
                case TouchpadDirection.Up:
                    ShowMode newShowMode = StaticMeshTools.StaticMeshScript.MeshShowMode == ShowMode.None ? ShowMode.All : ShowMode.None;
                    StaticMeshTools.StaticMeshScript.SwitchShowCollider(newShowMode);
                    break;
                case TouchpadDirection.Left: MoveToPreviousShowMode(); break;
                case TouchpadDirection.Right: MoveToNextShowMode(); SetUI_ToColliderDisplay(); break;
            }
        }

        void MoveToNextShowMode()
        {
            int cur_idx = (int)StaticMeshTools.StaticMeshScript.MeshShowMode;
            int num = (int)ShowMode.NumOfModes;

            ShowMode mode = StaticMeshTools.StaticMeshScript.MeshShowMode;
            do mode = (ShowMode)(++cur_idx % num);
            while (mode == ShowMode.None); //skip none mode

            StaticMeshTools.StaticMeshScript.SwitchShowCollider(mode);
        }      
        void MoveToPreviousShowMode()
        {
            int cur_idx = (int)StaticMeshTools.StaticMeshScript.MeshShowMode;
            int num = (int)ShowMode.NumOfModes;
            ShowMode mode;
            do
            {
                if (--cur_idx < 0) cur_idx += num;
                mode = (ShowMode)(cur_idx);
            } while (mode == ShowMode.None); //skip none mode

            StaticMeshTools.StaticMeshScript.SwitchShowCollider(mode);
        }

        #region Tile Control
        void HandleTrigger_DrawTiles(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    isTriggerDown = true;
                    EnableTileSpawn();
                    break;

                case ButtonStage.PressUp:  
                    isTriggerDown = false;
                    DisableTileSpawn();
                    break;
            }    
        }
        void EnableTileSpawn()
        {
            StopText.text = "";
            SaveText.text = "";
            GripCanvas.SetActive(false);
            TriggerCanvas.SetActive(false);
            TileSpawnerScript.TriggerPressDown();
        }
        void DisableTileSpawn()
        {
            StopText.text = "<";
            SaveText.text = ">";
            GripCanvas.SetActive(true);
            TriggerCanvas.SetActive(true);
            TileSpawnerScript.TriggerPressUp();
        }
        #endregion

        #region Dart 
        void HandleTrigger_SetDartControl(ButtonStage buttonStage, Vector2 axis)
        {
            bool allowDartControl = !ViveSR_RigidReconstruction.IsExporting
                && !ViveSR_RigidReconstruction.IsScanning
                && !StaticMeshTools.StaticMeshScript.ModelIsLoading;

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
            SetUI_ToDartControl();
        }
        void DisableDartControl()
        {
            ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_DartControl;
            if (actionMode == ActionMode.MeshControl) SetUI_ToMeshControl();
            else if (actionMode == ActionMode.ColliderDisplay) SetUI_ToColliderDisplay();
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
  
            GripCanvas.SetActive(false);
            TriggerCanvas.SetActive(false);
        }
        void SetUI_DartGeneratorType()
        {
            switch (dartGeneratorMgr.dartPlacementMode)
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
            LoadText.color = OriginalColor;
            ScanText.color = BrightColor;

            HintText.text = "Static Mesh";
            StopText.text = "[Stop]";
            SaveText.text = "[Save]";
            LoadText.text = "[Load]";
            ScanText.text = "[Scan]";
            GripText.text = "Tile Control";
            TriggerCanvas.SetActive(StaticMeshTools.StaticMeshScript.CheckModelLoaded());
            GripCanvas.SetActive(true);         
            TriggerCanvas.SetActive(true);
        }
        void SetUI_ToTileSpawn()
        {
            StopText.color = BrightColor;
            SaveText.color = BrightColor;
            LoadText.color = BrightColor;
            ScanText.color = BrightColor;

            StopText.text = "<";
            SaveText.text = ">";
            LoadText.text = "[Clear]";
            ScanText.text = occlusionEnabled? "Occlude OFF" : "Occlude ON";
            GripText.text = "View Colliders";
            HintText.text = "Tile Control";
            DartText.text = "Lay Tiles";
        }
        void SetUI_ToColliderDisplay()
        {
            StopText.color = BrightColor;
            SaveText.color = BrightColor;
            LoadText.color = BrightColor;

            ScanText.color = BrightColor;
            GripText.text = "Static Mesh";
            StopText.text = "<";
            SaveText.text = ">";
            LoadText.text = "";

            TriggerCanvas.SetActive(true);
            GripCanvas.SetActive(true);

            ShowMode CurrentCldDisplayMode = StaticMeshTools.StaticMeshScript.MeshShowMode;

            if (CurrentCldDisplayMode != ShowMode.None) ScanText.text = "[Hide All]";
            else ScanText.text = "[Show All]";

            if (StaticMeshTools.StaticMeshScript.texturedMesh != null)
            {
                string cld_display = "View Colliders\n";
                string all = "All";
                string hor = "Horizontal";
                string ver = "Vertical";
                string near = " - Nearest";
                string far = " - Furthest";
                string large= " - Largest";

                if (CurrentCldDisplayMode == ShowMode.All)
                    HintText.text = cld_display + all;
                else if (CurrentCldDisplayMode == ShowMode.Horizon)
                    HintText.text = cld_display + hor;
                else if (CurrentCldDisplayMode == ShowMode.LargestHorizon)
                    HintText.text = cld_display + hor + large;
                else if (CurrentCldDisplayMode == ShowMode.AllVertical)
                    HintText.text = cld_display + ver;
                else if (CurrentCldDisplayMode == ShowMode.LargestVertical)
                    HintText.text = cld_display + ver + large;
                else if (CurrentCldDisplayMode == ShowMode.NearestHorizon)
                    HintText.text = cld_display + hor + near;
                else if (CurrentCldDisplayMode == ShowMode.FurthestHorizon)
                    HintText.text = cld_display + hor + far;
                else if (CurrentCldDisplayMode == ShowMode.NearestVertical)
                    HintText.text = cld_display + ver + near;
                else if (CurrentCldDisplayMode == ShowMode.FurthestVertical)
                    HintText.text = cld_display + ver + far;
                else
                    HintText.text = cld_display; 
            }
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