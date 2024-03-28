using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// All data saved for persistent objects.
    /// </summary>
    [System.Serializable]
    public struct PersistentData
    {
        /// <summary>
        /// Unique identifier of the object for which the data is saved.
        /// </summary>
        public string NetworkID;
        /// <summary>
        /// For dynamically created objects: the NetworkID of the creator.
        /// </summary>
        public string CreatorID;
        /// <summary>
        /// World position of the object.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// Orientation of the object.
        /// </summary>
        public Quaternion rotation;
        /// <summary>
        /// Extra data to be saved for the object.
        /// </summary>
        public string MessageData;
    }

    /// <summary>
    /// Interface to be supplied by any object whose state needs to be persistent.
    /// </summary>
    public interface IDataPersistence
    {
        /// <summary>
        /// Supplies loaded persistent data to the object.
        /// </summary>
        /// <param name="pData">Data to load</param>
        void loadPersistentData(PersistentData pData);
        /// <summary>
        /// Returns persistent data for saving.
        /// </summary>
        /// <returns>Data to save</returns>
        PersistentData getPersistentDataForSaving();
        /// <summary>
        /// Returns object identity, i.e. the key under which the getPersistentData will be saved.
        /// </summary>
        /// <returns>Network ID</returns>
        string getNetworkID();
   }

    public class PersistenceController : MonoBehaviour, IDataPersistence
    {
        [Tooltip("Grabbable to persist. Default: on this GameObject.")]
        public VRTGrabbableController grabbableRef;
        [Tooltip("Transform to persist. Default: on this GameObject.")]
        public Transform transformRef;
        
        void Awake()
        {
            if (grabbableRef == null)
            {
                grabbableRef = GetComponent<VRTGrabbableController>();
            }
            if (transformRef == null)
            {
                transformRef = transform;
            }
        }

        /// <summary>
        /// Load persistent data into this object and synchronize instances of this
        /// object in other experience instances. If overriding this method in subclasses call
        /// the base method last.
        /// </summary>
        /// <param name="pData"></param>
        virtual public void loadPersistentData(PersistentData pData)
        {
            Debug.Log($"{name}: Load persistence data called");
            if (pData.NetworkID != grabbableRef.NetworkId)
            {
                Debug.LogWarning($"{name}: loading data for {pData.NetworkID} but grabbableSelf is {grabbableRef.NetworkId}");
            }
            transformRef.position = pData.position;
            transformRef.rotation = pData.rotation;
            grabbableRef.SendRigidbodySyncMessage();
         }

        /// <summary>
        /// Get persistent data for this object. If overriding in subclass
        /// call the base method first.
        /// </summary>
        /// <returns></returns>
        virtual public PersistentData getPersistentDataForSaving()
        {
            Debug.Log($"{name}: Save persistence data called");
            PersistentData pData = new PersistentData();
            pData.NetworkID = grabbableRef.NetworkId;
            pData.position = gameObject.transform.position;
            pData.rotation = gameObject.transform.rotation;
            return pData;
        }

        public string getNetworkID()
        {
            return grabbableRef.NetworkId;
        }
    }
    
   
}