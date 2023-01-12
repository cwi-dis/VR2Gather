using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Pilots.Common
{
    public abstract class PersistenceControllerBase : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

    }
    //xxxshishir Interface for persistence functions - The persistence manager is meant to find this 
    public interface IDataPersistence
    {
        PersistenceData loadPersistenceData(string NetworkID);
        void savePersistenceData(PersistenceData pData, string NetworkID);
        //PersistenceControllerBase objectData
    }
    [System.Serializable]
    public struct PersistenceData
    {
        public string NetworkID;
        public Vector3 position;
        public Quaternion rotation;
        public Material material;
        public string MessageData;
    }
}