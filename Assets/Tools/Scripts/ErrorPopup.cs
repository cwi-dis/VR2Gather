using UnityEngine;
using UnityEngine.UI;

public class ErrorPopup : MonoBehaviour {

    [Header("ErrorHandling")]
    [SerializeField] private GameObject errorPanel = null;
    [SerializeField] private Text errorTitle = null;
    [SerializeField] private Text errorMessage = null;
    [SerializeField] private Button errorButton = null;

    // Start is called before the first frame update
    void Start() {
// Obsolete
//        Application.RegisterLogCallback(HandleException);
        Application.logMessageReceived += Application_logMessageReceived;

        errorButton.onClick.AddListener(delegate { ErrorButton(); });        
    }

    private void Application_logMessageReceived(string condition, string stackTrace, LogType type) {
        string msg = condition;
        if (type == LogType.Exception) {
            FillError("Exception", msg);
        } else if (type == LogType.Error) {
            FillError("Error", msg);
        }
    }
    /*
    // Obsolete
    void HandleException(string condition, string stackTrace, LogType type) {
        string msg = condition;
        if (type == LogType.Exception) {
            FillError("Exception", msg);
        }
        else if (type == LogType.Error) {
            FillError("Error", msg);
        }
    }
    */
    private void FillError(string title, string message) {
        errorTitle.text = title;
        errorMessage.text = message.Length< 4096 ? message: message.Substring( 0, 4096);
        errorPanel.SetActive(true);
    }

    public void ErrorButton() {
        errorPanel.SetActive(false);
    }
}
