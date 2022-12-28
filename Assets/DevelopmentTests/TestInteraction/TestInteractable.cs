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
        Debug.Log($"{Name()}: OnActivate");
    }
    public void OnDeactivate()
    {
        Debug.Log($"{Name()}: OnDeactivate");
    }
    public void OnHoverEnter()
    {
        Debug.Log($"{Name()}: OnHoverEnter");
    }
    public void OnHoverExit()
    {
        Debug.Log($"{Name()}: OnHoverExit");
    }
    public void OnSelectEnter()
    {
        Debug.Log($"{Name()}: OnSelectEnter");
    }
    public void OnSelectExit()
    {
        Debug.Log($"{Name()}: OnSelectExit");
    }
}
