using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_SubBtn_3DPreview_Save : ViveSR_Experience_ISubBtn
    { 
        [SerializeField] ViveSR_Experience_Tutorial_InputHandler_3DPreview TutorialInputHandler_3DPreview;

        [SerializeField] _3DPreview_SubBtn SubBtnType;

        ViveSR_Experience_StaticMeshToolManager StaticMeshTools;

        int chairNum = 0;

        public UnityEvent OnMeshSaved;

        protected override void AwakeToDo()
        {
            ThisButtonTypeNum = (int)SubBtnType;             
        }

        protected override void StartToDo()
        {
            StaticMeshTools = ViveSR_Experience_Demo.instance.StaticMeshTools;
        }                       
       
        public override void ExecuteToDo()
        {
            ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
            SubMenu.RenderSubBtns(false);

            StaticMeshTools.SceneUnderstandingScript.SetChairSegmentationConfig(true);

            chairNum = 0;

            StaticMeshTools.SceneUnderstandingScript.TestSegmentationResult(UpdatePercentage_Segmentation, TestSegmentationResult_done);
        }
        void TestSegmentationResult_done()
        {
            List<SceneUnderstandingDataReader.SceneUnderstandingObject> SegResults = StaticMeshTools.SceneUnderstandingScript.GetSegmentationInfo(SceneUnderstandingObjectType.CHAIR);

            StaticMeshTools.SceneUnderstandingScript.GenerateHintLocators(SegResults);

            chairNum = StaticMeshTools.SceneUnderstandingScript.GetSegmentationInfo(SceneUnderstandingObjectType.CHAIR).Count;

            ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton.Segmentation].EnableButton(chairNum > 0);

            StaticMeshTools.SceneUnderstandingScript.SetSegmentation(false);
            StaticMeshTools.StaticMeshScript.ExportModel(UpdatePercentage_Mesh, ExportModel_done);
        }
        void ExportModel_done()
        {
            if (OnMeshSaved != null) OnMeshSaved.Invoke();
            ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
            SubMenu.RenderSubBtns(true);

            isOn = false;

            //Disable the [Save] button.
            SubMenu.subBtnScripts[ThisButtonTypeNum].isOn = false;
            SubMenu.subBtnScripts[ThisButtonTypeNum].EnableButton(false);

            //Enable the [Scan] button.
            ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton._3DPreview_Scan].ForceExcute(false);
            ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton._3DPreview_Scan].EnableButton(true);

            ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "", false);

            ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onHeadSet, "Mesh & Chair Data Saved!", true);

            //[Enable Mesh] is available.
            if (StaticMeshTools.StaticMeshScript.CheckModelFileExist())
            {
                ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_StaticMR].EnableButton(true);
                ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_StaticVR].EnableButton(true);
            }

            StaticMeshTools.SceneUnderstandingScript.ClearHintLocators();
        }

        void UpdatePercentage_Mesh(int percentage)
        {
            ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "Saving " + chairNum.ToString() + " Chair" + "\nSaving Mesh Data..." + percentage + "%", false);
        }
        void UpdatePercentage_Segmentation(int percentage)
        {
            ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "Saving Chair Data..." + percentage.ToString() + "%", false);
        }
    }
}