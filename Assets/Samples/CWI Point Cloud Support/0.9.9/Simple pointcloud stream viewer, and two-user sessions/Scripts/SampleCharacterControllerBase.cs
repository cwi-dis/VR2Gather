using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleCharacterControllerBase : MonoBehaviour
{
    [Tooltip("Orchestration object.")]
    [SerializeField] protected SampleOrchestration orchestrator;

    /// <summary>
    /// Describes movement of a character, relative to its own coordinate system.
    /// Rotation is applied first, then movement in the new local reference frame.
    /// </summary>
    [Serializable]
    protected class CharacterMovement
    {
        public Vector3 deltaRotation;
        public Vector3 deltaPosition;
    }

    protected const string CharacterMovementCommand = "Move";

    // Start is called before the first frame update
    virtual protected void Awake()
    {
           // Initialize orchestrator?
    }

}
