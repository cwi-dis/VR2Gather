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
        public static PersistenceManager Instance { get; private set; }
       private List<IDataPersistence> persistableSceneObjects;
        //xxxshishir ToDo: Add config variables to enable and disable this feature
        [Tooltip("Enable loading of previously saved persistence data ")]
        [SerializeField] private bool loadPersistenceData = true;
        [Tooltip("Enable saving of persistence data in this session")]
        [SerializeField] private bool savePersistenceData = true;
        //xxxshishir maybe we want to make this user specific and use the current username to name the file
        [Tooltip("Filename where persistent data is saved and loaded from")]
        [SerializeField] private string fileName = "PersistenceData.json";
        // Start is called before the first frame update
        void Start()
        {
            //xxxshishir ToDo: Replace this with config.json setting later
            loadPersistenceData = true;
            if (Instance != null)
                Debug.LogError("Found more than one Persistence Manager in the scene");
            Instance = this;
            //xxxshishir disable persistence for all users that are not the session host
            if (!Orchestrator.Wrapping.OrchestratorController.Instance.UserIsMaster)
                loadPersistenceData = false;

            if (!loadPersistenceData)
                return;
            loadAllPaersistenceData();
        }
        private void OnApplicationQuit()
        {
            if(Orchestrator.Wrapping.OrchestratorController.Instance.UserIsMaster)
                saveAllPersistenceData();
        }
        private List<IDataPersistence> FindAllPersistableObjects()
        {
            IEnumerable<IDataPersistence> persistableObjects = FindObjectsOfType<MonoBehaviour>().OfType<IDataPersistence>();
            return new List<IDataPersistence>(persistableObjects);
        }
        public void saveAllPersistenceData()
        {
            // xxxshishir We find persistable objects again on quit to account for dynamically created objects
            Debug.Log($"{name}: saveAllPersistenceData called");
            persistableSceneObjects = FindAllPersistableObjects();
            string NetworkID;
            PersistenceData pData;
            var persistenceDataDictionary = new Dictionary<string, PersistenceData>();
            foreach (IDataPersistence pObjects in persistableSceneObjects)
            {
                pData = pObjects.savePersistenceData();
                NetworkID = pObjects.getNetworkID();
                Debug.Log($"{name}: save data for {NetworkID}");
                persistenceDataDictionary.Add(NetworkID, pData);
            }
            PersistenceDataCollection pDataCollection = new PersistenceDataCollection();
            pDataCollection.fromDictionary(persistenceDataDictionary);
            string saveFile = Path.Combine(Application.persistentDataPath, fileName);
            string saveData = JsonUtility.ToJson(pDataCollection, true);
            //xxxshishir for now we always overwrite the existing savefile
            try
            {
                using (FileStream stream = new FileStream(saveFile, FileMode.Create))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(saveData);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Couldn't save persistence data to file :" + saveFile + "\n" + e);
            }
        }
        public void loadAllPaersistenceData()
        {
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
            var persistenceDataDictionary = pDataCollection.toDictionary();

            //xxxshishir Loading persistence data to all persistable objects (with controllers) in the scene
            string NetworkID;
            PersistenceData pData;
            foreach (IDataPersistence pObjects in persistableSceneObjects)
            {
                NetworkID = pObjects.getNetworkID();
                pData = persistenceDataDictionary[NetworkID];
                pObjects.loadPersistenceData(pData);
                persistenceDataDictionary.Remove(NetworkID);
            }
            if (persistenceDataDictionary.Count != 0)
                Debug.LogWarning($"{name}: Could not initialize {persistenceDataDictionary.Count} persistable objects in the save file");
        }
    }
    [System.Serializable]
    //xxxshishir Temporary class to hold the collection of persistence data, by default we can only serialize fields and not types like Dictionaries
    //https://docs.unity3d.com/2021.2/Documentation/Manual/JSONSerialization.html
    //Another option is to use the Newtonsoft Unity package: https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@2.0/manual/index.html
    public class PersistenceDataCollection
    {
        public List<PersistenceData> pDataCollection;
        public IDictionary<string,PersistenceData> toDictionary()
        {
            IDictionary<string, PersistenceData> pDataDictionary = new Dictionary<string,PersistenceData>();
            foreach (PersistenceData pData in pDataCollection)
                pDataDictionary.Add(pData.NetworkID, pData);
            return pDataDictionary;
        }
        public void fromDictionary(IDictionary<string,PersistenceData> pDataDictionary)
        {
            pDataCollection = pDataDictionary.Values.ToList();
        }
    }
}