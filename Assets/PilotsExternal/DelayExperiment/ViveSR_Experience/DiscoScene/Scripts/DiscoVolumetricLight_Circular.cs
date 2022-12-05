using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Music2Dance1980
{
    public class DiscoVolumetricLight_Circular : DiscoVolumetricLight
    {
        [SerializeField] Transform circularOffset;
        [SerializeField] protected float frequency, angle;

        private void Update()
        {
            transForAnimation.localRotation = Quaternion.Euler(new Vector3(Time.timeSinceLevelLoad * frequency , 1, 0));
            circularOffset.localRotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
    }
}