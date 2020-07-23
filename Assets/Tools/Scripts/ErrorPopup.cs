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
        Application.RegisterLogCallback(HandleException);

        errorButton.onClick.AddListener(delegate { ErrorButton(); });        
    }

    void HandleException(string condition, string stackTrace, LogType type) {
        string msg = condition;
        if (type == LogType.Exception) {
            FillError("Exception", msg);
        }
        else if (type == LogType.Error) {
            FillError("Error", msg);
        }
    }

    private void FillError(string title, string message) {
        errorTitle.text = title;
        errorMessage.text = message;
        errorPanel.SetActive(true);
    }

    public void ErrorButton() {
        errorPanel.SetActive(false);
    }
}
