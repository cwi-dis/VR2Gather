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
        int[] selectedQualities;

        public void Init(PrerecordedPointcloud _prerecordedPointcloud, int _nQualities, int _nTiles)
        {
            prerecordedPointcloud = _prerecordedPointcloud;
            nQualities = _nQualities;
            nTiles = _nTiles;
            Debug.Log($"xxxjack PrerecordedTileSelector nQualities={nQualities}, nTiles={nTiles}");
            selectedQualities = new int[nTiles];
        }
        private void Update()
        {
            //Debug.Log($"xxxjack PrerecordedPointcloud update called");
            if (selectedQualities == null)
            {
                // Not yet initialized
                return;
            }
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
    }
}
