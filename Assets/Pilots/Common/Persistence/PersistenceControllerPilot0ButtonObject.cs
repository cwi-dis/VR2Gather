using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRT.Pilots.Common
{
    public class PersistenceControllerPilot0ButtonObject : PersistenceController, IDataPersistence
    {
    
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
  
    }
}