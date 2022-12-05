using UnityEngine;
using UnityEngine.Events;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_ErrorHandler : MonoBehaviour {

        public ViveSR_Experience_ErrorPanel ErrorPanel; 
        public ViveSR_Experience_ErrorPanelInteractive ErrorPanelInteractive;

        public void EnablePanel(string errorMessage)
        {
            ErrorPanel.EnablePanel(errorMessage);
        }

        public void EnablePanel(string errorMessage, string leftButtonText, UnityAction onLeftButtonClick, string rightButtonText, UnityAction onRightButtonClick)   
        {                                    
            ErrorPanelInteractive.EnablePanel(errorMessage);
            ErrorPanelInteractive.LeftButton.onClick.RemoveAllListeners();
            ErrorPanelInteractive.RightButton.onClick.RemoveAllListeners();
            ErrorPanelInteractive.LeftButtonText.text = leftButtonText;
            ErrorPanelInteractive.RightButtonText.text = rightButtonText;
            ErrorPanelInteractive.LeftButton.onClick.AddListener(onLeftButtonClick);
            ErrorPanelInteractive.RightButton.onClick.AddListener(onRightButtonClick);
        }

        public void DisableAllErrorPanels()
        {       
            ErrorPanelInteractive.DisablePanel();
            ErrorPanel.DisablePanel();
        }            
    }
}
