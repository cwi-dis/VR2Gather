using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Load and save state of all persistable objects.
    /// </summary>
    public class PersistenceManager : NetworkIdBehaviour
    {
        public class PersistenceManagerData : BaseMessage
        {
            public bool doLoad;
            public bool doSave;
        }
       
        public static PersistenceManager Instance { get; private set; }
        //xxxshishir ToDo: Add config variables to enable and disable this feature
        [Tooltip("Automatically load of previously saved persistence data on start")]
        [SerializeField] private bool loadPersistentDataOnStart = true;
        [Tooltip("Automatically save persistence data on session end")]
        [SerializeField] private bool savePersistentDataOnQuit = true;
        [Tooltip("Automatic load/save in master only, otherwise in all instances")]
        [SerializeField] private bool masterOnly = true;
        //xxxshishir maybe we want to make this user specific and use the current username to name the file
        [Tooltip("Filename where persistent data is saved and loaded from")]
        [SerializeField] private string fileName = "PersistenceData.json";

        protected override void Awake()
        {
            base.Awake();
            OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_PersistenceManagerData, typeof(PersistenceManagerData));
        }

        public virtual void OnEnable()
        {
            OrchestratorController.Instance.Subscribe<PersistenceManagerData>(OnPersistenceCall);
        }

        public virtual void OnDisable()
        {
            OrchestratorController.Instance.Unsubscribe<PersistenceManagerData>(OnPersistenceCall);
        }
        
        // Start is called before the first frame update
        void Start()
        {
            if (Instance != null)
                Debug.LogError("PersistenceManager: multiple instances in scene");
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

        /// <summary>
        /// Call this method to load persistent data (or forward the request to do so to the master)
        /// </summary>
        public void OnLoad()
        {
            PersistenceManagerData data = new PersistenceManagerData()
            {
                doLoad = true
            };
            if (OrchestratorController.Instance.UserIsMaster)
            {
                Debug.Log("PersistenceManager: OnLoad: master, therefore loading data");
                OnPersistenceCall(data);
            }
            else
            {
                Debug.Log("PersistenceManager: OnLoad: forwarding to master");
                OrchestratorController.Instance.SendTypeEventToMaster(data);
            }
        }

        /// <summary>
        /// Call this method to save persistent data (or forward the request to do so to the master)
        /// </summary>
        public void OnSave()
        {
            PersistenceManagerData data = new PersistenceManagerData()
            {
                doSave = true
            };
            if (OrchestratorController.Instance.UserIsMaster)
            {
                Debug.Log("PersistenceManager: OnSave: master, therefore loading data");
                OnPersistenceCall(data);
            }
            else
            {
                Debug.Log("PersistenceManager: OnSave: forwarding to master");
                OrchestratorController.Instance.SendTypeEventToMaster(data);
            }
        }

        public void OnPersistenceCall(PersistenceManagerData data)
        {
            if (!OrchestratorController.Instance.UserIsMaster)
            {
                Debug.LogError($"PersistenceManager: OnPersistenceCall but not master");
                return;
            }
            if (data.doLoad)
            {
                loadAllPersistentData();
            }
            if (data.doSave)
            {
                saveAllPersistentData();
            }
        }

        private List<IDataPersistence> FindAllPersistableObjects()
        {
            IEnumerable<IDataPersistence> persistableObjects = FindObjectsOfType<MonoBehaviour>().OfType<IDataPersistence>();
            return new List<IDataPersistence>(persistableObjects);
        }

        private void saveAllPersistentData()
        {
            if (masterOnly && !OrchestratorController.Instance.UserIsMaster)
            {
                Debug.Log($"PersistenceManager: saveAllPersistentData: not saving, not master");
                return;
            }
            // xxxshishir We find persistable objects again on quit to account for dynamically created objects
            Debug.Log($"PersistenceManager: saveAllPersistenceData called");
            List<IDataPersistence> persistableSceneObjects = FindAllPersistableObjects();
            string NetworkID;
            PersistentData pData;
            var persistenceDataDictionary = new Dictionary<string, PersistentData>();
            foreach (IDataPersistence pObjects in persistableSceneObjects)
            {
                pData = pObjects.getPersistentDataForSaving();
                NetworkID = pObjects.getNetworkID();
                Debug.Log($"PersistenceManager: save data for {NetworkID}");
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
                Debug.LogError($"PersistenceManager: {saveFile}: Error {e}");
            }
        }

        private void loadAllPersistentData()
        {
            if (masterOnly && !OrchestratorController.Instance.UserIsMaster)
            {
                Debug.Log($"PersistenceManager: loadAllPersistentData: not loading, not master");
                return;
            }
            List<IDataPersistence> persistableSceneObjects = FindAllPersistableObjects();
            string saveFile = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(saveFile))
            {
                Debug.LogError($"PersistenceManager: File not found: {saveFile}");
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
                Debug.LogError($"PersistenceManager: Error reading from {saveFile}: {e}");
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
                Debug.LogWarning($"PersistenceManager: Could not initialize {persistenceDataDictionary.Count} persistable objects present in the save file");
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