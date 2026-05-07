using System.Collections.Generic;
using UnityEngine;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Allow disabling a user to move (because the scenario requires them to be static)
    /// </summary>
    public class DisableUserMovement : MonoBehaviour
    {
        [Tooltip("Disable movement automatically (otherwise call EnableMovement() method)")]
        public bool autoDisable = false;
        [Tooltip("GameObjects to disable")]
        [SerializeField]List<GameObject> movement;
        // Start is called before the first frame update
        void Start()
        {
            if (autoDisable)
            {
                EnableMovement(false);
            }
        }


        // Update is called once per frame
        void Update()
        {

        }

        public void EnableMovement(bool isEnabled)
        {
            foreach (GameObject o in movement)
            {
                o.SetActive(isEnabled);
            }
        }
    }
}

