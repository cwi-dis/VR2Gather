using UnityEngine;
using UnityEngine.Events;
using FishNet.Object;
using System.Collections.Generic;

namespace VRT.Fishnet {
    public class VRTFishnetTrigger : NetworkBehaviour
    {
        [Tooltip("The events this component will forward")]
        public List<UnityEvent> Events;

        [Tooltip("Introspection: enable for debug output")]
        [SerializeField] private bool debug = false;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        void Awake() {
            if (debug) Debug.Log($"{Name()}: Awake");
        }
        void Start() {
            if (debug) Debug.Log($"{Name()}: Start");
        }
        void OnEnable() {
            if (debug) Debug.Log($"{Name()}: OnEnable");
        }

        void OnDisable() {
            if (debug) Debug.Log($"{Name()}: OnDisable");
        }

        public void LocalEventTrigger(int index)
        {
            if (debug) Debug.Log($"{Name()}: LocalEventTrigger({index}) called");
            if (Events == null || index >= Events.Count) {
                Debug.LogWarning($"{Name()}: LocalEventTrigger: index={index} but no such Event");
            }
            ServerEventTrigger(index);
        }


        [ServerRpc(RequireOwnership = false)]
        public void ServerEventTrigger(int index)
        {
            if (debug) Debug.Log($"{Name()}: ServerEventTrigger({index}) called");

            ObserverEventTrigger(index);
            
        }

        [ObserversRpc]
        public void ObserverEventTrigger(int index)
        {
            if (debug) Debug.Log($"{Name()}: ObserverEventTrigger({index}) called");
            Events[index].Invoke();
        }
    }
}