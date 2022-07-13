using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
    public class Splash : MonoBehaviour
    {
        public float distance;
        public float velocity;
        public float scaleVel;
        Vector3 scale;

        private void Awake()
        {
            //scale = transform.localScale;
            //transform.localScale = new Vector3(scale.x / 2, scale.y / 2, scale.z / 2);
        }

        // Update is called once per frame
        void LateUpdate()
        {
            Vector3 forward = VRConfig.Instance.getMainCameraTransform().forward;
            forward.y = 0;
            forward = forward.normalized;

            Vector3 position = VRConfig.Instance.getMainCameraTransform().position + forward * distance;
            Quaternion rotation = Quaternion.LookRotation(forward);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, velocity);
            transform.position = Vector3.Lerp(transform.position, position, velocity);
            //transform.localScale = Vector3.Lerp(transform.localScale, scale, scaleVel);
        }
    }
}