using UnityEngine;
using UnityEngine.Video;
using VRT.Core;
using Cwipc;

namespace VRT.Pilots.Common
{
    abstract public class PilotController : MonoBehaviour
    {

        [HideInInspector] public float timer = 0.0f;

        // Start is called before the first frame update
        public virtual void Start()
        {
            _ = Config.Instance;
        }

        private void OnApplicationQuit()
        {
            BaseStats.Output("PilotController", $"quitting=1");
            // xxxjack the ShowTotalRefCount call may come too early, because the VoiceDashSender and VoiceDashReceiver seem to work asynchronously...
            BaseMemoryChunkReferences.ShowTotalRefCount();
        }

        public abstract void MessageActivation(string message);
    }
}