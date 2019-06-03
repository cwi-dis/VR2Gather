using UnityEngine;
using UnityEngine.UI;

public class OrchestratorGuiInitializer : MonoBehaviour
{
    [SerializeField]
    bool overrideFields = false;
    [Header("User Credentials")]
    [SerializeField]
    private string defaulOrchURL;
    [SerializeField]
    private InputField defaulOrchUrlIF;

    [Header("User Credentials")]
    [SerializeField]
    private string defaultUserName;
    [SerializeField]
    private InputField userNameIF;
    [SerializeField]
    private string defaultUserPassword;
    [SerializeField]
    private InputField userPasswordIF;

    [Header("Rabbit MQ")]
    [SerializeField]
    private string defaultMQurl;
    [SerializeField]
    private InputField MQurlIF;
    [SerializeField]
    private string defaultMQname;
    [SerializeField]
    private InputField MQnameIF;

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