using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using VRT.Core;

namespace VRT.Pilots.Common
{
    public class HeadsUpDisplay : MonoBehaviour, ErrorManagerSink
    {
        [Tooltip("The Input System Action that will show/hide the HUD")]
        [SerializeField] InputActionProperty m_ShowHideAction;
        [Tooltip("The canvas containing the HUD")]
        [SerializeField] GameObject canvas;
        [Tooltip("Canvas element: user interface")]
        [SerializeField] GameObject UserInterfaceGO;
        [Tooltip("Button that toggles User Interface")]
        [SerializeField] Toggle UserInterfaceToggle;
        [Tooltip("Canvas element: error messages")]
        [SerializeField] GameObject MessagesGO;
        [Tooltip("Button that toggles error messages")]
        [SerializeField] Toggle MessagesToggle;
        [Tooltip("Canvas element: help")]
        [SerializeField] GameObject HelpGO;
        [Tooltip("Button that toggles error messages")]
        [SerializeField] Toggle HelpToggle;
        [Tooltip("How far away is the HMD from the users eyes?")]
        public float distance = 1;
        [Tooltip("How far up/down from the center is the HMD?")]
        public float height = 0;
        [Tooltip("How fast should it move when the user changes position/orientation?")]
        public float velocity = 0.01f;
        [Tooltip("How far away from its preferred position can it be before we start moving it?")]
        public float maxOffset = 1;
        [Tooltip("How far away from its preferred orientation can it be before we start moving it?")]
        public float maxAngle = 1;
        [Tooltip("Should this HUD intercept and display error messages?")]
        [SerializeField] bool interceptErrors = true;
        [Tooltip("Prefab for error messages")]
        [SerializeField] GameObject errorPrefab;

        [Tooltip("Player controller (found dynamically)")]
        [DisableEditing] [SerializeField] PlayerControllerSelf playerController;
        [Tooltip("Dialog needs to move")]
        [DisableEditing][SerializeField] bool shouldChange;

        // Start is called before the first frame update
        void Start()
        {
            Hide();
             if (playerController == null) playerController = GetComponentInParent<PlayerControllerSelf>();
            if (interceptErrors)
            {
                if (ErrorManager.Instance == null)
                {
                    Debug.LogError("HeadsUpDisplay: interceptErrors is true, but there is no ErrorManager");
                    return;
                }
                ErrorManager.Instance.RegisterSink(this);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (m_ShowHideAction.action.WasPressedThisFrame())
            {
                canvas.SetActive(!canvas.activeSelf);
                if (canvas.activeSelf)
                {
                    PilotController.Instance.DisableDirectInteraction();
                }
                else
                {
                    PilotController.Instance.EnableDirectInteraction();
                }
            }
        }

        void LateUpdate()
        {
            if (Camera.main == null)
            {
                // Self player has not initialized yet.
                return;
            }
            Vector3 forward = Camera.main.transform.forward;
            forward.y = height;
            forward = forward.normalized;

            Vector3 position = Camera.main.transform.position + forward * distance;
            Quaternion rotation = Quaternion.LookRotation(forward);
            if (Vector3.Distance(transform.position, position) > maxOffset ||
                Quaternion.Angle(transform.rotation, rotation) > maxAngle)
            {
                shouldChange = true;
            }
            if (shouldChange)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, velocity);
                transform.position = Vector3.Lerp(transform.position, position, velocity);
                //transform.localScale = Vector3.Lerp(transform.localScale, scale, scaleVel);
                if (Vector3.Distance(transform.position, position) < maxOffset / 10 &&
                   Quaternion.Angle(transform.rotation, rotation) < maxAngle / 10)
                {
                    shouldChange = false;
                }
             }

        }

        public void FillError(string title, string message)
        {
            var popupGO = Instantiate(errorPrefab, MessagesGO.transform);
            popupGO.SetActive(true);
            ErrorPopup errorPopup = popupGO.GetComponent<ErrorPopup>();
            errorPopup.FillError(title, message);
            ShowMessages();
        }

        public void Hide()
        {
            PilotController.Instance.EnableDirectInteraction();
            canvas.SetActive(false);
            UserInterfaceGO.SetActive(true);
            MessagesGO.SetActive(false);
            HelpGO.SetActive(false);
            UserInterfaceToggle.isOn = true;
            MessagesToggle.isOn = false;
            HelpToggle.isOn = false;
        }

        public void ToggleChanged()
        {
            if (UserInterfaceToggle.isOn)
            {
                ShowCommands();
            }
            else if (MessagesToggle.isOn)
            {
                ShowMessages();
            }
            else
            {
                ShowHelp();
            }
        }

        public void ShowCommands()
        {
            UserInterfaceGO.SetActive(true);
            MessagesGO.SetActive(false);
            HelpGO.SetActive(false);
            UserInterfaceToggle.isOn = true;
            MessagesToggle.isOn = false;
            HelpToggle.isOn = false;
            canvas.SetActive(true);
            PilotController.Instance.DisableDirectInteraction();
        }

        public void ShowMessages()
        {
            UserInterfaceGO.SetActive(false);
            MessagesGO.SetActive(true);
            HelpGO.SetActive(false);
            UserInterfaceToggle.isOn = false;
            MessagesToggle.isOn = true;
            HelpToggle.isOn = false;
            canvas.SetActive(true);
            PilotController.Instance.DisableDirectInteraction();
        }

        public void ShowHelp()
        {
            UserInterfaceGO.SetActive(false);
            MessagesGO.SetActive(false);
            HelpGO.SetActive(true);
            UserInterfaceToggle.isOn = false;
            MessagesToggle.isOn = false;
            HelpToggle.isOn = true;
            canvas.SetActive(true);
            PilotController.Instance.DisableDirectInteraction();
        }

        public void OnHUDCommand(string command)
        {
            Debug.Log($"HeadsUpDisplay: OnHUDCommand({command})");
            bool ok = false;
            if (playerController != null)
            {
                ok = playerController.OnUserCommand(command);
            }
            if (!ok)
            {
                ok = PilotController.Instance.OnUserCommand(command);
            }
            if (!ok)
            {
                Debug.LogError($"HeadsUpDisplay: unknown command \"{command}\"");
            }
        }
    }

}
