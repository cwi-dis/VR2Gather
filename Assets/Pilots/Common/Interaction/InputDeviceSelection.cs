using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputDeviceSelection : MonoBehaviour
{
    [System.Serializable]
    public class ControlSchemeNameToObject
    {
        public string name;
        public GameObject implementation;
        public GameObject documentation;
    }

    [Tooltip("Mapping of input scheme names to implementation objects")]
    public ControlSchemeNameToObject[] schemes;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnDisable()
    {
        //
        // Workaround for a bug seen in October 2022:
        // https://forum.unity.com/threads/type-of-instance-in-array-does-not-match-expected-type.1320564/
        //
        var pi = GetComponent<PlayerInput>();
        pi.actions = null;
    }

    public void OnControlsChanged(PlayerInput pi)
    {
        Debug.Log($"InputDeviceSelection: OnControlsChanged({pi.name}): enabled={pi.enabled}, inputIsActive={pi.inputIsActive}, actionMap={pi.currentActionMap.name}, controlScheme={pi.currentControlScheme}");
        GameObject wanted = null;
        if (schemes == null)
        {
            Debug.LogError("InputDeviceSelection: no schemes specified on GameObject");
            return;
        }
        foreach(var so in schemes)
        {
            if (so.name == pi.currentControlScheme) wanted = so.implementation;
        }
        if (wanted == null)
        {
            Debug.LogError($"InputDeviceSelection: no implementation object for control scheme {pi.currentControlScheme}");
            return;
        }
        foreach(var so in schemes)
        {
            bool thisOneWanted = so.implementation == wanted;
            so.implementation.SetActive(thisOneWanted);
            if (so.documentation != null) so.documentation.SetActive(thisOneWanted);
        }
    }
  
    // Update is called once per frame
    void Update()
    {
        
    }
}
