using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.UserRepresentation.Voice;
using VRTCore;
using VRT.Core;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;
using System;

namespace VRT.UserRepresentation.PointCloud
{
    public class PrerecordedTileSelector : MonoBehaviour
    {
        PrerecordedPointcloud prerecordedPointcloud;
 

        int nQualities;
        int nTiles;
        public enum SelectionAlgorithm { interactive, alwaysBest, frontTileBest };
        public SelectionAlgorithm algorithm = SelectionAlgorithm.interactive; // xxxjack to be determined by overall controller

        //xxxshishir tile selector stuff ToDo Check with Jack before refactor
        public double bitRatebudget;
        private Transform cameraTransform;
        private Vector3 cameraForward;
        private int[] tileOrder;
        private float[] tileUtilities;
        private float t1Utility;
        private float t2Utility;
        private float t3Utility;
        private float t4Utility;
        private Vector3 TileC1 = new Vector3(0, 0, 1);
        private Vector3 TileC2 = new Vector3(1, 0, 0);
        private Vector3 TileC3 = new Vector3(0, 0, -1);
        private Vector3 TileC4 = new Vector3(-1, 0, 0);


        string Name()
        {
            return "PrerecordedTileSelector";
        }

        public void Init(PrerecordedPointcloud _prerecordedPointcloud, int _nQualities, int _nTiles)
        {
            prerecordedPointcloud = _prerecordedPointcloud;
            nQualities = _nQualities;
            nTiles = _nTiles;
            Debug.Log($"{Name()}: PrerecordedTileSelector nQualities={nQualities}, nTiles={nTiles}");
            if (nTiles != 4)
            {
                Debug.LogError($"{Name()}: Only 4 tiles implemented");
            }
        }

        private void Update()
        {
            //Debug.Log($"xxxjack PrerecordedPointcloud update called");
            if (prerecordedPointcloud == null)
            {
                // Not yet initialized
                return;
            }
            // xxxjack need to be obtained from somewhere...
            double[] a1 = null;
            double[] a2 = null;
            double[] a3 = null;
            double[] a4 = null;
            double budget = 0;

            //xxxshishir get camera orientation ToDo: Move to getTileOrder ?
            var cam = FindObjectOfType<Camera>().gameObject;
            if (cam == null)
                Debug.LogError("Camera not found!");
            cameraTransform = cam.transform;
            cameraForward = cameraTransform.forward;

            switch (algorithm)
            {
                case SelectionAlgorithm.interactive:
                    getTilesInteractive(a1, a2, a3, a4, budget);
                    break;
                case SelectionAlgorithm.alwaysBest:
                    getTilesAlwaysBest(a1, a2, a3, a4, budget);
                    break;
                case SelectionAlgorithm.frontTileBest:
                    getTilesFrontTileBest(a1, a2, a3, a4, budget);
                    break;
                default:
                    Debug.LogError($"{Name()}: Unknown algorithm");
                    break;
            }
        }

        void getTilesInteractive(double[] a1, double[] a2, double[] a3, double[] a4, double budget)
        {
            int[] selectedQualities = new int[nTiles];
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                prerecordedPointcloud.SelectTileQualities(selectedQualities);
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = nQualities - 1;
                prerecordedPointcloud.SelectTileQualities(selectedQualities);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[0] = nQualities - 1;
                prerecordedPointcloud.SelectTileQualities(selectedQualities);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[1] = nQualities - 1;
                prerecordedPointcloud.SelectTileQualities(selectedQualities);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[2] = nQualities - 1;
                prerecordedPointcloud.SelectTileQualities(selectedQualities);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[3] = nQualities - 1;
                prerecordedPointcloud.SelectTileQualities(selectedQualities);
            }

        }
        void getTilesAlwaysBest(double[] a1, double[] a2, double[] a3, double[] a4, double budget)
        {
            int[] selectedQualities = new int[nTiles];
            
            for (int i = 0; i < nTiles; i++) selectedQualities[i] = nQualities - 1;
             prerecordedPointcloud.SelectTileQualities(selectedQualities);
        }
        void getTilesFrontTileBest(double[] a1, double[] a2, double[] a3, double[] a4, double budget)
        {
            getTileOrder();
            int[] selectedQualities = new int[nTiles];
            for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
            selectedQualities[tileOrder[0]] = nQualities - 1;
            prerecordedPointcloud.SelectTileQualities(selectedQualities);
        }
        void getTileOrder()
        {
            tileOrder = new int[4];
            //Initialize index array
            for (int i = 0; i < 4; i++) tileOrder[i] = i;
            tileUtilities = new float[4];
            //Set tile utilities
            tileUtilities[0] = Vector3.Dot(cameraForward, TileC1);
            tileUtilities[1] = Vector3.Dot(cameraForward, TileC2);
            tileUtilities[2] = Vector3.Dot(cameraForward, TileC3);
            tileUtilities[3] = Vector3.Dot(cameraForward, TileC4);
            t1Utility = tileUtilities[0];
            t2Utility = tileUtilities[1];
            t3Utility = tileUtilities[2];
            t4Utility = tileUtilities[3];
            //Sort tile utilities and apply the same sort to tileOrder
            Array.Sort(tileUtilities, tileOrder);
            //The tile vectors represent the camera that sees the tile not the orientation of tile surface (ie dot product of 1 is highest utility tile, dot product of -1 is the lowest utility tile)
            Array.Reverse(tileOrder);
        }
    }
}
