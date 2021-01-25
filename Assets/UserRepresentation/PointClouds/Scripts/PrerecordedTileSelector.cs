using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.UserRepresentation.Voice;
using VRTCore;
using VRT.Core;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;

namespace VRT.UserRepresentation.PointCloud
{
    public class PrerecordedTileSelector : MonoBehaviour
    {
        PrerecordedPointcloud prerecordedPointcloud;
 

        int nQualities;
        int nTiles;
        public enum SelectionAlgorithm { interactive, alwaysBest };
        public SelectionAlgorithm algorithm = SelectionAlgorithm.interactive; // xxxjack to be determined by overall controller

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

            switch (algorithm)
            {
                case SelectionAlgorithm.interactive:
                    getTilesInteractive(a1, a2, a3, a4, budget);
                    break;
                case SelectionAlgorithm.alwaysBest:
                    getTilesAlwaysBest(a1, a2, a3, a4, budget);
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
    }
}
