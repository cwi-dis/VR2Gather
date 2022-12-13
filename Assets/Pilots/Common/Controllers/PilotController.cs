using UnityEngine;
using UnityEngine.Video;
using VRT.Core;

namespace VRT.Pilots.Common
{
    abstract public class PilotController : MonoBehaviour
    {

        [HideInInspector] public float timer = 0.0f;

        // Start is called before the first frame update
        public virtual void Start()
        {
        }

        public abstract void MessageActivation(string message);
    }
}