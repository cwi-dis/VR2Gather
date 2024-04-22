using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleCharacterControllerSelf : SampleCharacterControllerBase
{
    protected Vector3 previousPosition;
    protected Vector3 previousRotation;

    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        previousPosition = gameObject.transform.position;
        previousRotation = gameObject.transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        if (previousPosition == gameObject.transform.position && previousRotation == gameObject.transform.rotation.eulerAngles)
        {
            return;
        }
        Vector3 rotateInNewFrame = gameObject.transform.rotation.eulerAngles - previousRotation;
        Vector3 moveInNewFrame = -gameObject.transform.InverseTransformPoint(previousPosition);
        CharacterMovement movement = new CharacterMovement()
        {
            deltaRotation = rotateInNewFrame,
            deltaPosition = moveInNewFrame
        };

        Debug.Log($"Send move: deltaPosition={movement.deltaPosition}, deltaRotation={movement.deltaRotation}");
        orchestrator.Send<CharacterMovement>(CharacterMovementCommand, movement);
        previousPosition = gameObject.transform.position;
        previousRotation = gameObject.transform.rotation.eulerAngles;
    }
}
