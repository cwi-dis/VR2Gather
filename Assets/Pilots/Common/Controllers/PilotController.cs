using UnityEngine;
using UnityEngine.Video;
using VRTCore;

namespace VRT.Pilots.Common
{

    //public enum Actions { VIDEO_1_START, VIDEO_1_PAUSE, VIDEO_2_START, VIDEO_2_PAUSE, WAIT }

    abstract public class PilotController : MonoBehaviour
    {

        [HideInInspector] public float timer = 0.0f;

        // Start is called before the first frame update
        public virtual void Start()
        {
            var tmp = Config.Instance;
        }

        public abstract void MessageActivation(string message);
    }
}