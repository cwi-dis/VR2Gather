using UnityEngine;
using UnityEngine.UI;

public class OrchestratorGuiInitializer : MonoBehaviour
{
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

    void Awake()
    {
        userNameIF.text = defaultUserName;
        userPasswordIF.text = defaultUserPassword;
        MQurlIF.text = defaultMQurl;
        MQnameIF.text = defaultMQname;
    }
}