using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Tile : MonoBehaviour
    {
        private List<MeshRenderer> renderers = new List<MeshRenderer>();

        private void Awake()
        {
            MeshRenderer[] rnds = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer rnd in rnds)
            {
                renderers.Add(rnd);
            }
        }



        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}