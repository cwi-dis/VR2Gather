using UnityEngine;
using VRT.Pilots.Common;

namespace VRT.Pilots.Pilot0
{
    public class Pilot0Controller : PilotController
    {
        public static Pilot0Controller Instance { get; private set; }

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
        }

        public override void MessageActivation(string message) { }
    }
}