using FishNet;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishNetSpawn : NetworkBehaviour
{
    [SerializeField]
    private GameObject _prefab;

    private GameObject spawnedObject;

    [SerializeField]
    private Transform _spawnLocation;

    [ServerRpc(RequireOwnership = false)]
    public void OnSpawnTrigger()
    {
        Debug.Log($"xxxDavid: Calling OnSpawnTrigger to spawn my prefab");
        GameObject go = Instantiate(_prefab, _spawnLocation.position, Quaternion.identity);
        ServerManager.Spawn(go);
        SetSpawnedObject(go, this);
    }

    [ObserversRpc]
    public void SetSpawnedObject(GameObject spawnedObject, FishNetSpawn script)
    {
        script.spawnedObject = spawnedObject;
    }
}
