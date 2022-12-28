using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRT.Pilots.Common
{
    /// <summary>
    /// Component that controls the appearance of a hand (pointing, grabbing or neutral).
    /// </summary>
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

        /// <summary>
        /// Call to change the grabbing state.
        /// </summary>
        /// <param name="isGrabbing"></param>
        internal void SetGrab(bool isGrabbing)
        {
            if (animator.GetBool("IsGrabbing") != isGrabbing)
            {
                animator.SetBool("IsGrabbing", isGrabbing);
            }
        }

        /// <summary>
        /// Call to set the pointing state.
        /// </summary>
        /// <param name="isPointing"></param>
        internal void SetPoint(bool isPointing)
        {
            if (animator.GetBool("IsPointing") != isPointing)
            {
                animator.SetBool("IsPointing", isPointing);
            }
        }
    }

}
