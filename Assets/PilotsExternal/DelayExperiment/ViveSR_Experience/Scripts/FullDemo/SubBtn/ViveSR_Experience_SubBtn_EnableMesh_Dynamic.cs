using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_SubBtn_EnableMesh_Dynamic : ViveSR_Experience_ISubBtn
    {
        [SerializeField] EnableMesh_SubBtn SubBtnType;
        ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr_dynamic;

        ViveSR_Experience_DynamicMesh DynamicMeshScript;

        protected override void StartToDo()
        {
            DynamicMeshScript = GetComponent<ViveSR_Experience_DynamicMesh>();
            ThisButtonTypeNum = (int)SubBtnType;
            dartGeneratorMgr_dynamic = ViveSR_Experience_Demo.instance.DartGeneratorMgrs[DartGeneratorIndex.ForDynamic];

            DynamicMeshScript.SetMeshDisplay(true);
        }   

        public override void ExecuteToDo()
        {                                          
            if (isOn)
            {
                ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_StaticMR].ForceExcute(false);
                ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_StaticVR].ForceExcute(false);
                ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger; 
                ViveSR_Experience_ControllerDelegate.gripDelegate += HandleGrip;
            }
            else
            {
                ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger;
                ViveSR_Experience_ControllerDelegate.gripDelegate -= HandleGrip;
                dartGeneratorMgr_dynamic.DestroyObjs();
            }
            dartGeneratorMgr_dynamic.gameObject.SetActive(isOn);

            DynamicMeshScript.SetDynamicMesh(isOn);
        }

        void HandleTrigger(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    if (Time.timeSinceLevelLoad - dartGeneratorMgr_dynamic.tempTime > dartGeneratorMgr_dynamic.coolDownTime)
                    {
                        ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton.EnableMesh].SubMenu.RenderSubBtns(false);
                        ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                        disabled = true;
                    }
                    break;
                case ButtonStage.PressUp:
                    disabled = false;
                    ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton.EnableMesh].SubMenu.RenderSubBtns(true);
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
                    break;
            }
        }
        void HandleGrip(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    DynamicMeshScript.SetWireframeDisplay(!DynamicMeshScript.ShowWireframe);
                    break;
            }
        }              
    }
}