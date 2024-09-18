using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEditor.EditorTools;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
        public class PositionTracker : MonoBehaviour
    {
        [Serializable]
        public class PositionItem {
            public int ts;
            public Vector3 p_pos;
            public Quaternion p_rot;
            public Vector3 c_pos;
            public Quaternion c_rot;
        };
        public class PositionData {
            public List<PositionItem> positions;
        }
        PositionData positionData;

        public int positionIndex;
        [Tooltip("The body of this player")]
        public Transform BodyTransform;
        [Tooltip("The camera of this player")]
        public Transform CameraTransform;
        string inputFile;
        string outputFile;

        System.DateTime epoch = System.DateTime.UnixEpoch;

        [Tooltip("Interval in ms between samples, for recording")]
        public int timeInterval = 1000;
        [Tooltip("Introspection: next time in ms we will take a sample")]
        public int nextSampleTime;
        [Tooltip("Introspection: true if we are playing back")]
        public bool isPlayingBack = false;
        [Tooltip("Introspection: true if we are recording")]
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
                positionData = JsonUtility.FromJson<PositionData>(json);
            } catch(FileNotFoundException) {
                Debug.LogError($"{Name()}: File not found: {filename}");
                isPlayingBack = false;
            }
        }

        private void SavePositions() {
            Debug.Log($"{Name()}: Saving positions to {outputFile}");
            string filename = VRTConfig.ConfigFilename(outputFile);
            
            string json = JsonUtility.ToJson(positionData, true);
            System.IO.File.WriteAllText(filename, json);
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log($"{Name()}: Started");
            if (isPlayingBack) {
                LoadPositions();
            }
            if (positionData == null) {
                positionData = new();
            }
            if (positionData.positions == null) {
                positionData.positions = new();
            } 
        }

        int currentTime() {
            System.DateTime now = System.DateTime.Now;
            if (epoch == System.DateTime.UnixEpoch) {
                epoch = now;
            }
            double tsFloat = (now - epoch).TotalMilliseconds;
            return (int)tsFloat;
        }

        void RecordSample(int now) {
            Vector3 p_pos = BodyTransform.position;
            Quaternion p_rot = BodyTransform.rotation;
#if bad
            // Compute camera position/rotation relative to BodyTransform
            Vector3 c_pos = BodyTransform.InverseTransformPoint(CameraTransform.position);
            Quaternion c_rot = BodyTransform.InverseTransform
#else
            // We simply store everything in world coordinates.
            Vector3 c_pos = CameraTransform.position;
            Quaternion c_rot = CameraTransform.rotation;
#endif
            PositionItem data = new()
            {
                ts = now,
                p_pos = p_pos,
                p_rot = p_rot,
                c_pos = c_pos,
                c_rot = c_rot
            };
            positionData.positions.Add(data);
        }

        void PlaybackSample(PositionItem pos)
        {
            Debug.Log($"{Name()}: set position for ts={pos.ts}");
            BodyTransform.position = pos.p_pos;
            BodyTransform.rotation = pos.p_rot;
            CameraTransform.position = pos.c_pos;
            CameraTransform.rotation = pos.c_rot;
        }
        
        // Update is called once per frame
        void Update()
        {
            int now = currentTime();
            if (isRecording) {
                if (now >= nextSampleTime) {
                    RecordSample(now);
                    nextSampleTime = now + timeInterval;
                }
            }
            if (isPlayingBack) {
                if (positionData.positions.Count == 0) {
                    isPlayingBack = false;
                    Debug.Log($"{Name()}: End of data, stop playback");
                }
                else
                {
                    PositionItem nextPosition = positionData.positions[0];
                    if (now >= nextPosition.ts) {
                        positionData.positions.RemoveAt(0);
                        PlaybackSample(nextPosition);
                    }
                }
            }
        }

        void OnDestroy() {
            if (isRecording) {
                SavePositions();
            }
        }
    }
}
