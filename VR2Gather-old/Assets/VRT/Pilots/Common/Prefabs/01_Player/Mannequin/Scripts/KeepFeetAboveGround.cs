using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepFeetAboveGround : MonoBehaviour
{
    [Tooltip("Foot may not go below the origin of this object")]
    public GameObject RootObject;
    [Tooltip("Print logging message if position adjusted")]
    [SerializeField] bool debug = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float height = transform.position.y - RootObject.transform.position.y;
        if (height < 0)
        {
            if (debug) Debug.Log($"KeepFeetAboveGround {name}: {Time.frameCount}: height={height}, fixing.");
            transform.Translate(0, -height, 0, Space.World);
        } else
        {
            //transform.Translate(0, 0, 0);
        }
    }
}
