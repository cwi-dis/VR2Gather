using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using UnityEditor;

public class FishNetPingRPCTest : NetworkBehaviour
{
    [SerializeField]private AudioSource m_AudioSource;

    string Name() {
        return "FishNetPingRPCTest";
    }

    void Awake() {
        Debug.Log($"{Name()}: Awake");
    }
    void Start() {
        Debug.Log($"{Name()}: Start");
    }
    void OnEnable() {
        Debug.Log($"{Name()}: OnEnable");
    }

    void OnDisable() {
        Debug.Log($"{Name()}: OnDisable");
    }

    public void LocalEventFire()
    {
        Debug.Log($"{Name()}: Firing Event");

        ServerRPCPlaySound();
    }


    [ServerRpc(RequireOwnership = false)]
    public void ServerRPCPlaySound()
    {
        Debug.Log($"{Name()}: ServerRPCPlaySound: called");

        ObserversRPCPlaySound();
        
    }

    [ObserversRpc]
    public void ObserversRPCPlaySound()
    {
        Debug.Log($"{Name()}: ObserversRPCPlaySound: called");
        m_AudioSource.Play();

    }
}
