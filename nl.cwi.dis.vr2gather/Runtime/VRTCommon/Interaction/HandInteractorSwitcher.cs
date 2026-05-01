using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace VRT.Pilots.Common
{

    public class HandInteractorSwitcher : MonoBehaviour
    {
        [Tooltip("The Input System Action that toggles the interactors")]
        [SerializeField] InputActionProperty m_toggleAction;
        
        [Tooltip("GameObjects to enable")]
        [SerializeField] List<GameObject> m_groupOne;
        [Tooltip("GameObjects to disable")]
        [SerializeField] List<GameObject> m_groupTwo;
        
        [Tooltip("Introspection: currently enabled")]
        [SerializeField] bool m_groupOneEnabled = false;

        void OnEnable()
        {
            m_toggleAction.action.performed += togglePerformed;
        }

        void OnDisable()
        {
            m_toggleAction.action.performed -= togglePerformed;
        }

        void togglePerformed(InputAction.CallbackContext ctx)
        {
            Debug.Log($"HandInteractionSwitcher: togglePerformed({ctx.ToString()}");
            toggleGameObjects(!m_groupOneEnabled);
        }

        void toggleGameObjects(bool groupOne)
        {
            foreach (GameObject go in m_groupOne)
            {
                go.SetActive(groupOne);
            }
            foreach (GameObject go in m_groupTwo)
            {
                go.SetActive(!groupOne);
            }
            m_groupOneEnabled = groupOne;
        }
        
    }
}