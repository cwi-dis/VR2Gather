using UnityEngine;
using UnityEngine.AI;

namespace Vive.Plugin.SR.Experience
{                                 
    public class ViveSR_Experience_Chair : MonoBehaviour
    {
        public enum OccupierType
        {
            None,
            Player,
            Fairy
        }

        NavMeshObstacle NaveMeshObstacle = null;     
        public ViveSR_Experience_NPCAnimationRef OccupyingNPC { get; private set; }
        public OccupierType Occupier;

        public void CreateChair(Vector3 Position, Vector3 Forward)
        {
            if (NaveMeshObstacle == null)
            {
                transform.position = Position;
                transform.forward = Forward;
                NaveMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
                NaveMeshObstacle.size = new Vector3(0.7f, 1, 0.05f);
                NaveMeshObstacle.center = new Vector3(0f, 0f, -0.2f);
            }
        }

        public void AssignFairyAsOccupier(ViveSR_Experience_NPCAnimationRef npc)
        {
            if (OccupyingNPC != null) OccupyingNPC.OccupyingChair = null;

            OccupyingNPC = npc;
            OccupyingNPC.OccupyingChair = this;
            Occupier = OccupierType.Fairy;
        }

        public void AssignPlayerAsOccupier()
        {
            if (OccupyingNPC != null)
            {
                OccupyingNPC.OccupyingChair = null;
                OccupyingNPC = null;
            }

            Occupier = OccupierType.Player;
        }

        public void RemoveOccupier()
        {
            if (OccupyingNPC != null)
            {
                OccupyingNPC.OccupyingChair = null;
                OccupyingNPC = null;
            }

            Occupier = OccupierType.None;
        }
    }
}