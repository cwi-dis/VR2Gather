using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;
using VRT.Orchestrator.Wrapping;

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
        [SerializeField] private bool loadPersistentDataOnStart = true;
        [Tooltip("Enable saving of persistence data in this session")]
        [SerializeField] private bool savePersistentDataOnQuit = true;
        [Tooltip("When set will load/save only in master, otherwise in all instances")]
        [SerializeField] private bool masterOnly = true;
        //xxxshishir maybe we want to make this user specific and use the current username to name the file
        [Tooltip("Filename where persistent data is saved and loaded from")]
        [SerializeField] private string fileName = "PersistenceData.json";
        // Start is called before the first frame update
        void Start()
        {
            if (Instance != null)
                Debug.LogError("Found more than one Persistence Manager in the scene");
            Instance = this;
            if (loadPersistentDataOnStart)
            {
                loadAllPersistentData();
            }
        }

        private void OnApplicationQuit()
        {
            if(savePersistentDataOnQuit)
            {
                saveAllPersistentData();
            }
        }
        private List<IDataPersistence> FindAllPersistableObjects()
        {
            IEnumerable<IDataPersistence> persistableObjects = FindObjectsOfType<MonoBehaviour>().OfType<IDataPersistence>();
            return new List<IDataPersistence>(persistableObjects);
        }
        public void saveAllPersistentData()
        {
            if (masterOnly && !OrchestratorController.Instance.UserIsMaster)
            {
                Debug.Log($"{name}: saveAllPersistentData: not saving, not master");
                return;
            }
            // xxxshishir We find persistable objects again on quit to account for dynamically created objects
            Debug.Log($"{name}: saveAllPersistenceData called");
            persistableSceneObjects = FindAllPersistableObjects();
            string NetworkID;
            PersistentData pData;
            var persistenceDataDictionary = new Dictionary<string, PersistentData>();
            foreach (IDataPersistence pObjects in persistableSceneObjects)
            {
                pData = pObjects.getPersistentDataForSaving();
                NetworkID = pObjects.getNetworkID();
                Debug.Log($"{name}: save data for {NetworkID}");
                persistenceDataDictionary.Add(NetworkID, pData);
            }
            PersistentDataCollection pDataCollection = new PersistentDataCollection();
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
        public void loadAllPersistentData()
        {
            if (masterOnly && !OrchestratorController.Instance.UserIsMaster)
            {
                Debug.Log($"{name}: loadAllPersistentData: not loading, not master");
                return;
            }
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
            PersistentDataCollection pDataCollection = JsonUtility.FromJson<PersistentDataCollection>(saveFileData);
            var persistenceDataDictionary = pDataCollection.toDictionary();

            //xxxshishir Loading persistence data to all persistable objects (with controllers) in the scene
            string NetworkID;
            PersistentData pData;
            foreach (IDataPersistence pObjects in persistableSceneObjects)
            {
                NetworkID = pObjects.getNetworkID();
                pData = persistenceDataDictionary[NetworkID];
                pObjects.loadPersistentData(pData);
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
    public class PersistentDataCollection
    {
        public List<PersistentData> pDataCollection;
        public IDictionary<string,PersistentData> toDictionary()
        {
            IDictionary<string, PersistentData> pDataDictionary = new Dictionary<string,PersistentData>();
            foreach (PersistentData pData in pDataCollection)
                pDataDictionary.Add(pData.NetworkID, pData);
            return pDataDictionary;
        }
        public void fromDictionary(IDictionary<string,PersistentData> pDataDictionary)
        {
            pDataCollection = pDataDictionary.Values.ToList();
        }
    }
}