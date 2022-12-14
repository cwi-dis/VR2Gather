using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRT.Pilots.Common
{
    [RequireComponent(typeof(Animator))]
    public class Hand : MonoBehaviour
    {
        Animator animator;

        // Start is called before the first frame update
        void Start()
        {
            animator = GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        internal void SetGrab(bool isGrabbing)
        {
            if (animator.GetBool("IsGrabbing") != isGrabbing)
            {
                animator.SetBool("IsGrabbing", isGrabbing);
            }
        }

        internal void SetPoint(bool isPointing)
        {
            if (animator.GetBool("IsPointing") != isPointing)
            {
                animator.SetBool("IsPointing", isPointing);
            }
        }
    }

}
