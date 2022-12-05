using UnityEngine;
using System.Collections.Generic;
using Valve.VR;
using Valve.VR.InteractionSystem;
using System.Collections;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience : MonoBehaviour
    {
        #region singleton
        private static ViveSR_Experience _instance;
        public static ViveSR_Experience instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ViveSR_Experience>();
                }
                return _instance;
            }
        }
        #endregion

        public DeviceType CurrentDevice = DeviceType.NOT_SUPPORT;
        public bool IsAMD;
        public int AttachPointIndex;

        public SceneType scene;
                              
        public Hand targetHand;
        public GameObject PlayerHeadCollision;
        public GameObject AttachPoint;

        public List<Renderer> ControllerRenderers = new List<Renderer>();
        public GameObject ControllerObjGroup = null;

        public ViveSR_Experience_SoundManager SoundManager { get; private set; }
        public ViveSR_ControllerLatency ControllerLatency { get; private set; }

        public ViveSR_Experience_ErrorHandler ErrorHandlerScript;

        private void Awake()
        {
            Player.instance.allowToggleTo2D = false;
            SoundManager = FindObjectOfType<ViveSR_Experience_SoundManager>();
        }
    }
}