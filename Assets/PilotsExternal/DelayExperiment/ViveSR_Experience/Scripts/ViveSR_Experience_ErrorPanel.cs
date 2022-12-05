using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{                         
    public class ViveSR_Experience_ErrorPanel : MonoBehaviour
    {
        [SerializeField] Text _text;

        public void EnablePanel(string text)
        {
            gameObject.SetActive(true);
            _text.text = text;
        }

        public void DisablePanel()
        {
            gameObject.SetActive(false);
        }
    }
}