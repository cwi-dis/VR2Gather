using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
    using HandState = HandDirectAppearance.HandState;

    public class HandNetworkControllerSelf : HandNetworkControllerBase
    {
        protected ActionBasedController controller;
        HandState oldState = HandState.Idle;

        public override VRTGrabbableController HeldGrabbable
        {
            set {
                if (value == m_HeldGrabbable) return;
                if (m_HeldGrabbable != null)
                {
                    // Release any held object
                    HandGrabEvent handGrabEvent = new HandGrabEvent()
                    {
                        GrabbableObjectId = m_HeldGrabbable.NetworkId,
                        UserId = _Player?.UserId,
                        Handedness = handHandedness,
                        EventType = HandInteractionEventType.Release
                    };
                    ExecuteHandGrabEvent(handGrabEvent);
                }
                m_HeldGrabbable = value;
                if (m_HeldGrabbable != null)
                {
                    // Grab new object
                    HandGrabEvent handGrabEvent = new HandGrabEvent()
                    {
                        GrabbableObjectId = m_HeldGrabbable.NetworkId,
                        UserId = _Player?.UserId,
                        Handedness = handHandedness,
                        EventType = HandInteractionEventType.Grab
                    };
                    ExecuteHandGrabEvent(handGrabEvent);
                }
            }
            get => m_HeldGrabbable;
        }

        protected override void Start()
        {
            base.Start();
            controller = GetComponent<ActionBasedController>();
        }


        private void Update()
        {
            if (oldState != handAppearance.state)
            {
                oldState = handAppearance.state;
                // Inform other participants of the change in our hand state
                var data = new HandControllerData
                {
                    handHandedness = handHandedness,
                    handState = handAppearance.state
                };

                if (OrchestratorController.Instance.UserIsMaster)
                {
                    OrchestratorController.Instance.SendTypeEventToAll(data);
                }
                else
                {
                    OrchestratorController.Instance.SendTypeEventToMaster(data);
                }
            }
        }
    }
}
