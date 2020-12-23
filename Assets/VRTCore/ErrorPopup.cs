using UnityEngine;
using UnityEngine.UI;

namespace VRTCore
{
    public class ErrorPopup : MonoBehaviour
    {

        [Header("ErrorHandling")]
        [SerializeField] private Text errorTitle = null;
        [SerializeField] private Text errorMessage = null;
        [SerializeField] private Button errorButton = null;

        public string ErrorMessage { get { return errorMessage.text; } }

        // Start is called before the first frame update
        void Start()
        {
            errorButton.onClick.AddListener(delegate { ErrorButton(); });
        }

        public void FillError(string title, string message)
        {
            errorTitle.text = title;
            errorMessage.text = message.Length < 4096 ? message : message.Substring(0, 4096);
        }

        public void ErrorButton()
        {
            Destroy(gameObject);
        }
    }
}