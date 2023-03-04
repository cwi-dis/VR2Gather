using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRT.Pilots.Common;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Add this component to an object with a Collider with trigger set.
    /// Put it in layer PlayerDetector.
    /// When a user (actually: a non-trigger collider on layer PlayerCollider)
    /// enters or leaves the area it will fire the corresponding events.
    /// 
    /// </summary>
    public class VRTSensorArea : MonoBehaviour
    {
        [Tooltip("These events are called when the player enters the area")]
        public UnityEvent areaEntered;
        [Tooltip("These events are called when the player leaves the area")]
        public UnityEvent areaLeft;
        [Tooltip("If set: find the player and call its areaEnteredMessage and areaLeftMessage")]
        public bool messagesToPlayer = false;
        [Tooltip("Name of the message to send. The messageParameter string is passed.")]
        public string areaEnteredMessage;
        [Tooltip("Name of the message to send. The messageParameter string is passed.")]
        public string areaLeftMessage;
        [Tooltip("The parameter to the messages")]
        public string messageParameter;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter(Collider other)
        {
            string layer = LayerMask.LayerToName(other.gameObject.layer);
            if (layer != "PlayerCollider") return;
            Debug.Log($"{name}: {other.name} entered the area");
            areaEntered.Invoke();
            if (messagesToPlayer && areaEnteredMessage != null && areaEnteredMessage != "")
            {
                PlayerControllerSelf playerController = FindObjectOfType<PlayerControllerSelf>();
                if (playerController == null)
                {
                    Debug.LogError("SensorArea: cannot find PlayerControllerSelf");
                    return;
                }
                GameObject playerGO = playerController.gameObject;
                playerGO.SendMessage(areaEnteredMessage, messageParameter);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            string layer = LayerMask.LayerToName(other.gameObject.layer);
            if (layer != "PlayerCollider") return;
            Debug.Log($"{name}: {other.name} left the area");
            areaLeft.Invoke();
            if (messagesToPlayer && areaLeftMessage != null && areaLeftMessage != "")
            {
                PlayerControllerSelf playerController = FindObjectOfType<PlayerControllerSelf>();
                if (playerController == null)
                {
                    Debug.LogError("SensorArea: cannot find PlayerControllerSelf");
                    return;
                }
                GameObject playerGO = playerController.gameObject;
                playerGO.SendMessage(areaLeftMessage, messageParameter);
            }
        }
    }
}
