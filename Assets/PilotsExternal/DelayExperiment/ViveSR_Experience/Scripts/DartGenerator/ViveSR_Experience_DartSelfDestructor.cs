using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_DartSelfDestructor : MonoBehaviour
    {
        void Update()
        {
            if (transform.position.y < -1) Destroy(gameObject);
        }
    }
}