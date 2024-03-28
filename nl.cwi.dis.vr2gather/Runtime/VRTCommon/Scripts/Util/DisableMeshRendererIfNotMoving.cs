using UnityEngine;

namespace VRT.Pilots.Common
{

    public class DisableMeshRendererIfNotMoving : MonoBehaviour
    {
        public Transform ReferenceTransform;
        public Renderer RendererToDisable;
        public float TimeOut = 1.0f;

        private float _LastMoveTime;

        private Vector3 _PreviousPosition;
        private Quaternion _PreviousRotation;
        private Vector3 _PreviousScale;

        private void Awake()
        {
            _LastMoveTime = Time.realtimeSinceStartup;

            //use local positions, because otherwise swapping to a new location or moving the
            //root transform would also cause the hands to be displayed again
            _PreviousPosition = transform.localPosition;
            _PreviousRotation = transform.localRotation;
            _PreviousScale = transform.localScale;
        }

        void Update()
        {
            //Note: Using ReferenceTransform.hasChanged here won't work
            //That actually does no check if the values have changed, so anything setting
            //the transform to the same values as before would still cause hasChanged to be true
            if (HasChanged())
            {
                RendererToDisable.enabled = true;
                _LastMoveTime = Time.realtimeSinceStartup;
            }
            else if (RendererToDisable.enabled)
            {
                if (Time.realtimeSinceStartup - _LastMoveTime > TimeOut)
                {
                    RendererToDisable.enabled = false;
                }
            }
        }

        private bool HasChanged()
        {
            bool hasChanged = false;

            if (transform.localPosition != _PreviousPosition ||
                transform.localRotation != _PreviousRotation ||
                transform.localScale != _PreviousScale)
            {
                hasChanged = true;
                _PreviousPosition = transform.localPosition;
                _PreviousRotation = transform.localRotation;
                _PreviousScale = transform.localScale;
            }

            return hasChanged;
        }
    }
}
