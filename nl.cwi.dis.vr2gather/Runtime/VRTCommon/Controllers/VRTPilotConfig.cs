using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Base class for per-experience (per-session) settings that are loaded from a JSON file
    /// (default pilotconfig.json, found like config.json through VRTConfig.ConfigFilename(), so
    /// VRTrun can supply it in the run folder) and survive scene transitions.
    ///
    /// VRTConfig describes the machine and installation; VRTPilotConfig describes the experience:
    /// experimental condition, scenario order, participant numbers, and so on.
    ///
    /// Subclass it in your app to add the fields (public, or [SerializeField]), override HasConfig to
    /// tell whether the loaded settings are complete, and OnLoaded() to validate them. Put the
    /// subclass on a GameObject in your LoginManager scene, so it exists from the start. Access it
    /// with VRTPilotConfig.GetInstance&lt;YourPilotConfig&gt;().
    /// NOTE: when overriding Awake() ensure that base.Awake() is called.
    /// </summary>
    public class VRTPilotConfig : MonoBehaviour
    {
        [Tooltip("Where to load the configuration from")]
        public string configFilename = "pilotconfig.json";
        [Tooltip("introspection: configuration was loaded from the config file")]
        public bool wasLoaded = false;

        /// <summary>
        /// True when the settings were loaded from the config file and are complete.
        /// Override to check your own fields (and call base.HasConfig).
        /// </summary>
        public virtual bool HasConfig => wasLoaded;

        static VRTPilotConfig _Instance;
        public static VRTPilotConfig Instance
        {
            get
            {
                if (_Instance == null)
                {
                    Debug.LogError("VRTPilotConfig: Instance accessed before allocation. Must be on a Component that is initialized very early.");
                }
                return _Instance;
            }
        }

        public static bool InstanceExists()
        {
            return _Instance != null;
        }

        /// <summary>
        /// The instance as your subclass, or null if there is none (or it has another type).
        /// </summary>
        public static T GetInstance<T>() where T : VRTPilotConfig
        {
            if (_Instance == null) return null;
            var instance = _Instance as T;
            if (instance == null)
            {
                Debug.LogError($"VRTPilotConfig: Instance is a {_Instance.GetType().Name}, not a {typeof(T).Name}");
            }
            return instance;
        }

#if UNITY_EDITOR
        [ContextMenu("Save to config file")]
        private void SaveToConfigFile()
        {
            string file = VRTConfig.ConfigFilename(configFilename, force: true);
            System.IO.File.WriteAllText(file, JsonUtility.ToJson(this, true));
            Debug.Log($"VRTPilotConfig: Saved to {file}");
        }
#endif

        protected virtual void Awake()
        {
            if (_Instance != null)
            {
                Debug.LogWarning($"VRTPilotConfig: Awake() called but there is an Instance already from {_Instance.gameObject}. Keeping the old one.");
                Destroy(gameObject);
                return;
            }
            Initialize();
        }

        /// <summary>
        /// Called after the config file has been loaded. Override to validate your fields.
        /// </summary>
        protected virtual void OnLoaded(string filename)
        {
        }

        void Initialize()
        {
            _Instance = this;
            var filename = VRTConfig.ConfigFilename(configFilename, label: "Pilot config");
            if (System.IO.File.Exists(filename))
            {
                // Fills the fields of the actual (sub)class
                JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(filename), this);
                wasLoaded = true;
                OnLoaded(filename);
            }
            else
            {
                Debug.LogWarning($"VRTPilotConfig: file not found: {filename}");
            }
            DontDestroyOnLoad(gameObject);
        }
    }
}
