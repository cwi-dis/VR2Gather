using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
namespace VRT.Pilots.Common
{
    public class FixActions : MonoBehaviour
    {
        [Tooltip("EventSystem that manages UI (default: find first one")]
        [SerializeField] private EventSystem eventSystem;
        [Tooltip("InputActionManager (default: from this GameObject")]
        [SerializeField] private InputActionManager inputActionManager;
        [Tooltip("ActionMaps to disable when a UI element is selected and enabled")]
        [SerializeField] private string[] actionMapNames;
        [Tooltip("There currently is an active UI selection")]
        [SerializeField][DisableEditing] private bool eventSystemHasSelection;

        List<InputActionMap> mapsToControl = new List<InputActionMap>();

        // Start is called before the first frame update
        void Start()
        {
            if (eventSystem == null)
            {
                eventSystem = FindObjectOfType<EventSystem>();
            }
            if (inputActionManager == null)
            {
                inputActionManager = GetComponent<InputActionManager>();
            }
            if (inputActionManager == null || inputActionManager == null)
            {
                Debug.LogWarning("FixActions: missing EventSystem or InputActionManager");
                return;
            }
            foreach (string actionMapToDisableWithSelection in actionMapNames)
            {
                foreach (InputActionAsset actionAsset in inputActionManager.actionAssets)
                {
                    InputActionMap actionMap = actionAsset.FindActionMap(actionMapToDisableWithSelection, false);
                    if (actionMap != null)
                    {
                        mapsToControl.Add(actionMap);
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (eventSystem == null) return;
            GameObject sel = eventSystem.currentSelectedGameObject;
            bool activeNow = sel != null && sel.activeInHierarchy;
            bool wasActive = eventSystemHasSelection;
            eventSystemHasSelection = activeNow;
            if (wasActive == activeNow) return;
       
            foreach(var actionMap in mapsToControl)
            {
                if (activeNow)
                {
                    actionMap.Disable();
                }
                else
                {
                    actionMap.Enable();
                }
            }
        }
    }

}

