using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Pilots.Common
{
    public class InstantiateBall : MonoBehaviour
    {

        public GameObject objectToInstance;
        public GameObject initPoint;
        public AudioSource audioAppear;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void InstantiateObject()
        {
            audioAppear.Play();
            GameObject newObject = Instantiate(objectToInstance, initPoint.transform.position, initPoint.transform.rotation);
            NetworkIdBehaviour[] netIdBehaviours = newObject.GetComponentsInChildren<NetworkIdBehaviour>(true);
            foreach (var nib in netIdBehaviours)
            {
                nib.CreateNetworkId();
            }
        }
    }
}
