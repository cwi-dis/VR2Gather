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
    }

    [Tooltip("Mapping of input scheme names to implementation objects")]
    public ControlSchemeNameToObject[] schemes;

    [Header("Introspection (for debugging)")]
    [Tooltip("Current control scheme")]
    public string currentControlScheme;

    // Start is called before the first frame update
    void Start()
    {
        PlayerInput pi = GetComponentInParent<PlayerInput>();
        OnControlsChanged(pi);
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
        currentControlScheme = pi.currentControlScheme;
        if (currentControlScheme == null || currentControlScheme == "")
        {
            Debug.Log("InputDeviceSelection: empty scheme");
            return;
        }
        GameObject wanted = null;
        if (schemes == null)
        {
            Debug.LogError("InputDeviceSelection: no schemes specified on GameObject");
            return;
        }
        foreach(var so in schemes)
        {
            if (so.name == currentControlScheme) wanted = so.implementation;
        }
        if (wanted == null)
        {
            Debug.LogError($"InputDeviceSelection: no implementation object for control scheme \"{currentControlScheme}\"");
            return;
        }
        foreach(var so in schemes)
        {
            bool thisOneWanted = so.implementation == wanted;
            so.implementation.SetActive(thisOneWanted);
        }
    }
  
    // Update is called once per frame
    void Update()
    {
        
    }
}
