using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Valve.VR.InteractionSystem;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Portal : MonoBehaviour
    {
        [SerializeField] GameObject PortalPrefab;
        ViveSR_Portal Portal;
        GameObject HeadCollision;
        public ViveSR_PortalManager PortalManager;
        Vector3 OldPosition;
        public ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr_portal;

        List<ViveSR_PortalTraveller> HandTravellers = new List<ViveSR_PortalTraveller>();

        [SerializeField] protected GameObject VR_BG, VR_BG_Cutout;
        [SerializeField] List<Material> BGObjsMats = new List<Material>();
        WorldMode oldMode = WorldMode.RealWorld;

        bool isPortalOn;
        public bool IsPortalOn { get {return isPortalOn;} private set {isPortalOn = value;} }

        /// <summary>
        /// An event triggered when the portal is set on.
        /// </summary>
        public UnityEvent portalOnEvent = new UnityEvent();
        /// <summary>
        /// An event triggered when the portal is set off.
        /// </summary>
        public UnityEvent portalOffEvent = new UnityEvent();
      
        public void Init()
        {
            for (int i = 0; i < Player.instance.handCount; i++)
            {
                if (Player.instance.GetHand(i).isPoseValid)
                    HandTravellers.Add(Player.instance.GetHand(i).GetComponent<ViveSR_PortalTraveller>());
            }

            if(ViveSR_Experience.instance.scene == SceneType.Demo) InitForDemo();
            InitForAll();
        }

        private void InitForDemo()
        {
            if (PortalManager == null)
                PortalManager = ViveSR_Experience_Demo.instance.PortalManager;
            if (VR_BG == null)
                VR_BG = ViveSR_Experience_Demo.instance.Portal_VR_BG;
            if (VR_BG_Cutout == null)
                VR_BG_Cutout = ViveSR_Experience_Demo.instance.Portal_VR_BG_Cutout;
            if (dartGeneratorMgr_portal == null && ViveSR_Experience_Demo.instance.DartGeneratorMgrs.Count > 0)
                dartGeneratorMgr_portal = ViveSR_Experience_Demo.instance.DartGeneratorMgrs[DartGeneratorIndex.ForPortal];
        }

        private void InitForAll()
        {
            HeadCollision = ViveSR_Experience.instance.PlayerHeadCollision;

            //Get objects ready to be used as VRBackground
            if (VR_BG != null) SetVRBGMaterials(VR_BG, false);
            if (VR_BG_Cutout != null) SetVRBGMaterials(VR_BG_Cutout, true);
        }

        public void SetPortal(bool isOn)
        {                         
            isPortalOn = isOn;
            if (isOn)
            {
                Portal.gameObject.SetActive(true);
                PortalManager.TurnOnCamera();
                QualitySettings.shadows = ShadowQuality.Disable;
                ResetPortalPosition();
            }
            else
            {
                try
                {
                    PortalManager.viewerInWorld = WorldMode.RealWorld;
                    PortalManager.UpdateViewerWorld();
                    MatchControllerWorld();

                    ResetHandTravellers();   
                }       
                catch(System.Exception e)
                {
                    Debug.LogWarning("[EXPERIENCE] ISSUE #411:" + e.Message);
                }

                Portal.gameObject.SetActive(false);
               
                PortalManager.TurnOffCamera();
                QualitySettings.shadows = ShadowQuality.All;
            }
            dartGeneratorMgr_portal.gameObject.SetActive(isOn);
            PortalManager.gameObject.SetActive(isOn);

            if (isOn)
            {
                portalOnEvent.Invoke();
            }
            else
            {
                portalOffEvent.Invoke();
            }
        } 

        void SetVRBGMaterials(GameObject Obj, bool Cutout)
        {
            for (int i = 0; i < Obj.transform.childCount; i++)
            {
                Renderer renderer = Obj.transform.GetChild(i).gameObject.GetComponent<Renderer>();
                if (renderer)
                {
                    BGObjsMats.Add(renderer.material);
                    renderer.material.shader = Shader.Find(Cutout ? "ViveSR/Standard, AlphaTest, Stencil" : "ViveSR/Standard, Stencil");
                    if (Cutout) renderer.material.SetFloat("_CullMode", (float)UnityEngine.Rendering.CullMode.Off);
                    renderer.material.SetFloat("_StencilComp", (float)UnityEngine.Rendering.CompareFunction.Equal);
                    renderer.material.SetFloat("_ZTestComp", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                }

                if (Obj.transform.GetChild(i).transform.childCount > 0)
                {
                    SetVRBGMaterials(Obj.transform.GetChild(i).gameObject, Cutout);
                }
            }
        }                    

        public void UpdateBGStencil()
        {
            foreach (Material mat in BGObjsMats)
            {
                mat.SetFloat("_StencilComp", PortalManager.viewerInWorld == WorldMode.RealWorld ?
                     (float)UnityEngine.Rendering.CompareFunction.Equal :
                     (float)UnityEngine.Rendering.CompareFunction.Always);
            }
        }

        public void InitPortal()
        {
            GameObject go = Instantiate(PortalPrefab);
            go.transform.position = ViveSR_Experience.instance.PlayerHeadCollision.transform.position + ViveSR_Experience.instance.PlayerHeadCollision.transform.forward * 0.5f;
            go.transform.forward = -ViveSR_Experience.instance.PlayerHeadCollision.transform.forward;
            PortalManager.AddPortal(go);
            Portal = go.GetComponent<ViveSR_Portal>();

            OldPosition = Portal.transform.position;

            StartCoroutine(CheckViewerInWorld());
            StartCoroutine(CheckPortalPosition());

            Portal.gameObject.SetActive(false);
        }

        public void ResetPortalPosition()
        {
            Portal.transform.position = HeadCollision.transform.position + HeadCollision.transform.forward * 0.5f;
            Portal.transform.forward = (PortalManager.viewerInWorld == WorldMode.RealWorld ? -1 : 1) * HeadCollision.transform.forward;
            Portal.UpdatePlaneNormal();
            OldPosition = Portal.transform.position;
        }

        IEnumerator CheckPortalPosition()
        {
            while (isPortalOn)
            {
                if (Portal.transform.position != OldPosition)
                {
                    Portal.UpdatePlaneNormal();
                    OldPosition = Portal.transform.position;
                }
                yield return new WaitForEndOfFrame();
            }
        }
                             
        IEnumerator CheckViewerInWorld()
        {
            while (isPortalOn)
            {
                if (PortalManager.viewerInWorld != oldMode)
                {
                    MatchControllerWorld();
                    UpdateBGStencil();
                    oldMode = PortalManager.viewerInWorld;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        public void MatchControllerWorld()
        {
            foreach (ViveSR_PortalTraveller handTraveller in HandTravellers)
            {
                handTraveller.IsTransitioning = false;
                PortalManager.controllerInWorld = handTraveller.CurrentWorld = PortalManager.viewerInWorld;
                if (handTraveller.Renderers == null) handTraveller.Renderers = handTraveller.gameObject.GetComponentsInChildren<MeshRenderer>(true);
                handTraveller.SwitchMaterials(handTraveller.Renderers, handTraveller.CurrentWorld);
                handTraveller.SetClippingPlaneEnable(handTraveller.Renderers, false, handTraveller.CurrentWorld);
            }
        }

        private void ResetHandTravellers()
        {
            foreach (ViveSR_PortalTraveller handTraveller in HandTravellers)
                handTraveller.ResetTransitionState();
        }

        private void OnDestroy()
        {
            foreach (Material mat in BGObjsMats)
            {
                mat.shader = Shader.Find("Standard");
            }
        }
    }
}