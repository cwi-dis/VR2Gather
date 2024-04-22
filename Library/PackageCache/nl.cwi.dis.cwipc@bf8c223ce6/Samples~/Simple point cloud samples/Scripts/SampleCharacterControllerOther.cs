using System;
using UnityEngine;

public class SampleCharacterControllerOther : SampleCharacterControllerBase
{
    CharacterMovement moveReceived;

    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();
        orchestrator.RegisterCallback<CharacterMovement>(CharacterMovementCommand, MoveCallback);  
    }

    // Update is called once per frame
    void Update()
    {
        if (moveReceived == null) return;
        Debug.Log($"Recv move: deltaPosition={moveReceived.deltaPosition}, deltaRotation={moveReceived.deltaRotation}");
        gameObject.transform.Rotate(moveReceived.deltaRotation);
        gameObject.transform.Translate(moveReceived.deltaPosition);
        moveReceived = null;
    }

    void MoveCallback(CharacterMovement _moveReceived)
    {
        if (moveReceived != null)
        {
            Debug.LogWarning("SampleCharacterControllerOther: received move while previous one not processed yet.");
        }
        moveReceived = _moveReceived;
    }
}
