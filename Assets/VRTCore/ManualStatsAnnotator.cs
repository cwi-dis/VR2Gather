using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Core
{
    public class ManualStatsAnnotator : MonoBehaviour
    {
        private string msg = "Something interesting happened here";
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                BaseStats.Output("ManualAnnotator", msg);
            }
        }
    }
}
