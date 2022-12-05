using System.Linq;namespace Vive.Plugin.SR.Experience{    public class ViveSR_Experience_Tutorial_InputHandler_Effects : ViveSR_Experience_Tutorial_IInputHandler    {
        protected override void AwakeToDo()
        {
            Button = ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton.Effects];
        }
        public override void TriggerDown()
        {
            base.TriggerDown();
            HoldTrigger();
        }
        public override void TriggerUp()
        {
            base.TriggerUp();
            HoldTrigger();
        }

        void HoldTrigger()
        {                                   
            tutorial.SetCanvasText(TextCanvas.onRotator, tutorial.MainLineManagers[Button.ButtonType].mainLines.First(x => x.messageType == (tutorial.isTriggerPressed ? "Trigger" : "On")).text);

            tutorial.SetTouchpadSprite(!tutorial.isTriggerPressed, ControllerInputIndex.left, ControllerInputIndex.right, ControllerInputIndex.mid);
        }

        protected override void MidPressedDown()
        {
            bool isDisabled = ViveSR_Experience_Demo.instance.ButtonScripts[Button.ButtonType].disabled;
            if (isDisabled) return;

            base.MidPressedDown();

            tutorial.SetCanvas(TextCanvas.onTrigger, ViveSR_Experience_Demo.instance.ButtonScripts[Button.ButtonType].isOn);
        }    }}