using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Cwipc;
using JetBrains.Annotations;
using Unity.Profiling;
// using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        PositionItem previousPosition;
        PositionItem nextPosition;
        [Tooltip("Enable debug logging")]
        public bool debug;

        void Awake() 
        {
            inputFile = VRTConfig.Instance.LocalUser.PositionTracker.inputFile;
            outputFile = VRTConfig.Instance.LocalUser.PositionTracker.outputFile;
            string sceneName = SceneManager.GetActiveScene().name;
            string dateTime = DateTime.Now.ToString("yyyyMMdd-HHmm");
            outputFile = outputFile.Replace("{scene}", sceneName);
            outputFile = outputFile.Replace("{time}", dateTime);
            
            if (VRTConfig.Instance.LocalUser.PositionTracker.outputIntervalOverride > 0) {
                timeInterval = VRTConfig.Instance.LocalUser.PositionTracker.outputIntervalOverride;
            }
            isPlayingBack = !string.IsNullOrEmpty(inputFile);
            isRecording = !string.IsNullOrEmpty(outputFile);
            if ( !isPlayingBack && !isRecording ) {
                gameObject.SetActive(false);
                Debug.Log($"{Name()}: Disabled");
            }
#if VRT_WITH_STATS
            if (isRecording) {
                Statistics.Output(Name(), $"outputFile={outputFile}, interval_ms={timeInterval}");
            }
            if (isPlayingBack) {
                Statistics.Output(Name(), $"inputFile={inputFile}");
            }
#endif
        }

        string Name() {
            return "PositionTracker";
        }

        private void LoadPositions() {
            string filename = VRTConfig.ConfigFilename(inputFile, force:true);
            Debug.Log($"{Name()}: Loading positions from {filename}");
            try
            {
                string json = System.IO.File.ReadAllText(filename);
                positionData = JsonUtility.FromJson<PositionData>(json);
            } catch(FileNotFoundException) {
                Debug.LogError($"{Name()}: File not found: {filename}");
                isPlayingBack = false;
            }
        }

        private void SavePositions() {
            string filename = VRTConfig.ConfigFilename(outputFile, force:true);
            Debug.Log($"{Name()}: Saving positions to {filename}");

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

            // We simply store everything in world coordinates.
            Vector3 c_pos = CameraTransform.position;
            Quaternion c_rot = CameraTransform.rotation;

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

        bool PopSample() {
            if (positionData.positions.Count == 0) {
                isPlayingBack = false;
                Debug.Log($"{Name()}: End of data, stop playback");
                return false;
            }
            else
            {
                previousPosition = nextPosition;
                nextPosition = positionData.positions[0];
                positionData.positions.RemoveAt(0);
            }
            if (previousPosition == null) {
                previousPosition = nextPosition;
            }
            return true;
        }

        void PlaybackSample()
        {
            int now = currentTime();
            if (nextPosition == null || nextPosition.ts < now) {
                if (!PopSample()) {
                    return;
                }
            }
        
            if (now >= nextPosition.ts) {
                // The next position time has already passed. hard-set it.
                if (debug) Debug.Log($"{Name()}: set position for ts={nextPosition.ts}");
                BodyTransform.position = nextPosition.p_pos;
                BodyTransform.rotation = nextPosition.p_rot;
                CameraTransform.position = nextPosition.c_pos;
                CameraTransform.rotation = nextPosition.c_rot;
                previousPosition = nextPosition;
                if (positionData.positions.Count == 0)
                {
                    isPlayingBack = false;
                    Debug.Log($"{Name()}: End of data, stop playback");
                    return;
                }
                positionData.positions.RemoveAt(0);
                return;
            }
            // Otherwise we lerp.
            float interval = nextPosition.ts - previousPosition.ts;
            if (interval == 0) {
                interval = 1f;
            }
            float fraction = (now - previousPosition.ts) / interval;
            if (debug) Debug.Log($"{Name()}: set position for now={now}, ts={nextPosition.ts}, frac={fraction}");
            BodyTransform.position = Vector3.Lerp(previousPosition.p_pos, nextPosition.p_pos, fraction);
            BodyTransform.rotation = Quaternion.Lerp(previousPosition.p_rot, nextPosition.p_rot, fraction);
            CameraTransform.position = Vector3.Lerp(previousPosition.c_pos, nextPosition.c_pos, fraction);
            CameraTransform.rotation = Quaternion.Lerp(previousPosition.c_rot, nextPosition.c_rot, fraction);
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
                PlaybackSample();
            }
        }

        void OnDestroy() {
            if (isRecording) {
                SavePositions();
            }
        }
    }
}
