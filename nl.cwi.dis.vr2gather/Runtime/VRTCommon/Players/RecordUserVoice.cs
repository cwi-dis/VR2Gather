using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.UserRepresentation.Voice;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Allow disabling a user to move (because the scenario requires them to be static)
    /// </summary>
    public class RecordUserVoice : MonoBehaviour
    {
        [Tooltip("Start recording automatically (otherwise call StartRecording() method)")]
        public bool autoStartRecord = false;
        [Tooltip("Voice pipeline (default: automatically discovered)")]
        [SerializeField]VoicePipelineSelf voicePipeline;

        [Tooltip("Filename, can contain {scene} and {time} constructs")]
        public string outputFilename;

        void OnEnable()
        {
            if (voicePipeline == null)
            {
                voicePipeline = FindFirstObjectByType<VoicePipelineSelf>(FindObjectsInactive.Include);
            }

            if (voicePipeline == null)
            {
                Debug.LogError("RecordingUserVoice: No VoicePipeline found");
            }
        }
        
        // Start is called before the first frame update
        void Start()
        {
            if (autoStartRecord)
            {
                StartRecording(outputFilename);
            }
        }

        public void StartRecording(string filename)
        {
            if (voicePipeline == null)
            {
                Debug.LogError($"RecordUserVoice: StartRecording: no voice pipeline");
                return;
            }
            if (string.IsNullOrEmpty(filename)) {
                Debug.LogError($"RecordUserVoice: StartRecording: filename is empty");
                return;
            }
            string sceneName = SceneManager.GetActiveScene().name;
            string dateTime = DateTime.Now.ToString("yyyyMMdd-HHmm");
            filename = filename.Replace("{scene}", sceneName);
            filename = filename.Replace("{time}", dateTime);
            voicePipeline.StartRecording(filename);
        }

        public void StopRecording()
        {
            if (voicePipeline == null) return;
            voicePipeline.StopRecording();
        }
    }
}

