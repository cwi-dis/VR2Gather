using System.Linq;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Tutorial_InputHandler_Portal : ViveSR_Experience_Tutorial_IInputHandler
    {
        ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr_portal;
        ViveSR_Experience_IDartGenerator DartGenerator;

        protected override void AwakeToDo()
        {
            Button = ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton.Portal];
        }

        protected override void StartToDo()
        {
            dartGeneratorMgr_portal = ViveSR_Experience_Demo.instance.DartGeneratorMgrs[DartGeneratorIndex.ForPortal];
        }

        protected override void LeftRightPressedDown()
        {                                    
            if (tutorial.isTriggerPressed) SetTriggerMessage();
            else base.LeftRightPressedDown();
        }

        public override void TriggerDown()
        {
            base.TriggerDown();

            SetTriggerMessage();

            tutorial.SetTouchpadSprite(!tutorial.isTriggerPressed, ControllerInputIndex.mid);
        }
        public override void TriggerUp()
        {
            base.TriggerUp();

            tutorial.SetMainMessage();

            tutorial.SetTouchpadSprite(!tutorial.isTriggerPressed, ControllerInputIndex.mid);
        }

        protected override void MidPressedDown()
        {
            base.MidPressedDown();
            tutorial.SetCanvas(TextCanvas.onTrigger, Button.isOn);
            tutorial.SetTouchpadSprite(Button.isOn, Button.isOn, ControllerInputIndex.up, ControllerInputIndex.down);
        }

        void SetTriggerMessage()
        {
            if(!DartGenerator) 
                DartGenerator = dartGeneratorMgr_portal.DartGenerators[dartGeneratorMgr_portal.dartPlacementMode];

            string targetLine = "";

            if (DartGenerator.isActiveAndEnabled)
            {
                if (DartGenerator.currentDartPrefeb == 2) targetLine = "Trigger(Sword)";
                else if (DartGenerator.currentDartPrefeb == 0) targetLine = "Trigger(Sphere)";
                else if (DartGenerator.currentDartPrefeb == 1) targetLine = "Trigger(ViveDeer)";

                ViveSR_Experience_IButton CurrentButton = ViveSR_Experience_Demo.instance.Rotator.CurrentButton;

                tutorial.SetCanvasText(TextCanvas.onRotator, tutorial.MainLineManagers[CurrentButton.ButtonType].mainLines.First(x => x.messageType == targetLine).text);
            }
        }
    }
}