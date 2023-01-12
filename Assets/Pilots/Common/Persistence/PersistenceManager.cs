using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace VRT.Pilots.Common
{
    //
    //Scenes that contain persistable objects should include a single persistence manager to manage the saving and loading of game object data
    //The manager maintains a dictionary of objects identified by their network ID 
    public class PersistenceManager : MonoBehaviour
    {
        public static PersistenceManager instance { get; private set; }
        public IDictionary<string, PersistenceData> persistenceDataDictionary;
        //xxxshishir ToDo: Add config variables to enable and disable this feature
        private bool loadPersistenceData = true;
        private bool savePersistenceData = true;
        //xxxshishir maybe we want to make this user specific and use the current username to name the file
        private string fileName = "PersistenceData.json";
        // Start is called before the first frame update
        private void Awake()
        {
            if (instance != null)
                Debug.LogError("Found more than one Persistence Manager in the scene");
            instance = this;
            PersistenceData pData;

        }
    }
}