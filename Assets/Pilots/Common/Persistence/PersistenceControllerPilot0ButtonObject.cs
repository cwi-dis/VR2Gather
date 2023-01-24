using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRT.Pilots.Common
{
    public class PersistenceControllerPilot0ButtonObject : PersistenceControllerBase, IDataPersistence
    {
        private bool testInfoPrinted = false;
        public Grabbable grabbableSelfRef;
        public Transform TransformSelfRef;
        public Material MaterialSelfRef;
        //public PersistenceData pData;

        // Start is called before the first frame update
        void Start()
        {
            //pData = loadPersistenceData(grabbableSelfRef.NetworkId);
        }

        // Update is called once per frame
        void Update()
        {

        }
        void OnApplicationQuit()
        {

        }
        public void loadPersistenceData(PersistenceData pData)
        {
            Debug.Log("xxxshishir: Load persistence data called");
            gameObject.transform.position = pData.position;
            gameObject.transform.rotation = pData.rotation;
            grabbableSelfRef.SendSyncMessage();
            //xxxshishir ToDo: Add material change tracking later
        }
        public PersistenceData savePersistenceData()
        {
            Debug.Log("xxxshishir: Save persistence data called");
            PersistenceData pData = new PersistenceData();
            pData.NetworkID = grabbableSelfRef.NetworkId;
            pData.position = gameObject.transform.position;
            pData.rotation = gameObject.transform.rotation;
            //pData.material = MaterialSelfRef;
            pData.material = null;
            pData.MessageData = "";
            //xxxshishir we assume all pilot0button objects are already in the scene and cannot be dynamically created
            pData.isStatic = true;
            return pData;
        }
        public string getNetworkID()
        {
            return grabbableSelfRef.NetworkId;
        }
    }
}