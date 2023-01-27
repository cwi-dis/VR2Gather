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
    /// <summary>
    /// Interface to be supplied by any object whose state needs to be persistent.
    /// </summary>
    public interface IDataPersistence
    {
        /// <summary>
        /// Supplies loaded persistent data to the object.
        /// </summary>
        /// <param name="pData"></param>
        void loadPersistentData(PersistentData pData);
        /// <summary>
        /// Returns persistent data for saving.
        /// </summary>
        /// <returns></returns>
        PersistentData getPersistentDataForSaving();
        /// <summary>
        /// Returns object identity, i.e. the key under which the getPersistentData will be saved.
        /// </summary>
        /// <returns></returns>
        string getNetworkID();
        //PersistenceControllerBase objectData
    }
    [System.Serializable]
    public struct PersistentData
    {
        public string NetworkID;
        public Vector3 position;
        public Quaternion rotation;
        public Material material;
        public string MessageData;
        public bool isStatic;
    }
}