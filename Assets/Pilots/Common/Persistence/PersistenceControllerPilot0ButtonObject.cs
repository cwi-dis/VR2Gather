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
        public PersistenceData pData;

        // Start is called before the first frame update
        void Start()
        {
            pData = loadPersistenceData(grabbableSelfRef.NetworkId);
        }

        // Update is called once per frame
        void Update()
        {
            if (testInfoPrinted == false)
            {
                string NID = "0000";
                NID = grabbableSelfRef.NetworkId;
                Debug.Log("xxxshishir: Printing test message from persistable game object with ID: " + NID);
                testInfoPrinted = true;
            }


        }
        void OnApplicationQuit()
        {
            TransformSelfRef = gameObject.transform;
            MaterialSelfRef = gameObject.GetComponent<Renderer>().material;
            pData.NetworkID = grabbableSelfRef.NetworkId;
            pData.position = TransformSelfRef.position;
            pData.rotation = TransformSelfRef.rotation;
            pData.material = MaterialSelfRef;
            savePersistenceData(pData, pData.NetworkID);
        }
        public PersistenceData loadPersistenceData(string NetworkID)
        {
            Debug.Log("xxxshishir: Load persistence data called");
            return pData;
        }
        public void savePersistenceData(PersistenceData pData, string NetworkID)
        {
            Debug.Log("xxxshishir: Save persistence data called");
        }
    }
}