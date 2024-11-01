using UnityEngine;
using FishNet.Object;

namespace VRT.Fishnet {
    public class VRTFishnetTrigger : NetworkBehaviour
    {
        [Tooltip("Audio source to play")]
        [SerializeField] private AudioSource m_AudioSource;
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

        public void LocalEventFire()
        {
            if (debug) Debug.Log($"{Name()}: Firing Event");

            ServerRPCPlaySound();
        }


        [ServerRpc(RequireOwnership = false)]
        public void ServerRPCPlaySound()
        {
            if (debug) Debug.Log($"{Name()}: ServerRPCPlaySound: called");

            ObserversRPCPlaySound();
            
        }

        [ObserversRpc]
        public void ObserversRPCPlaySound()
        {
            if (debug) Debug.Log($"{Name()}: ObserversRPCPlaySound: called");
            m_AudioSource.Play();

        }
    }
}