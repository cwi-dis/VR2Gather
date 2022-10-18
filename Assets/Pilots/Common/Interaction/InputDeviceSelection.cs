using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputDeviceSelection : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnControlsChanged(PlayerInput pi)
    {
        Debug.Log($"InputDeviceSelection: OnControlsChanged({pi.name}): enabled={pi.enabled}, inputIsActive={pi.inputIsActive}, actionMap={pi.currentActionMap.name}, controlScheme={pi.currentControlScheme}");
     }
  
    // Update is called once per frame
    void Update()
    {
        
    }
}
