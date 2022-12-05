using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_CloudGenerator : MonoBehaviour
    {
        [SerializeField] int NumOfClouds;
        [SerializeField] GameObject cloud; 
        public List<Mesh> cloudMeshes;
        [SerializeField] List<GameObject> CloudObjs;
        [SerializeField] float StencilValue = 0f;
        static private ViveSR_Experience_CloudGenerator _instance;
        static public ViveSR_Experience_CloudGenerator instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ViveSR_Experience_CloudGenerator>();
                }
                return _instance;
            }
        }
        private void OnEnable()
        {
            if (CloudObjs.Count == 0)
                GenerateClouds(NumOfClouds);
            else
                SetClouds(true);
        }
        private void OnDisable()
        {
            SetClouds(false);
        }

        void GenerateClouds(int NumOfClouds)
        {
            for (int i = 0; i < NumOfClouds; i++)
            {
                // To-Do: if is Portal Mode, set stencil comp to "Equal" & layer to "virtual world"
                GameObject go = Instantiate(cloud, gameObject.transform, transform);
                MeshRenderer rnd = go.GetComponentInChildren<MeshRenderer>(true);
                if (rnd)
                {
                    rnd.material.SetFloat("_StencilComp", (float)UnityEngine.Rendering.CompareFunction.Equal);
                    rnd.material.SetFloat("_StencilValue", StencilValue);
                    rnd.gameObject.layer = LayerMask.NameToLayer("VirtualWorldLayer");
                    rnd.receiveShadows = false;
                    rnd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                CloudObjs.Add(go);
            }
        }

        void SetClouds(bool on)
        {
            foreach (GameObject go in CloudObjs)
            {
                if (go != null) go.SetActive(on);
            }
        }
    }
}
