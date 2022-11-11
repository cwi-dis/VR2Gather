using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

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
#if !VRT_WITH_STATS
            Debug.LogWarning("ManualStatsAnnotator: VRT_WITH_STATS not defined, making this script a bit pointless...");
#endif
        }

            // Update is called once per frame
            void Update()
        {
#if VRT_WITH_STATS
            if(Input.GetKeyDown(key1))
                Statistics.Output("ManualAnnotator", msg1);
            if(Input.GetKeyDown(key2))
                Statistics.Output("ManualAnnotator", msg2);
            if(Input.GetKeyDown(key3))
                Statistics.Output("ManualAnnotator", msg3);
#endif
        }
    }
}
