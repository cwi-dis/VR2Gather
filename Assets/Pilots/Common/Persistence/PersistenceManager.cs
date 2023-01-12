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
            //xxxshishir ToDo: Replace with config.json setting later
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

            PersistenceData pDataArray[] = JsonUtility.FromJson<PersistenceData[]>(saveFileData);

        }
        private List<IDataPersistence> FindAllPersistableObjects()
        {
            IEnumerable<IDataPersistence> persistableObjects = FindObjectsOfType<MonoBehaviour>().OfType<IDataPersistence>();
            return new List<IDataPersistence>(persistableObjects);
        }
    }
}