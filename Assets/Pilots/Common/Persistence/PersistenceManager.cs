using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;


namespace VRT.Pilots.Common
{
    //
    //Scenes that contain persistable objects should include a single persistence manager to manage the saving and loading of game object data
    //The manager maintains a dictionary of objects identified by their network ID 
    public class PersistenceManager : MonoBehaviour
    {
        public static PersistenceManager instance { get; private set; }
        public IDictionary<string, PersistenceData> persistenceDataDictionary;
        private List<IDataPersistence> persistableSceneObjects;
        //xxxshishir ToDo: Add config variables to enable and disable this feature
        private bool loadPersistenceData = true;
        private bool savePersistenceData = true;
        //xxxshishir maybe we want to make this user specific and use the current username to name the file
        private string fileName = "PersistenceData.json";
        // Start is called before the first frame update
        private void Awake()
        {
            //xxxshishir ToDo: Replace this with config.json setting later
            loadPersistenceData = true;
            if (instance != null)
                Debug.LogError("Found more than one Persistence Manager in the scene");
            instance = this;
            if (!loadPersistenceData)
                return;
            persistableSceneObjects = FindAllPersistableObjects();
            string saveFile = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(saveFile))
            {
                Debug.LogError("No save data found!");
                return;
            }
            string saveFileData = "";
            try
            {
                using (FileStream stream = new FileStream(saveFile, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        saveFileData = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Couldn't load persistence data from file :" + saveFile + "\n" + e);
                return;
            }
            PersistenceDataCollection pDataCollection = JsonUtility.FromJson<PersistenceDataCollection>(saveFileData);
            persistenceDataDictionary = pDataCollection.toDictionary();

            //xxxshishir Loading persistence data to all persistable objects (with controllers) in the scene
            foreach (IDataPersistence pObjects in persistableSceneObjects)
            {
                string NetworkID = pObjects.getNetworkID();
                PersistenceData pData = persistenceDataDictionary[NetworkID];
                pObjects.loadPersistenceData(pData);
                persistenceDataDictionary.Remove(NetworkID);
            }
        }
        private List<IDataPersistence> FindAllPersistableObjects()
        {
            IEnumerable<IDataPersistence> persistableObjects = FindObjectsOfType<MonoBehaviour>().OfType<IDataPersistence>();
            return new List<IDataPersistence>(persistableObjects);
        }
    }
    [System.Serializable]
    //xxxshishir Temporary class to hold the collection of persistence data, by default we can only serialize fields and not types like Dictionaries
    //https://docs.unity3d.com/2021.2/Documentation/Manual/JSONSerialization.html
    //Another option is to use the Newtonsoft Unity package: https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@2.0/manual/index.html
    public class PersistenceDataCollection
    {
        public ICollection<PersistenceData> pDataCollection;
        public IDictionary<string,PersistenceData> toDictionary()
        {
            IDictionary<string, PersistenceData> pDataDictionary = new Dictionary<string,PersistenceData>();
            foreach (PersistenceData pData in pDataCollection)
                pDataDictionary.Add(pData.NetworkID, pData);
            return pDataDictionary;
        }
        public void fromDictionary(IDictionary<string,PersistenceData> pDataDictionary)
        {
            pDataCollection = pDataDictionary.Values;
        }
    }
}