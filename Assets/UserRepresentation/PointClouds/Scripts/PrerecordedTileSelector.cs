using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        public enum SelectionAlgorithm { interactive, alwaysBest, frontTileBest, greedy, uniform, hybrid };
        public SelectionAlgorithm algorithm = SelectionAlgorithm.interactive; // xxxjack to be determined by overall controller

        //xxxshishir tile selector stuff ToDo Check with Jack before refactor
        public double bitRatebudget;
        private double savings;
        private bool[] tileVisibility;
        private Transform cameraTransform;
        private Vector3 cameraForward;
        private int[] tileOrder;
        private float[] tileUtilities;
        private float t1Utility;
        private float t2Utility;
        private float t3Utility;
        private float t4Utility;
        private Vector3 TileC1 = new Vector3(0, 0, -1);
        private Vector3 TileC2 = new Vector3(1, 0, 0);
        private Vector3 TileC3 = new Vector3(0, 0, 1);
        private Vector3 TileC4 = new Vector3(-1, 0, 0);
        //Adaptation Variables ToDo Refactor
        private List<adaptationSet> [] aTile = new List<adaptationSet>[4];

        public static long curIndex;

        string Name()
        {
            return "PrerecordedTileSelector";
        }
        //xxxshishir adaptation set struct
        public struct adaptationSet
        {
            public string PCframe;
            //public double[] encodedSize;
            public List<double> encodedSize;
            public void addEncodedSize(double a, int i)
            {
                if (encodedSize == null)
                    encodedSize = new List<double>();
                if ((encodedSize.Count - 1) < i)
                    encodedSize.Add(a);
                else
                    encodedSize[i] = a;
            }
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

            //xxxshishir load the tile description csv files
            string rootFolder = Config.Instance.LocalUser.PCSelfConfig.PrerecordedReaderConfig.folder;
            string [] tileFolder = Config.Instance.LocalUser.PCSelfConfig.PrerecordedReaderConfig.tiles;
            for(int i=0;i < aTile.Length;i++)
            {
                aTile[i] = new List<adaptationSet>();
                FileInfo tileDescFile = new FileInfo(System.IO.Path.Combine(rootFolder, tileFolder[i], "tiledescription.csv"));
                if(!tileDescFile.Exists)
                    Debug.LogError("Tile description not found for tile "+ i + " at" + System.IO.Path.Combine(rootFolder, tileFolder[i], "tiledescription.csv"));
                StreamReader tileDescReader = tileDescFile.OpenText();
                //Skip header
                var aLine = tileDescReader.ReadLine();
                adaptationSet aFrame = new adaptationSet();
                while ((aLine = tileDescReader.ReadLine()) != null)
                {                  
                    var aLineValues = aLine.Split(',');
                    aFrame.PCframe = aLineValues[0];
                    for(int j =1;j<aLineValues.Length;j++)
                    {
                        aFrame.addEncodedSize(double.Parse(aLineValues[j]), j - 1);
                    }
                    aTile[i].Add(aFrame);
                    aFrame = new adaptationSet();
                }
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
            double[] a1 = aTile[0][(int)curIndex].encodedSize.ToArray();
            double[] a2 = aTile[1][(int)curIndex].encodedSize.ToArray();
            double[] a3 = aTile[2][(int)curIndex].encodedSize.ToArray();
            double[] a4 = aTile[3][(int)curIndex].encodedSize.ToArray();
            //xxxshishir debug code
            if (a1 == null)
            {
                Debug.Log("<color=red> Current Index </color> " + curIndex);
                a1 = aTile[0][0].encodedSize.ToArray();
            }
            if (a2 == null)
                a2 = aTile[1][0].encodedSize.ToArray();
            if (a3 == null)
                a3 = aTile[2][0].encodedSize.ToArray();
            if (a4 == null)
                a4 = aTile[3][0].encodedSize.ToArray();
            double budget = bitRatebudget;
            if (budget == 0) budget = 100000;
            //xxxshishir get camera orientation ToDo: Move to getTileOrder ?
            var cam = FindObjectOfType<Camera>().gameObject;
            if (cam == null)
                Debug.LogError("Camera not found!");
            cameraTransform = cam.transform;
            cameraForward = cameraTransform.forward;
            //Debug.Log("<color=red> Camera Transform </color>" + cameraForward.x + " " + cameraForward.y + " " + cameraForward.z);
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
                case SelectionAlgorithm.greedy:
                    getTilesGreedy(a1, a2, a3, a4, budget);
                    break;
                case SelectionAlgorithm.uniform:
                    getTilesUniform(a1, a2, a3, a4, budget);
                    break;
                case SelectionAlgorithm.hybrid:
                    getTilesHybrid(a1, a2, a3, a4, budget);
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

        //xxxshishir actual tile selection strategies used for evaluation
        void getTilesGreedy(double[] a1, double[] a2, double[] a3, double[] a4, double budget)
        {
            getTileOrder();
            int[] Tiles = new int[4];
            Tiles[0] = 0;
            Tiles[1] = 0;
            Tiles[2] = 0;
            Tiles[3] = 0;
            double[][] adaptationSet = new double[nTiles][];
            adaptationSet[0] = a1;
            adaptationSet[1] = a2;
            adaptationSet[2] = a3;
            adaptationSet[3] = a4;
            double spent;
            spent = a1[0] + a2[0] + a3[0] + a4[0];
            double nextSpend;
            bool representationSet = false;
            bool stepComplete = false;
            while (representationSet != true)
            {
                stepComplete = false;
                for (int i = 0; i < 4; i++)
                {
                    if (Tiles[tileOrder[i]] < (a1.Length - 1))
                    {
                        nextSpend = adaptationSet[tileOrder[i]][(Tiles[tileOrder[i]] + 1)] - adaptationSet[tileOrder[i]][Tiles[tileOrder[i]]];
                        if ((spent + nextSpend) <= budget)
                        {
                            Tiles[tileOrder[i]]++;
                            stepComplete = true;
                            spent = spent + nextSpend;
                            break;
                        }
                    }
                }
                if (stepComplete == false)
                {
                    representationSet = true;
                    savings = budget - spent;
                    // UnityEngine.Debug.Log("<color=green> XXXDebug Budget" + budget + " spent " + spent + " savings " + savings + " </color> ");
                }
            }
            prerecordedPointcloud.SelectTileQualities(Tiles);
        }
        void getTilesUniform(double[] a1, double[] a2, double[] a3, double[] a4, double budget)
        {
            getTileOrder();
            int[] Tiles = new int[4];
            Tiles[0] = 0;
            Tiles[1] = 0;
            Tiles[2] = 0;
            Tiles[3] = 0;
            double[][] adaptationSet = new double[nTiles][];
            adaptationSet[0] = a1;
            adaptationSet[1] = a2;
            adaptationSet[2] = a3;
            adaptationSet[3] = a4;
            double spent;
            spent = a1[0] + a2[0] + a3[0] + a4[0];
            double nextSpend;
            bool representationSet = false;
            bool stepComplete = false;
            while (representationSet != true)
            {
                stepComplete = false;
                for (int i = 0; i < 4; i++)
                {
                    if (Tiles[tileOrder[i]] < (a1.Length - 1))
                    {
                        nextSpend = adaptationSet[tileOrder[i]][(Tiles[tileOrder[i]] + 1)] - adaptationSet[tileOrder[i]][Tiles[tileOrder[i]]];
                        if ((spent + nextSpend) <= budget)
                        {
                            Tiles[tileOrder[i]]++;
                            stepComplete = true;
                            spent = spent + nextSpend;
                        }
                    }

                }
                if (stepComplete == false)
                {
                    representationSet = true;
                    savings = budget - spent;
                }
            }
            prerecordedPointcloud.SelectTileQualities(Tiles);
        }
        void getTilesHybrid(double[] a1, double[] a2, double[] a3, double[] a4, double budget)
        {
            getTileOrder();
            getTileVisibility();
            int[] Tiles = new int[4];
            Tiles[0] = 0;
            Tiles[1] = 0;
            Tiles[2] = 0;
            Tiles[3] = 0;
            double[][] adaptationSet = new double[nTiles][];
            adaptationSet[0] = a1;
            adaptationSet[1] = a2;
            adaptationSet[2] = a3;
            adaptationSet[3] = a4;
            double spent;
            spent = a1[0] + a2[0] + a3[0] + a4[0];
            double nextSpend;
            bool representationSet = false;
            bool stepComplete = false;
            while (representationSet != true)
            {
                stepComplete = false;
                for (int i = 0; i < 4; i++)
                {
                    if (Tiles[tileOrder[i]] < (a1.Length - 1))
                    {
                        nextSpend = adaptationSet[tileOrder[i]][(Tiles[tileOrder[i]] + 1)] - adaptationSet[tileOrder[i]][Tiles[tileOrder[i]]];
                        if ((spent + nextSpend) <= budget && tileVisibility[tileOrder[i]] == true)
                        {
                            Tiles[tileOrder[i]]++;
                            stepComplete = true;
                            spent = spent + nextSpend;
                        }
                    }

                }
                //Increse representation of tiles facing away from the user if the visible tiles are already maxed
                if (stepComplete == false)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (Tiles[tileOrder[i]] < (a1.Length - 1))
                        {
                            nextSpend = adaptationSet[tileOrder[i]][(Tiles[tileOrder[i]] + 1)] - adaptationSet[tileOrder[i]][Tiles[tileOrder[i]]];
                            if ((spent + nextSpend) < budget && tileVisibility[tileOrder[i]] == false)
                            {
                                Tiles[tileOrder[i]]++;
                                stepComplete = true;
                                spent = spent + nextSpend;
                            }
                        }

                    }
                }
                if (stepComplete == false)
                {
                    representationSet = true;
                    savings = budget - spent;
                }
            }
            prerecordedPointcloud.SelectTileQualities(Tiles);
        }
        void getTileVisibility()
        {
            tileVisibility = new bool[4];
            tileVisibility[0] = false;
            tileVisibility[1] = false;
            tileVisibility[2] = false;
            tileVisibility[3] = false;
            //Tiles with dot product > 0 have the tile cameras facing in the same direction as the current scene camera (Note: TileC1-C4 contain the orientation of tile cameras NOT tile surfaces)
            if (Vector3.Dot(cameraForward, TileC1) > 0)
                tileVisibility[0] = true;
            if (Vector3.Dot(cameraForward, TileC2) > 0)
                tileVisibility[1] = true;
            if (Vector3.Dot(cameraForward, TileC3) > 0)
                tileVisibility[2] = true;
            if (Vector3.Dot(cameraForward, TileC4) > 0)
                tileVisibility[3] = true;
        }
        public void setCamera(Vector3 Orientation)
        {
            cameraForward = Orientation;
        }
        public void setBudget(double budget)
        {
            bitRatebudget = budget;
        }
    }
}
