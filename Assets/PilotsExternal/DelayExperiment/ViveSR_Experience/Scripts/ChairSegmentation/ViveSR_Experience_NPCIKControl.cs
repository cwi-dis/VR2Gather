using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    [System.Serializable]
    public class ViveSR_Experience_NPCIKControl
    {
        [SerializeField]
        string IKControlName;
        public bool IsActive;
        public AvatarIKGoal targetPart;
        public AvatarIKHint targetHint;
        public GameObject IKObj;
        public Vector3 OriginalIKPos;
        public GameObject IKObj_thigh;
        public GameObject IKHintObj;
        public float IKWeight;
    }
}