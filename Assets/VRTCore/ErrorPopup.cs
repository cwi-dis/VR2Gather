using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRT.Core
{
    public class ErrorPopup : MonoBehaviour
    {

        [Header("ErrorHandling")]
        [SerializeField] private TextMeshProUGUI errorTitle = null;
        [SerializeField] private TextMeshProUGUI errorMessage = null;
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