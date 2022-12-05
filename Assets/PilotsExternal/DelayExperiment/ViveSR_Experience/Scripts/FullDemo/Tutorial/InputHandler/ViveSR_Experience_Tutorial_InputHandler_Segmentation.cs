using UnityEngine;
using System.Collections;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Tutorial_InputHandler_Segmentation : ViveSR_Experience_Tutorial_IInputHandler
    {
        protected override void AwakeToDo()
        {
            Button = ViveSR_Experience_Demo.instance.ButtonScripts[MenuButton.Segmentation];
        }                           

        protected override void MidPressedDown()
        {
            base.MidPressedDown();

            tutorial.SetTouchpadSprite(true, true, ControllerInputIndex.up);
        }
    }
}