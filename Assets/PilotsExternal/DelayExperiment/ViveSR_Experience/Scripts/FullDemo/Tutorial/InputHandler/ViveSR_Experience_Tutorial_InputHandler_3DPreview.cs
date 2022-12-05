using UnityEngine;
using System.Collections;
namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Tutorial_InputHandler_3DPreview : ViveSR_Experience_Tutorial_IInputHandler
    {
        ViveSR_Experience_Scan_ControllerDetection ControllerVisibilityDetector;

        protected override void AwakeToDo()
        {
            Button = ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton._3DPreview];
        }

        protected override void StartToDo()
        {
            ControllerVisibilityDetector = ((ViveSR_Experience_SubBtn_3DPreview_Scan)ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton._3DPreview_Scan]).ControllerVisibilityDetector;
            ControllerVisibilityDetector.OnBecameInvisibleEvent.AddListener(SetSaveSubMessage);
                        

            ViveSR_Experience_SubBtn_3DPreview_Scan scanScript = ((ViveSR_Experience_SubBtn_3DPreview_Scan)ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton._3DPreview_Scan]);
            scanScript.OnGPUMemoryFullControlPanelOn.AddListener(()=>tutorial.ToggleTutorial(false));
            scanScript.OnGPUMemoryFullControlPanel_Abort.AddListener(() => tutorial.ToggleTutorial(true));

            ViveSR_Experience_SubBtn_3DPreview_Save saveScript = ((ViveSR_Experience_SubBtn_3DPreview_Save)ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton._3DPreview_Save]);
            saveScript.OnMeshSaved.AddListener(MeshSaved);

        }

        protected override void MidPressedDown()
        {
            if (SubMenu.SelectedButton == (int)_3DPreview_SubBtn.Scan)
            {
                base.MidPressedDown();
            }

            if (SubMenu.SelectedButton == (int)_3DPreview_SubBtn.Save && !SubMenu.subBtnScripts[(int)_3DPreview_SubBtn.Save].disabled)
            {
                tutorial.ToggleTutorial(false);
            }
        }

        void MeshSaved()
        {      
            tutorial.ToggleTutorial(true);

            if (SubMenu.SelectedButton == (int)_3DPreview_SubBtn.Save) SetSubBtnMessage("Disabled");
            tutorial.SetTouchpadSprite(true, ControllerInputIndex.mid);
        }

        public void SetSaveSubMessage()
        {
            SetSubBtnMessage();
        }                              
    }
}