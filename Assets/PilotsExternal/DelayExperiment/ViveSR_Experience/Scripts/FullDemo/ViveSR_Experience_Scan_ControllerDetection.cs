using UnityEngine;
using UnityEngine.Events;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Scan_ControllerDetection : MonoBehaviour
    {                                                                                     
        public UnityEvent OnBecameVisibleEvent, OnBecameInvisibleEvent;

        void OnBecameInvisible()
        {
            if (OnBecameInvisibleEvent != null) OnBecameInvisibleEvent.Invoke();
        }

        void OnBecameVisible()
        {
            if (OnBecameVisibleEvent != null) OnBecameVisibleEvent.Invoke();
        }

    }
}