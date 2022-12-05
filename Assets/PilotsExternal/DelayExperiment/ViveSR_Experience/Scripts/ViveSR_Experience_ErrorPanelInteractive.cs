using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_ErrorPanelInteractive : ViveSR_Experience_ErrorPanel
    {
        public Button LeftButton, RightButton;
        public Text LeftButtonText, RightButtonText;

        [SerializeField] GameObject ControllerHint;

        private void Awake()
        {
            ControllerHint.transform.SetParent(ViveSR_Experience.instance.AttachPoint.transform, false);
            gameObject.transform.SetParent(ViveSR_Experience.instance.PlayerHeadCollision.transform, false);
        }

        private void OnEnable()
        {
            PlayerHandUILaserPointer.SetColors(Color.red, Color.white);
            PlayerHandUILaserPointer.EnableLaserPointer(true);
            ControllerHint.SetActive(true);
        }

        private void OnDisable()
        {
            PlayerHandUILaserPointer.ResetColors();
            PlayerHandUILaserPointer.EnableLaserPointer(false);
            ControllerHint.SetActive(false);
        }
    }
}