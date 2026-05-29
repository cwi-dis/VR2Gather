using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.UserRepresentation.Voice;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Record the local user's voice to a WAV file via the VoicePipelineSelf.
    /// </summary>
    public class RecordUserVoice : MonoBehaviour
    {
        [Tooltip("Start recording automatically (otherwise call StartRecording() method)")]
        public bool autoStartRecord = false;
        [Tooltip("Voice pipeline (default: automatically discovered)")]
        [SerializeField]VoicePipelineSelf voicePipeline;

        [Tooltip("Filename, can contain {scene} and {time} constructs")]
        public string outputFilename;

        string _currentFilename;

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
            filename = System.IO.Path.Combine(Application.persistentDataPath, filename);
            _currentFilename = filename;
            voicePipeline.StartRecording(filename);
#if VRT_WITH_STATS
            Statistics.Output("RecordUserVoice", $"recording_started=1, filename={filename}");
#endif
        }

        public void StopRecording()
        {
            if (voicePipeline == null) return;
            voicePipeline.StopRecording();
#if VRT_WITH_STATS
            Statistics.Output("RecordUserVoice", $"recording_stopped=1, filename={_currentFilename}");
#endif
            _currentFilename = null;
        }

        public void AddMarker(string markerName)
        {
#if VRT_WITH_STATS
            Statistics.Output("RecordUserVoice", $"marker={markerName}, filename={_currentFilename}");
#endif
        }
    }
}

