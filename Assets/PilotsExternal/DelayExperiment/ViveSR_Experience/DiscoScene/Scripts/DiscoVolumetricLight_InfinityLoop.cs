using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Music2Dance1980
{
    public class DiscoVolumetricLight_InfinityLoop : DiscoVolumetricLight
    {
        [SerializeField] protected float frequency_x, frequency_y, magnitude;

        private void Update()
        {                                                                
             transForAnimation.localRotation = Quaternion.Euler(new Vector3(Mathf.Cos(Time.timeSinceLevelLoad * frequency_x), Mathf.Sin(Time.timeSinceLevelLoad * frequency_y), 0) * magnitude);
        }
    }
}