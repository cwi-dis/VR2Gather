using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
    //
    // This script emulates HandController for use when you have a gamepad controller.
    //
    // When you press the grope key you will see an indication whether the object in the center of the screen is
    // touchable. If so you can press touchKey and touch it.
    //
    // Grabbing not implemented, because it doesn't seem to useful (without hands). But doable
    // if we want to.
    //
    public class HandControllerGamepad : HandControllerEmulation
    {
        
    }
}
