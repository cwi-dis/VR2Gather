

using System.ComponentModel;
using System.IO;
#if VRT_WITH_RECORDER
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Encoder;
#endif
using UnityEngine;

namespace VRT.Core
{
    public class UnityRecorderController : MonoBehaviour
    {
#if VRT_WITH_RECORDER
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

            // Perform an MP4 recording with medium quality
            m_Settings.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Medium
            };

            m_Settings.ImageInputSettings = new Camera360InputSettings {
                OutputWidth = 8192,
                OutputHeight = 1024,
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
