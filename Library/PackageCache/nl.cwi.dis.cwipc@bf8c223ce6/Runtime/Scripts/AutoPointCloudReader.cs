using System;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class AutoPointCloudReader : AbstractPointCloudSource
    {
        [Header("Generic capturerer specific fields")]
        [Tooltip("Filename of cameraconfig.xml file")]
        public string configFileName;
       
        override public void _AllocateReader()
        {
            
            try
            {
                reader = cwipc.capturer(configFileName);
                if (reader != null)
                {
#if CWIPC_WITH_LOGGING
                    Debug.Log($"{Name()}: Started.");
#endif
                }
                else
                    throw new System.Exception($"{Name()}: cwipc_capturer could not be created"); // Should not happen, should throw exception
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: exception {e.ToString()}");
                throw;
            }
        }

        public override bool EndOfData()
        {
            return reader == null;
        }
    }
}
