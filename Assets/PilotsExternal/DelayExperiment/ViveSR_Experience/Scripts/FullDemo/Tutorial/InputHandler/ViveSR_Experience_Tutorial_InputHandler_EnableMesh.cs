using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Tutorial_InputHandler_EnableMesh : ViveSR_Experience_Tutorial_IInputHandler
    {
        ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr_Static;
        ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr_Dynamic;

        ViveSR_Experience_StaticMesh StaticMeshScript;

        protected override void AwakeToDo()
        {
            Button = ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton.EnableMesh];
        }

        protected override void StartToDo()
        {
            StaticMeshScript = ViveSR_Experience_Demo.instance.StaticMeshTools.StaticMeshScript;
            dartGeneratorMgr_Static = ViveSR_Experience_Demo.instance.DartGeneratorMgrs[DartGeneratorIndex.ForStatic];
            dartGeneratorMgr_Dynamic = ViveSR_Experience_Demo.instance.DartGeneratorMgrs[DartGeneratorIndex.ForDynamic];
        }

        public override void SetTouchpadText(Vector2 touchpad)
        {
            tutorial.currentInput = tutorial.GetCurrentSprite(touchpad);

            if (tutorial.isTriggerPressed)
            {
                if (tutorial.currentInput == ControllerInputIndex.right || tutorial.currentInput == ControllerInputIndex.left || tutorial.currentInput == ControllerInputIndex.up || tutorial.currentInput == ControllerInputIndex.down)
                {
                    SetDartGeneratorMessage(true);
                    base.SetTouchpadText(touchpad);
                }                
            }
            else
            {
                base.SetTouchpadText(touchpad);
            }
        }

        public override void TriggerDown()
        {
            base.TriggerDown();
            holdObj(true, !ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_Dynamic].isOn);
        }

        public override void TriggerUp()
        {
            base.TriggerUp();
            holdObj(false, !ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_Dynamic].isOn);
              
            tutorial.SetTouchpadSprite(true, false, ControllerInputIndex.left, ControllerInputIndex.right, ControllerInputIndex.up, ControllerInputIndex.down);

            List<ControllerInputIndex> indexes = new List<ControllerInputIndex>();
            for (int i = 0; i < 4; i++) indexes.Add((ControllerInputIndex)i);
            tutorial.SetTouchpadSprite(true, false, indexes.ToArray());

        }

        void holdObj(bool isTriggerDown, bool allowSwitchingTool)
        {
            if ((StaticMeshScript.isMeshReady && ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton.EnableMesh].SubMenu.subBtnScripts[SubMenu.SelectedButton].isOn)
            || ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_Dynamic].isOn)
            {
                tutorial.SetTouchpadSprite(true, true, ControllerInputIndex.left, ControllerInputIndex.right, ControllerInputIndex.up, ControllerInputIndex.down);
                tutorial.currentInput = ControllerInputIndex.none;
                if (!allowSwitchingTool) tutorial.SetTouchpadSprite(!isTriggerDown, ControllerInputIndex.up);
                tutorial.SetTouchpadSprite(!isTriggerDown, ControllerInputIndex.mid);
                SetDartGeneratorMessage(isTriggerDown);
            }
        }

        void SetDartGeneratorMessage(bool isTriggerDown)
        {
            string targetLine = "";
            //    sphere = 0,
            //    deer = 1,  
            //    dart = 2,

            if (isTriggerDown)
            {
                if (dartGeneratorMgr_Static.isActiveAndEnabled || dartGeneratorMgr_Dynamic.isActiveAndEnabled)
                {
                    ViveSR_Experience_DartGeneratorMgr currentMgr = dartGeneratorMgr_Static.isActiveAndEnabled ? dartGeneratorMgr_Static : dartGeneratorMgr_Dynamic;

                    ViveSR_Experience_IDartGenerator DartGenerator = currentMgr.DartGenerators[currentMgr.dartPlacementMode];
                    if (DartGenerator.currentDartPrefeb == 2) targetLine = "Trigger(Dart)";
                    else if (DartGenerator.currentDartPrefeb == 0) targetLine = "Trigger(Sphere)";
                    else if (DartGenerator.currentDartPrefeb == 1) targetLine = "Trigger(ViveDeer)";
                    tutorial.SetCanvasText(TextCanvas.onRotator, tutorial.MainLineManagers[Button.ButtonType].mainLines.First(x => x.messageType == targetLine).text);
                }
            }
            else
            {
                SetSubBtnMessage();
            }

            tutorial.SetCanvas(TextCanvas.onTrigger, !isTriggerDown);
        }

        IEnumerator WaitForLoading()
        {
            while (StaticMeshScript.ModelIsLoading)
            {
                yield return new WaitForEndOfFrame();
            }
            tutorial.SetCanvas(TextCanvas.onTrigger, true);
            if (SubMenu.SelectedButton == (int)EnableMesh_SubBtn.StaticMR)
            {
                tutorial.SetCanvas(TextCanvas.onGrip, true);
            }

            SetTouchpadMessage();
            tutorial.ToggleTutorial(true);
            SetSubBtnMessage();
        }

        protected override void MidPressedDown()
        {
            base.MidPressedDown();
            if (SubMenu.SelectedButton == (int)EnableMesh_SubBtn.StaticMR)
            {
                if (ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_StaticMR].isOn) //mesh hasn't been loaded.
                {
                    if (!StaticMeshScript.isMeshReady)
                    {
                        tutorial.ToggleTutorial(false);
                        StartCoroutine(WaitForLoading());
                    }
                    else
                    {
                        tutorial.SetCanvas(TextCanvas.onTrigger, true);
                        tutorial.SetCanvas(TextCanvas.onGrip, true);
                    }
                }
                else
                {
                    tutorial.SetCanvas(TextCanvas.onTrigger, false);
                    tutorial.SetCanvas(TextCanvas.onGrip, false);
                }
            }
            else if (SubMenu.SelectedButton == (int)EnableMesh_SubBtn.StaticVR)
            {
                if (ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_StaticVR].isOn) //mesh hasn't been loaded.
                {
                    tutorial.SetCanvas(TextCanvas.onGrip, false);
                    if (!StaticMeshScript.isMeshReady)
                    {
                        tutorial.ToggleTutorial(false);
                        StartCoroutine(WaitForLoading());
                    }
                    else
                    {
                        tutorial.SetCanvas(TextCanvas.onTrigger, true);
                    }
                }
                else
                {
                    tutorial.SetCanvas(TextCanvas.onTrigger, false);
                }
            }
            else if (SubMenu.SelectedButton == (int)EnableMesh_SubBtn.Dynamic)
            {
                bool isOn = ViveSR_Experience_Demo.instance.SubButtonScripts[SubMenuButton.EnableMesh_Dynamic].isOn;
                tutorial.SetCanvas(TextCanvas.onGrip, isOn);
                tutorial.SetCanvas(TextCanvas.onTrigger, isOn);
            }
        }
    }
}