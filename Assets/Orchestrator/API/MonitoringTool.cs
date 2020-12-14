using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orchestrator
{
    public class MonitoringTool : MonoBehaviour
    {
        public static MonitoringTool instance;

        private int startPoint;
        private int endPoint;

        private float ms = 0F;

        private bool bReady = false;

        private void Awake()
        {
            if (instance == null)
                instance = this;
        }


        void Start()
        {

        }

        void Update()
        {
            ms += Time.deltaTime * 1000;
        }

        public void WriteStartPoint()
        {
            if (bReady)
            {
                startPoint = (int)ms;
                bReady = false;
            }
        }

        public void WriteEndPoint()
        {
            if (!bReady)
            {
                endPoint = (int)ms;
                CalculateLatency();
                bReady = true;
            }
        }

        private void CalculateLatency()
        {
            float lLatency = 0;

            if (endPoint >= startPoint)
            {
                lLatency = endPoint - startPoint;
            }

            //Debug.Log("[MonitoringTool][CalculateLantecy] : " + lLatency + " ms");
        }
    }
}