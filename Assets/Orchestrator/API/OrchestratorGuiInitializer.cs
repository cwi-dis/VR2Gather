using UnityEngine;
using UnityEngine.UI;

namespace VRT.Orchestrator
{
    public class OrchestratorGuiInitializer : MonoBehaviour
    {
        [SerializeField]
        bool overrideFields = false;
        [Header("User Credentials")]
        [SerializeField]
        private string defaulOrchURL = null;
        [SerializeField]
        private InputField defaulOrchUrlIF = null;

        [Header("User Credentials")]
        [SerializeField]
        private string defaultUserName = null;
        [SerializeField]
        private InputField userNameIF = null;
        [SerializeField]
        private string defaultUserPassword = null;
        [SerializeField]
        private InputField userPasswordIF = null;

        [Header("Rabbit MQ")]
        [SerializeField]
        private string defaultMQurl = null;
        [SerializeField]
        private InputField MQurlIF = null;
        [SerializeField]
        private string defaultMQname = null;
        [SerializeField]
        private InputField MQnameIF = null;

#if UNITY_EDITOR
        void Awake()
        {
            if (!overrideFields) return;

            defaulOrchUrlIF.text = defaulOrchURL;
            userNameIF.text = defaultUserName;
            userPasswordIF.text = defaultUserPassword;
            MQurlIF.text = defaultMQurl;
            MQnameIF.text = defaultMQname;
        }
#endif
    }
}