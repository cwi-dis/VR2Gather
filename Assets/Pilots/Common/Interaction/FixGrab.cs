using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Fix direct grab being "sticky" with the previous object being grabbed again even if
    /// not touched when you use the grab action again.
    /// </summary>
    public class FixGrab : MonoBehaviour
    {
        [Tooltip("Print logging messages")]
        [SerializeField] bool debug = false;

        HashSet<Collider> currentlyEntered = new HashSet<Collider>();
        // Start is called before the first frame update
        void Start()
        {

        }

        private void OnTriggerEnter(Collider other)
        {
            if (debug) Debug.Log($"FixGrab: OnTriggerEnter({other})");
            currentlyEntered.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (debug) Debug.Log($"FixGrab: OnTriggerExit({other})");
            currentlyEntered.Remove(other);
        }

        public void AboutToDisable()
        {
            if (currentlyEntered.Count == 0) return;
            if (debug) Debug.Log($"FixGrab: Disabled, {currentlyEntered.Count} colliders not exited");
            HashSet<Collider> tmp = new HashSet<Collider>(currentlyEntered);
            foreach(var c in tmp)
            {
                SendMessage("OnTriggerExit", c, SendMessageOptions.DontRequireReceiver);
            }
            if (debug) Debug.Log($"FixGrab: Disabled, now {currentlyEntered.Count} colliders not exited");
       }
        // Update is called once per frame
        void Update()
        {

        }
    }

}
