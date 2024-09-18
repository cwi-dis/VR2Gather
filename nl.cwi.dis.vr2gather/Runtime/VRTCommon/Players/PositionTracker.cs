using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
        public class PositionTracker : MonoBehaviour
    {
        [Serializable]
        public class PositionData {
            Int64 ts;
            Vector3 p_pos;
            Vector3 p_angle;
            Vector3 c_pos;
            Vector3 c_angle;
        };

        PositionData[] positions;

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
            string filename = VRTConfig.ConfigFilename(inputFile);
            try {
                string json = System.IO.File.ReadAllText(filename);
                JsonUtility.FromJson<PositionData[]>(json);
            } catch(FileNotFoundException) {
                Debug.LogError($"{Name()}: File not found: {filename}");
                isPlayingBack = false;
            }
        }

        private void SavePositions() {
            Debug.Log($"{Name()}: Saving positions to {outputFile}");
            string filename = VRTConfig.ConfigFilename(outputFile);
            string json = JsonUtility.ToJson(positions);
            System.IO.File.WriteAllText(filename, json);
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log($"{Name()}: Started");
            if (isPlayingBack) {
                LoadPositions();
            }     
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
