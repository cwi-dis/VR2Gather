using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRT.Pilots.Common
{
    /// <summary>
    /// Component that controls the appearance of a hand (pointing, grabbing or neutral).
    /// Animations between states are automatically executed.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HandDirectAppearance : MonoBehaviour
    {
        /// <summary>
        /// Various states the hand can be in.
        /// </summary>
        public enum HandState
        {
            Idle,
            Pointing,
            Grabbing,
            Teleporting
        }

        [Tooltip("Current state")]
        [SerializeField] HandState m_state = HandState.Idle;
        /// <summary>
        /// Current state of the hand. Changing this will execute the animation.
        /// </summary>
        public HandState state
        {
            get => m_state;
            set
            {
                if (m_state != value)
                {
                    m_state = value;
                    SetGrab(m_state == HandState.Grabbing);
                    SetPoint(m_state == HandState.Pointing || m_state == HandState.Teleporting);
                }
            }
        }

        Animator animator;

        // Start is called before the first frame update
        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {

        }

       void SetGrab(bool isGrabbing)
        {
            if (animator.GetBool("IsGrabbing") != isGrabbing)
            {
                animator.SetBool("IsGrabbing", isGrabbing);
            }
        }

        void SetPoint(bool isPointing)
        {
            if (animator.GetBool("IsPointing") != isPointing)
            {
                animator.SetBool("IsPointing", isPointing);
            }
        }
    }

}
