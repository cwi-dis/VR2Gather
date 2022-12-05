using UnityEngine;


namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_SubBtn_EnableMesh_StaticVR : ViveSR_Experience_ISubBtn
    {
        [SerializeField] EnableMesh_SubBtn SubBtnType;
        ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr_static;
        ViveSR_Experience_StaticMeshToolManager StaticMeshTools;

        protected override void StartToDo()
        {
            StaticMeshTools = ViveSR_Experience_Demo.instance.StaticMeshTools;
            ThisButtonTypeNum = (int)SubBtnType;
            dartGeneratorMgr_static = ViveSR_Experience_Demo.instance.DartGeneratorMgrs[DartGeneratorIndex.ForStatic];
            EnableButton(StaticMeshTools.StaticMeshScript.CheckModelFileExist());
        }

        public override void ExecuteToDo()
        {
            DualCameraDisplayMode targetMode = isOn ? DualCameraDisplayMode.VIRTUAL : DualCameraDisplayMode.MIX;
            StaticMeshTools.SwitchModeScript.SwitchMode(targetMode);
            if (isOn)
            {
                ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_Dynamic].ForceExcute(false);
                ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_StaticMR].ForceExcute(false);

                StaticMeshTools.StaticMeshScript.LoadMesh(true, true,
                () => {
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                    SubMenu.RenderSubBtns(false);
                    disabled = true;
                    ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onHeadSet, "[Enable Mesh]\nLoading...", false);
                },
                () => {
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
                    SubMenu.RenderSubBtns(true);
                    disabled = false;
                    ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "", false);
                    ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onHeadSet, "Mesh Loaded!", true, 1f);
                    dartGeneratorMgr_static.gameObject.SetActive(true);
                    ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger;
                });
            }
            else
            {
                StaticMeshTools.StaticMeshScript.LoadMesh(false);
                dartGeneratorMgr_static.gameObject.SetActive(false);
                ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger;
                dartGeneratorMgr_static.DestroyObjs();
            }


        }

        void HandleTrigger(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    if (Time.timeSinceLevelLoad - dartGeneratorMgr_static.tempTime > dartGeneratorMgr_static.coolDownTime)
                    {
                        ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton.EnableMesh].SubMenu.RenderSubBtns(false);
                        ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                    }
                    disabled = true;
                    break;

                case ButtonStage.PressUp:
                    disabled = false;
                    ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton.EnableMesh].SubMenu.RenderSubBtns(true);
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
                    break;
            }
        }
    }
}