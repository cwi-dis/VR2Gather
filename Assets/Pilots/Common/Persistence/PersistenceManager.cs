using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRT.Pilots.Common
{
    //
    //Scenes that contain persistable objects should include a single persistence manager to manage the saving and loading of data
    //The manager maintains a dictionary of objects identified by their network ID 
    public class PersistenceManager : MonoBehaviour
    {
        public static PersistenceManager instance { get; private set; }
        // Start is called before the first frame update
        private void Awake()
        {
            if (instance != null)
                Debug.LogError("Found more than one Persistence Manager in the scene");
            instance = this;
        }
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}