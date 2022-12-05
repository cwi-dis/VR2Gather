using UnityEngine;
using System.Collections.Generic;
namespace Vive.Plugin.SR.Experience
{
    [System.Serializable]
    public class ViveSR_Experience_Tutorial_Line
    {
        public string messageType;
        [TextArea] public string text;
    }
}
