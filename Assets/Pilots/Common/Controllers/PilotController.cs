using UnityEngine;
using UnityEngine.Video;
using VRT.Core;

namespace VRT.Pilots.Common
{
    abstract public class PilotController : MonoBehaviour
    {

        [Tooltip("Fade in at start of scene (default only fadeout)")]
        public bool enableFadeIn = false;

        public void Awake()
        {
           if (CameraFader.Instance != null)
            {
                CameraFader.Instance.StartFadedOut = enableFadeIn;
            }
        }
        // Start is called before the first frame update
        public virtual void Start()
        {
        }

        public abstract void MessageActivation(string message);
    }
}