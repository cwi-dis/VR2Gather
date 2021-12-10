using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Core
{
    public class ManualStatsAnnotator : MonoBehaviour
    {
        public string msg1 = "Something interesting happened here";
        public string msg2 = "Something interesting happened here";
        public string msg3 = "Something interesting happened here";

        public KeyCode key1;
        public KeyCode key2;
        public KeyCode key3;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(key1))
                BaseStats.Output("ManualAnnotator", msg1);
            if(Input.GetKeyDown(key2))
                BaseStats.Output("ManualAnnotator", msg2);
            if(Input.GetKeyDown(key3))
                BaseStats.Output("ManualAnnotator", msg3);
        }
    }
}
