using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Music2Dance1980
{
    public class DiscoBall : MonoBehaviour
    {
        [SerializeField] float magnitude;

        // Update is called once per frame
        void Update()
        {
            transform.rotation = Quaternion.Euler(new Vector3(0, Time.timeSinceLevelLoad * magnitude, 0));
        }
    }
}