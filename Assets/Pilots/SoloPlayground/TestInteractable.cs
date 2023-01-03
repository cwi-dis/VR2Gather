using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TestInteractable : MonoBehaviour
{

    string Name()
    {
        return "TestInteractable";
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnActivate()
    {
        Debug.Log($"{Name()}: {name}: OnActivate");
    }
    public void OnDeactivate()
    {
        Debug.Log($"{Name()}: {name}: OnDeactivate");
    }
    public void OnHoverEnter()
    {
        Debug.Log($"{Name()}: {name}: OnHoverEnter");
    }
    public void OnHoverExit()
    {
        Debug.Log($"{Name()}: {name}: OnHoverExit");
    }
    public void OnSelectEnter()
    {
        Debug.Log($"{Name()}: {name}: OnSelectEnter");
    }
    public void OnSelectExit()
    {
        Debug.Log($"{Name()}: {name}: OnSelectExit");
    }
}
