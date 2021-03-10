using UnityEngine;
using VRT.Pilots.Common;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
//XXXShishir switched back to original pilot0controller, ToDo: Cherry pick the rating scale scene transitions, ToDo: Reimplement scene controller to use modified entity pipeline later
//Note: Use scenemanager.loadsceneasync for the rating scale scene once remote user prerecorded view is reimplemented
namespace VRT.UserRepresentation.PointCloud
{
    public class QualityAssesmentController : MonoBehaviour
    {
        public static QualityAssesmentController Instance { get; private set; }


        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            //xxxshishir load the stimuli list and set the target folder for the prerecorded pointcloud
            
        }

    }
}