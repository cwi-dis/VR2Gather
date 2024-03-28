

using System.ComponentModel;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif
using UnityEngine;

namespace VRT.Core
{
    public class UnityRecorderController : MonoBehaviour
    {
#if UNITY_EDITOR
        RecorderController m_RecorderController;
        public bool m_RecordAudio = true;
        internal MovieRecorderSettings m_Settings = null;

        public FileInfo OutputFile {
            get {
                var fileName = m_Settings.OutputFile + ".mp4";
                return new FileInfo(fileName);
            }
        }

        void OnEnable() {
            Initialize();
        }

        internal void Initialize() {
            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            m_RecorderController = new RecorderController(controllerSettings);

            var mediaOutputFolder = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "SampleRecordings"));

            // Video
            m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            m_Settings.name = "My Video Recorder";
            m_Settings.Enabled = true;

            // This example performs an MP4 recording
            m_Settings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
            m_Settings.VideoBitRateMode = VideoBitrateMode.Medium;

            m_Settings.ImageInputSettings = new Camera360InputSettings {
                OutputWidth = 4096,
                OutputHeight = 2160,
                MapSize = 1024,
                CameraTag = "MainCamera",
                RenderStereo = false
            };

            m_Settings.AudioInputSettings.PreserveAudio = false;// m_RecordAudio;

            // Simple file name (no wildcards) so that FileInfo constructor works in OutputFile getter.
            m_Settings.OutputFile = mediaOutputFolder.FullName + "/" + "video";

            // Setup Recording
            controllerSettings.AddRecorderSettings(m_Settings);
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = 30.0f;

            RecorderOptions.VerboseMode = false;
            m_RecorderController.PrepareRecording();
            m_RecorderController.StartRecording();

            Debug.Log($"Started recording for file {OutputFile.FullName}");
        }

        void OnDisable() {
            m_RecorderController.StopRecording();
        }
#endif
        private void Update() {
        }
    }
}
