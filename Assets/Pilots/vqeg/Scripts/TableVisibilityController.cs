using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableVisibilityController : MonoBehaviour
{
    public GameObject player1; // Assign Player 1 GameObject in the inspector
    public GameObject player2; // Assign Player 2 GameObject in the inspector
    private Renderer[] renderers;

    void Start()
    {
        // Get all renderer components in this GameObject and its children
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        // Update visibility based on whether the current player is Player 2
        UpdateVisibility(IsPlayer2());
    }

    bool IsPlayer2()
    {
        
        return false;
    }

    void UpdateVisibility(bool visible)
    {
        // Set the visibility for each renderer
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }
}