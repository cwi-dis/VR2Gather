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
            loadPersistenceData(pData);
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
            savePersistenceData(pData);
        }
        public void loadPersistenceData(PersistenceData pData)
        {
            Debug.Log("xxxshishir: Load persistence data called");
        }
        public void savePersistenceData(PersistenceData pData)
        {
            Debug.Log("xxxshishir: Save persistence data called");
        }
    }
}