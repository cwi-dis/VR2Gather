using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
        public class PositionTracker : MonoBehaviour
    {
        public string inputFile;
        public string outputFile;
        public bool isPlayingBack = false;
        public bool isRecording = false;
        void Awake() 
        {
            inputFile = VRTConfig.Instance.LocalUser.PositionTracker.inputFile;
            outputFile = VRTConfig.Instance.LocalUser.PositionTracker.outputFile;
            isPlayingBack = !string.IsNullOrEmpty(inputFile);
            isRecording = !string.IsNullOrEmpty(outputFile);
            if ( !isPlayingBack && !isRecording ) {
                gameObject.SetActive(false);
                Debug.Log($"{Name()}: Disabled");
            }
        }

        string Name() {
            return "PositionTracker";
        }

        private void LoadPositions() {
            Debug.Log($"{Name()}: Loading positions from {inputFile}");
        }

        private void SavePositions() {
            Debug.Log($"{Name()}: Saving positions to {outputFile}");
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log($"{Name()}: Started");      
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        void OnDestroy() {
            if (isRecording) {
                SavePositions();
            }
        }
    }
}
