using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_SubBtn_EnableMesh_StaticMR : ViveSR_Experience_ISubBtn
    {
        [SerializeField] EnableMesh_SubBtn SubBtnType;             

        ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr_static;
        ViveSR_Experience_StaticMesh StaticMeshScript;

        protected override void StartToDo()
        {
            StaticMeshScript = ViveSR_Experience_Demo.instance.StaticMeshTools.StaticMeshScript;

            ThisButtonTypeNum = (int)SubBtnType;
            dartGeneratorMgr_static = ViveSR_Experience_Demo.instance.DartGeneratorMgrs[DartGeneratorIndex.ForStatic];
            EnableButton(StaticMeshScript.CheckModelFileExist());
        }

        public override void ExecuteToDo()
        {
            if (isOn)
            {
                ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_Dynamic].ForceExcute(false);
                ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_StaticVR].ForceExcute(false);

                StaticMeshScript.LoadMesh(true, false,
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
                        ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onHeadSet, "Mesh Loaded!", true);
                        dartGeneratorMgr_static.gameObject.SetActive(true);
                        ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger;
                        ViveSR_Experience_ControllerDelegate.gripDelegate += HandleGrip;
                    }
                );  
            }
            else
            {
                StaticMeshScript.SwitchShowCollider(ShowMode.None);
                StaticMeshScript.LoadMesh(false);
                dartGeneratorMgr_static.gameObject.SetActive(false);
                ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger;
                ViveSR_Experience_ControllerDelegate.gripDelegate -= HandleGrip;
                dartGeneratorMgr_static.DestroyObjs();
            } 
        }


        void HandleTrigger(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    disabled = true;

                    if (Time.timeSinceLevelLoad - dartGeneratorMgr_static.tempTime > dartGeneratorMgr_static.coolDownTime)
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
                        StaticMeshScript.SwitchShowCollider(StaticMeshScript.MeshShowMode == ShowMode.All? ShowMode.None : ShowMode.All);
                    break;
            }                                   
        }
    }
}