using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_NPCGenerator : MonoBehaviour
    {    
        bool isGeneratingNPC;
        [SerializeField] int NumOfNPC;
        List<ViveSR_Experience_NPCAnimationRef> NPCRefs = new List<ViveSR_Experience_NPCAnimationRef>();
        [SerializeField] public GameObject NPCPrefab;

        ViveSR_Experience_Chair PlayersChair = null;
        List<ViveSR_Experience_Chair> Chairs = new List<ViveSR_Experience_Chair>();

        Vector3 Spawn_Pos, Spawn_Fwd, Portal_Pos;

        [SerializeField] GameObject PortalPrefeb;
        ViveSR_PortalManager portalMgr = null;
        bool InDemo = false;
        Coroutine NPCAnimateCoroutine = null;

        ViveSR_Portal Portal;

        private void Awake()
        {
            if (ViveSR_Experience_Demo.instance != null)
                InDemo = true;

            if (portalMgr == null)
            {
                if (InDemo)
                    portalMgr = ViveSR_Experience_Demo.instance.PortalManager;
                else
                    portalMgr = FindObjectOfType<ViveSR_PortalManager>();
            }

            Portal = Instantiate(PortalPrefeb.GetComponent<ViveSR_Portal>());
            Portal.gameObject.SetActive(false);
        }

        private static void TransitionMatCB(Material mat)
        {
            mat.shader = Shader.Find("ViveSR/Standard, AlphaTest, Stencil");
            mat.renderQueue = 2001;
        }
        private void Update()
        {
            //Update NPC
            if (NPCRefs.Count != 0 && !isGeneratingNPC)
            {
                //if player walks close to a chair, the npc yields to player.
                YieldToPlayer();

                //standing npc attempts to find a chair
                foreach (ViveSR_Experience_NPCAnimationRef NPCRef in NPCRefs)
                {
                    if (NPCRef.OccupyingChair == null) AssignClosestChair(NPCRef);
                }
            }
        }

        public void Play(Vector3 spawnPos, Vector3 spawnForward, List<ViveSR_Experience_Chair> Chairs = null)
        {
            isGeneratingNPC = false;

            Spawn_Fwd = spawnForward;
            Spawn_Pos = spawnPos;
            Spawn_Fwd = new Vector3(Spawn_Fwd.x, 0, Spawn_Fwd.z);
            Spawn_Pos = new Vector3(Spawn_Pos.x, 0, Spawn_Pos.z);

            Portal_Pos = Spawn_Pos - Spawn_Fwd * 2;

            ClearScene();

            if (Chairs != null) this.Chairs = Chairs;
            NumOfNPC = Chairs.Count;

            if (NPCAnimateCoroutine != null) {
                StopCoroutine(NPCAnimateCoroutine);
                NPCAnimateCoroutine = null;
            }

            NPCAnimateCoroutine = StartCoroutine(GenerateNPCs());
        }

        public void ClearScene()
        {
            isGeneratingNPC = false;
            Portal.gameObject.SetActive(false);

            foreach (ViveSR_Experience_NPCAnimationRef NPCRef in NPCRefs) Destroy(NPCRef.gameObject);
            foreach (ViveSR_Experience_Chair chair in Chairs) chair.RemoveOccupier();
            NPCRefs.Clear();
        }

        IEnumerator GenerateNPCs()
        {
            Portal.gameObject.SetActive(false);

            isGeneratingNPC = true;

            Portal.gameObject.SetActive(true);

            Portal.TransitionMaterialUpdateCB = TransitionMatCB;

            Portal.transform.position = Portal_Pos;
            Portal.transform.forward = -Spawn_Fwd;

            portalMgr.AddPortal(Portal.gameObject);
            portalMgr.UpdateViewerWorld();

            ViveSR_Experience_PortalAnimation pa = Portal.GetComponent<ViveSR_Experience_PortalAnimation>();
            pa.PortalLogo.gameObject.layer = LayerMask.NameToLayer("VirtualWorldLayer");

            while (NPCRefs.Count < NumOfNPC && isGeneratingNPC)
            {
                ViveSR_Experience_NPCAnimationRef NPCRef = Instantiate(NPCPrefab).GetComponent<ViveSR_Experience_NPCAnimationRef>();
                NPCRefs.Add(NPCRef);

                NPCRef.transform.position = Spawn_Pos;
                NPCRef.transform.forward = -Spawn_Fwd;

                NPCRef.NPCAnimController.Walk(Portal_Pos, () =>
                {
                    AssignClosestChair(NPCRef);
                });

                //next fairy
                yield return new WaitForSeconds(2f);
            }

            if(pa.isActiveAndEnabled) pa.SetPortalScale(false);

            isGeneratingNPC = false;
        }

        void AssignClosestChair(ViveSR_Experience_NPCAnimationRef NPCRef, ViveSR_Experience_Chair OldChair = null)
        {
            if (NPCRef.NPCAnimController.isActing) return;

            float minDist = 999;
            ViveSR_Experience_Chair targetChair = null;
            for (int i = 0; i < Chairs.Count; i++)
            {
                bool isChairOccupied = Chairs[i].Occupier != ViveSR_Experience_Chair.OccupierType.None;
                if (isChairOccupied) continue;

                float distToNpc = Vector3.Distance(NPCRef.transform.position, Chairs[i].transform.position);

                if (distToNpc < minDist)
                {
                    minDist = distToNpc;
                    targetChair = Chairs[i];
                }
            }

            if(targetChair != null)
            {
                targetChair.AssignFairyAsOccupier(NPCRef);
                NPCRef.NPCAnimController.StartAnimationSequence_ChairFound(targetChair);
            }
            else
            {
                if(OldChair != null)
                    NPCRef.NPCAnimController.StartAnimationSequence_ChairNotFound(OldChair);
            }
        }

        void YieldToPlayer()
        {
            //Player walks away from a chair;
            if (PlayersChair != null && Vector3.Distance(ViveSR_Experience.instance.PlayerHeadCollision.transform.position, PlayersChair.transform.position) > 1)
            {
                PlayersChair.RemoveOccupier();
                PlayersChair = null;
            }

            //player find a closet chair within a distance
            float minDist = 999;
            ViveSR_Experience_Chair targetChair = null;
            for (int i = 0; i < Chairs.Count; i++)
            {
                Vector3 chairPos = Chairs[i].transform.position;
                Vector3 playerHeadPos = ViveSR_Experience.instance.PlayerHeadCollision.transform.position;

                float dist = Vector3.Distance(playerHeadPos, new Vector3(chairPos.x, playerHeadPos.y, chairPos.z));
                if (dist < minDist) minDist = dist;
                else continue; // this chair isn't the closest
                if (minDist >= 1f) continue; // the closest chair is too far
                if (PlayersChair == Chairs[i]) continue; // player is already using the closet chair.
                targetChair = Chairs[i];
            }

            if (targetChair == null) return;

            //NPC on the chair yields to the player
            if (targetChair.OccupyingNPC != null)
            {
                if (targetChair.OccupyingNPC.NPCAnimController.isActing) return;
                if (PlayersChair != null) PlayersChair.RemoveOccupier();

                ViveSR_Experience_NPCAnimationRef NPCToYield = targetChair.OccupyingNPC;
                PlayersChair = targetChair;
                PlayersChair.AssignPlayerAsOccupier();
                NPCToYield.NPCAnimController.Stand(() => AssignClosestChair(NPCToYield, targetChair));
            }
            else
            {
                if (PlayersChair != null) PlayersChair.RemoveOccupier();
                PlayersChair = targetChair;
                PlayersChair.AssignPlayerAsOccupier();
            }
        }
    }
}