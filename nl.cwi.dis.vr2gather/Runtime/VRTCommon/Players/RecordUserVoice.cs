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
        float _recordingStartTime;

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
            filename = VRT.Core.VRTConfig.ConfigFilename(filename, force:true, label:"Voice recording");
            _currentFilename = filename;
            _recordingStartTime = Time.realtimeSinceStartup;
            voicePipeline.StartRecording(filename);
#if VRT_WITH_STATS
            Statistics.Output("RecordUserVoice", $"recording_started=1, filename={filename}");
#endif
        }

        void OnDestroy()
        {
            StopRecording();
        }

        public void StopRecording()
        {
            if (voicePipeline == null) return;
            if (_currentFilename == null) return;
            voicePipeline.StopRecording();
#if VRT_WITH_STATS
            Statistics.Output("RecordUserVoice", $"recording_stopped=1, filename={_currentFilename}");
#endif
            _currentFilename = null;
        }

        public void AddMarker(string markerName)
        {
            if (_recordingStartTime == 0)
            {
                Debug.LogError($"RecordUserVoice: AddMarker: called while not recording");
            }
#if VRT_WITH_STATS
            float position = Time.realtimeSinceStartup - _recordingStartTime;
            Statistics.Output("RecordUserVoice", $"marker={markerName}, position_sec={position:F3}, filename={_currentFilename}");
#endif
        }
    }
}

