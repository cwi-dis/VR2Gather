using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.UserRepresentation.Voice;
using VRT.Core;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;
using System;
using System.Linq;

namespace VRT.UserRepresentation.PointCloud
{
    public abstract class BaseTileSelector : MonoBehaviour
    {
        // Object where we send our quality selection decisions. Initialized by subclass.
        protected PointCloudPipeline pipeline;

        // Number of tiles available. Initialized by subclass.
        protected int nTiles;

        // Number of qualities available per tile. Initialized by subclass.
        protected int nQualities;

        // Where the individual tiles are facing. Initialized by subclass.
        protected Vector3[] TileOrientation;

        // Most-recently selected tile qualities (if non-null)
        protected int[] previousSelectedTileQualities;

        public enum SelectionAlgorithm { interactive, alwaysBest, frontTileBest, greedy, uniform, hybrid };
        //
        // Set by overall controller (in production): which algorithm to use for this scene run.
        //
        public SelectionAlgorithm algorithm = SelectionAlgorithm.interactive;
        //
        // Can be set (in scene editor) to print all decision made by the algorithms.
        //
        public bool debugDecisions = false;
        //Keep track of stimuli being played back
        public static int stimuliIndex = -1;
        public static string[] stimuliList;
        public string currentStimuli;
        //
        // Temporary public variable, set by PrerecordedReader: next pointcloud we are expecting to show.
        //
        public static long curIndex;
        //Adaptation gap limiter
        public int maxAdaptation = 30;
        string Name()
        {
            return "BaseTileSelector";
        }

        //
        // Get the per-tile per-quality bandwidth usage matrix for the current frame.
        // Must be implemented in subclass.
        //
        abstract protected double[][] getBandwidthUsageMatrix(long currentFrameNumber);

        //
        // Get current frame number or timestamp or whatever.
        // To be implemented by subclass
        //
        abstract protected long getCurrentFrameIndex();

        //
        // Get current total badnwidth available for this pointcloud.
        // To be implemented by subclass.
        //
        abstract protected double getBitrateBudget();

        //
        // Get viewer forward-fsacing vector.
        // To be implemented by subclass.
        //
        protected abstract Vector3 getCameraForward();

        //
        // Get best known position for viewed pointcloud.
        // To be implemented by subclass.
        //
        protected abstract Vector3 getPointcloudPosition(long currentFrameNumber);

        private void Update()
        {
            // Debug.Log($"{Name()}: xxxjack update called");
            if (pipeline == null)
            {
                // Not yet initialized
                Debug.LogWarning($"{Name()}: Update() called, but no pipeline set yet");
                return;
            }
            long currentFrameIndex = getCurrentFrameIndex();
            double[][] bandwidthUsageMatrix = getBandwidthUsageMatrix(currentFrameIndex);
            if (bandwidthUsageMatrix == null)
            {
                // Not yet initialized
                Debug.LogWarning($"{Name()}: Update() called, but no bandwidthUsageMatrix set yet");
                return;
            }
            double budget = getBitrateBudget();
            if (budget == 0) budget = 100000;
            Vector3 cameraForward = getCameraForward();
            Vector3 pointcloudPosition = getPointcloudPosition(currentFrameIndex);
            int[] selectedTileQualities = getTileQualities(bandwidthUsageMatrix, budget, cameraForward, pointcloudPosition);
            bool changed = previousSelectedTileQualities == null;
            if (!changed && selectedTileQualities != null)
            {
                for(int i=0; i != selectedTileQualities.Length; i++)
                {
                    if (selectedTileQualities[i] != previousSelectedTileQualities[i])
                    {
                        changed = true;
                    }
                }
            }
            if (changed && selectedTileQualities != null && debugDecisions)
            {
                // xxxjack: we could do this in stats: format too, may help analysis.
                Debug.Log($"Name(): tileQualities: {String.Join(", ", selectedTileQualities)}");
            }
            pipeline.SelectTileQualities(selectedTileQualities);
            previousSelectedTileQualities = selectedTileQualities;
            string statMsg = $"currentstimuli={currentStimuli}, currentFrame={curIndex}, tile0={selectedTileQualities[0]},";
            for(int i=1; i<selectedTileQualities.Length; i++)
            {
                statMsg += $", tile{i}={selectedTileQualities[i]}";
            }
            BaseStats.Output(Name(), statMsg);

            pipeline.SelectTileQualities(selectedTileQualities);
            previousSelectedTileQualities = selectedTileQualities;
            //
            // Check whether the user wants to leave the scene (by pressing escape)
            //
            float rightTrigger = Input.GetAxisRaw("PrimaryTriggerRight");
            float leftTrigger = Input.GetAxisRaw("PrimaryTriggerLeft");
            if (Input.GetKeyDown(KeyCode.Escape) || leftTrigger >= 0.8f)
            {
                //SceneManager.LoadScene("QualityAssesmentRatingScene", LoadSceneMode.Additive);
                SceneManager.LoadScene("QualityAssesmentRatingScene");
            }
        }

        public virtual int[] getTileOrder(Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            int[] tileOrder = new int[nTiles];
            //Initialize index array
            for (int i = 0; i < nTiles; i++)
            {
                tileOrder[i] = i;
            }
            float[] tileUtilities = new float[nTiles];
            for (int i = 0; i < nTiles; i++)
            {
                tileUtilities[i] = Vector3.Dot(cameraForward, TileOrientation[i]);
            }
            //Sort tile utilities and apply the same sort to tileOrder
            Array.Sort(tileUtilities, tileOrder);
            //The tile vectors represent the camera that sees the tile not the orientation of tile surface (ie dot product of 1 is highest utility tile, dot product of -1 is the lowest utility tile)
            Array.Reverse(tileOrder);
            return tileOrder;
        }

        // Get array of per-tile quality wanted, based on current timestamp/framenumber, budget
        // and algorithm
        int[] getTileQualities(double[][] bandwidthUsageMatrix, double budget, Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            switch (algorithm)
            {
                case SelectionAlgorithm.interactive:
                    return getTileQualities_Interactive(bandwidthUsageMatrix, budget, cameraForward, pointcloudPosition);
                case SelectionAlgorithm.alwaysBest:
                    return getTileQualities_AlwaysBest(bandwidthUsageMatrix, budget, cameraForward, pointcloudPosition);
                case SelectionAlgorithm.frontTileBest:
                    return getTilesFrontTileBest(bandwidthUsageMatrix, budget, cameraForward, pointcloudPosition);
                case SelectionAlgorithm.greedy:
                    return getTileQualities_Greedy(bandwidthUsageMatrix, budget, cameraForward, pointcloudPosition);
                case SelectionAlgorithm.uniform:
                    return getTileQualities_Uniform(bandwidthUsageMatrix, budget, cameraForward, pointcloudPosition);
                case SelectionAlgorithm.hybrid:
                    return getTileQualities_Hybrid(bandwidthUsageMatrix, budget, cameraForward, pointcloudPosition);
                default:
                    Debug.LogError($"{Name()}: Unknown algorithm");
                    return null;
            }
        }

        int[] getTileQualities_Interactive(double[][] bandwidthUsageMatrix, double budget, Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            int[] selectedQualities = new int[nTiles];
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = nQualities - 1;
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[0] = nQualities - 1;
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[1] = nQualities - 1;
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[2] = nQualities - 1;
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[3] = nQualities - 1;
                return selectedQualities;
            }
            return null;
        }
        int[] getTileQualities_AlwaysBest(double[][] bandwidthUsageMatrix, double budget, Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            int[] selectedQualities = new int[nTiles];

            for (int i = 0; i < nTiles; i++) selectedQualities[i] = nQualities - 1;
            return selectedQualities;
        }
        int[] getTilesFrontTileBest(double[][] bandwidthUsageMatrix, double budget, Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            int[] tileOrder = getTileOrder(cameraForward, pointcloudPosition);
            int[] selectedQualities = new int[nTiles];
            for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
            selectedQualities[tileOrder[0]] = nQualities - 1;
            return selectedQualities;
        }

        //xxxshishir actual tile selection strategies used for evaluation
        int[] getTileQualities_Greedy(double[][] bandwidthUsageMatrix, double budget, Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            double spent = 0;
            int[] tileOrder = getTileOrder(cameraForward, pointcloudPosition);
            // Start by selecting minimal quality for each tile
            int[] selectedQualities = new int[nTiles];
            // Assume we spend at least minimal quality badnwidth requirements for each tile
            for (int i = 0; i < nTiles; i++) spent += bandwidthUsageMatrix[i][0];
            bool representationSet = false;
            bool stepComplete = false;
            while (!representationSet)
            {
                stepComplete = false;
                for (int i = 0; i < nTiles; i++)
                {
                    if (selectedQualities[tileOrder[i]] < nQualities - 1)
                    {
                        double nextSpend = bandwidthUsageMatrix[tileOrder[i]][(selectedQualities[tileOrder[i]] + 1)] - bandwidthUsageMatrix[tileOrder[i]][selectedQualities[tileOrder[i]]];
                        if ((spent + nextSpend) <= budget)
                        {
                            selectedQualities[tileOrder[i]]++;
                            stepComplete = true;
                            spent = spent + nextSpend;
                            break;
                        }
                    }
                }
                if (!stepComplete)
                {
                    representationSet = true;
                    double savings = budget - spent;
                    // UnityEngine.Debug.Log("<color=green> XXXDebug Budget" + budget + " spent " + spent + " savings " + savings + " </color> ");
                }
            }
            return selectedQualities;
        }
        int[] getTileQualities_Uniform(double[][] bandwidthUsageMatrix, double budget, Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            double spent = 0;
            int[] tileOrder = getTileOrder(cameraForward, pointcloudPosition);
            // Start by selecting minimal quality for each tile
            int[] selectedQualities = new int[nTiles];
            // Assume we spend at least minimal quality badnwidth requirements for each tile
            for (int i = 0; i < nTiles; i++) spent += bandwidthUsageMatrix[i][0];
            bool representationSet = false;
            bool stepComplete = false;
            while (representationSet != true)
            {
                stepComplete = false;
                for (int i = 0; i < nTiles; i++)
                {
                    if (selectedQualities[tileOrder[i]] < (nQualities - 1))
                    {
                        double nextSpend = bandwidthUsageMatrix[tileOrder[i]][(selectedQualities[tileOrder[i]] + 1)] - bandwidthUsageMatrix[tileOrder[i]][selectedQualities[tileOrder[i]]];
                        if ((spent + nextSpend) <= budget)
                        {
                            selectedQualities[tileOrder[i]]++;
                            stepComplete = true;
                            spent = spent + nextSpend;
                        }
                    }

                }
                if (stepComplete == false)
                {
                    representationSet = true;
                    double savings = budget - spent;
                }
            }
            return selectedQualities;
        }
        int[] getTileQualities_Hybrid(double[][] bandwidthUsageMatrix, double budget, Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            bool[] tileVisibility = getTileVisibility(cameraForward, pointcloudPosition);
            double spent = 0;
            int[] tileOrder = getTileOrder(cameraForward, pointcloudPosition);
            // Start by selecting minimal quality for each tile
            int[] selectedQualities = new int[nTiles];
            // Assume we spend at least minimal quality badnwidth requirements for each tile
            for (int i = 0; i < nTiles; i++) spent += bandwidthUsageMatrix[i][0];
            bool representationSet = false;
            bool stepComplete = false;
            while (representationSet != true)
            {
                stepComplete = false;
                for (int i = 0; i < nTiles; i++)
                {
                    if (selectedQualities[tileOrder[i]] < (nQualities - 1))
                    {
                        double nextSpend = bandwidthUsageMatrix[tileOrder[i]][(selectedQualities[tileOrder[i]] + 1)] - bandwidthUsageMatrix[tileOrder[i]][selectedQualities[tileOrder[i]]];
                        if ((spent + nextSpend) <= budget && tileVisibility[tileOrder[i]] == true && (selectedQualities[tileOrder[i]] - selectedQualities.Min()) < maxAdaptation)
                        {
                            selectedQualities[tileOrder[i]]++;
                            stepComplete = true;
                            spent = spent + nextSpend;
                        }
                    }

                }
                //Increse representation of tiles facing away from the user if the visible tiles are already maxed
                if (stepComplete == false)
                {
                    for (int i = 0; i < nTiles; i++)
                    {
                        if (selectedQualities[tileOrder[i]] < (nQualities - 1))
                        {
                            double nextSpend = bandwidthUsageMatrix[tileOrder[i]][(selectedQualities[tileOrder[i]] + 1)] - bandwidthUsageMatrix[tileOrder[i]][selectedQualities[tileOrder[i]]];
                            if ((spent + nextSpend) < budget && tileVisibility[tileOrder[i]] == false && (selectedQualities[tileOrder[i]] - selectedQualities.Min()) < maxAdaptation)
                            {
                                selectedQualities[tileOrder[i]]++;
                                stepComplete = true;
                                spent = spent + nextSpend;
                            }
                        }

                    }
                }
                if (stepComplete == false)
                {
                    representationSet = true;
                    double savings = budget - spent;
                }
            }
            return selectedQualities;
        }
        bool[] getTileVisibility(Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            // xxxjack currently ignores pointcloud position, which is probably wrong...
            bool[] tileVisibility = new bool[nTiles];
            //Tiles with dot product > 0 have the tile cameras facing in the same direction as the current scene camera (Note: TileC1-C4 contain the orientation of tile cameras NOT tile surfaces)
            for (int i = 0; i < nTiles; i++)
            {
                tileVisibility[i] = Vector3.Dot(cameraForward, TileOrientation[i]) > 0;
            }
            return tileVisibility;
        }

    }

    public class PrerecordedTileSelector : BaseTileSelector
    {

        //
        // Datastructure that contains all bandwidth data (per-tile, per-sequencenumber, per-quality bitrate usage)
        //
        List<AdaptationSet>[] prerecordedTileAdaptationSets = null;
        //
        // Set by overall controller (in production): total available bitrate for this run.
        //
        public double bitRatebudget = 1000000;
        //
        //Datastructure that contains tile geometry data (per-tile, per-sequencenumber, per-frame tile geometry information)
        //
        List<TileGeometry>[] prerecordedTileGeometrySets = null;
        //
        // Randomized rotation angle used for each stimuli
        //
        private float yRotation;

        string Name()
        {
            return "PrerecordedTileSelector";
        }

        //xxxshishir adaptation set struct
        public struct AdaptationSet
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
        //xxxshishir Struct to store tile geometry information
        public struct TileGeometry
        {
            public string PCframe;
            public float PCCentroidX;
            public float PCCentroidY;
            public float PCCentroidZ;
            public float PCBBXmin;
            public float PCBBYmin;
            public float PCBBZmin;
            public float PCBBXmax;
            public float PCBBYmax;
            public float PCBBZmax;
            public float PCBBXCentroid;
            public float PCBBYCentroid;
            public float PCBBZCentroid;
        }

        public void Init(PointCloudPipeline _prerecordedPointcloud, int _nQualities, int _nTiles, TilingConfig? tilingConfig)
        {
            if (tilingConfig != null)
            {
                throw new System.Exception($"{Name()}: Cannot handle tilingConfig argument");
            }
            //xxxshishir randomize initial orientation of prerecorded pointcloud
            var prerecordedGameObject = GameObject.Find("PrerecordedPosition");
            yRotation = UnityEngine.Random.Range(0, 360);
            prerecordedGameObject.transform.Rotate(0.0f, yRotation, 0.0f, Space.World);
            string statMsg = $"currentstimuli={currentStimuli}, currentFrame={curIndex}, InitialRotation={yRotation}";
            BaseStats.Output(Name(), statMsg);

            pipeline = _prerecordedPointcloud;
            nQualities = _nQualities;
            Debug.Log($"{Name()}: PrerecordedTileSelector nQualities={nQualities}, nTiles={nTiles}");
            nTiles = _nTiles;
            TileOrientation = new Vector3[nTiles];
            for (int ti = 0; ti < nTiles; ti++)
            {
                double angle = ti * Math.PI / 2;
                Vector3 InitalVector = new Vector3((float)Math.Sin(angle), 0, (float)-Math.Cos(angle));
                TileOrientation[ti] = Quaternion.Euler(0.0f, yRotation, 0.0f) * InitalVector;
                TileOrientation[ti] = Vector3.Normalize(TileOrientation[ti]);
                //TileOrientation[ti] = new Vector3((float)Math.Sin(angle), 0, (float)-Math.Cos(angle));
            }
            string statsMsg = $"currentstimuli={currentStimuli}, currentFrame={curIndex}, Orientationtile0={TileOrientation[0]},";
            for (int i = 1; i < nTiles; i++)
            {
                statsMsg += $", Orientationtile{i}={TileOrientation[i]}";
            }
            BaseStats.Output(Name(), statsMsg);

            LoadAdaptationSets();
        }

        //
        // Load the adaptationSet data: per-frame, per-tile, per-quality bandwidth usage.
        // This data is read from (per-tile) CSV files, which have a row per frame and a column
        // per quality level.
        //
        // Note: The location of the files is obtained from the configfile instance (it would be better
        // design to get this from our parent PrerecordedPointcloud, as this would allow for showing
        // multiple prerecorded pointclouds at the same time).
        //
        private void LoadAdaptationSets()
        {
            prerecordedTileAdaptationSets = new List<AdaptationSet>[nTiles];
            prerecordedTileGeometrySets = new List<TileGeometry>[nTiles];
            Config._User realUser = Config.Instance.LocalUser;
            stimuliIndex++;
            stimuliList = Config.Instance.stimuliList;
            if (stimuliIndex == stimuliList.Length)
                stimuliIndex = 0;
            currentStimuli = stimuliList[stimuliIndex];
            int dataset = int.Parse(currentStimuli[1].ToString());
            int codec = int.Parse(currentStimuli[4].ToString());
            int ratepoint = int.Parse(currentStimuli[7].ToString());
            maxAdaptation = Config.Instance.maxAdaptation;
            realUser.PCSelfConfig.PrerecordedReaderConfig.folder = System.IO.Path.Combine(Config.Instance.rootFolder, "H" + dataset.ToString());

            switch (codec)
            {
                case 3:
                    algorithm = SelectionAlgorithm.greedy;
                    break;
                case 4:
                    algorithm = SelectionAlgorithm.hybrid;
                    break;
                case 5:
                    algorithm = SelectionAlgorithm.uniform;
                    break;
            }
            switch (ratepoint)
            {
                case 1:
                    bitRatebudget = 19287;
                    break;
                case 2:
                    bitRatebudget = 58896;
                    break;
                case 3:
                    bitRatebudget = 196215;
                    break;
                case 4:
                    bitRatebudget = 516196;
                    break;
                default:
                    bitRatebudget = 100000;
                    break;
            }
            //xxxshishir load the tile description csv files
            string rootFolder = Config.Instance.LocalUser.PCSelfConfig.PrerecordedReaderConfig.folder;
            string[] tileFolder = Config.Instance.LocalUser.PCSelfConfig.PrerecordedReaderConfig.tiles;
            for (int i = 0; i < prerecordedTileAdaptationSets.Length; i++)
            {
                string csvFilename = System.IO.Path.Combine(rootFolder, tileFolder[i], "tiledescription.csv");
                prerecordedTileAdaptationSets[i] = new List<AdaptationSet>();
                FileInfo tileDescFile = new FileInfo(csvFilename);
                if (!tileDescFile.Exists)
                {
                    prerecordedTileAdaptationSets = null; // Delete tile datastructure to forestall further errors
                    throw new System.Exception($"Tile description not found for tile {i} at {csvFilename}");
                }
                StreamReader tileDescReader = tileDescFile.OpenText();
                //Skip header
                var aLine = tileDescReader.ReadLine();
                AdaptationSet aFrame = new AdaptationSet();
                // Check that all lines have the same number of fields
                int numfields = -1;
                while ((aLine = tileDescReader.ReadLine()) != null)
                {
                    var aLineValues = aLine.Split(',');
                    aFrame.PCframe = aLineValues[0];
                    if (numfields < 0) numfields = aLineValues.Length;
                    if (aLineValues.Length != numfields)
                    {
                        throw new System.Exception($"{Name()}: some lines have {numfields} fields, others have {aLineValues.Length} in {csvFilename}");
                    }
                    for (int j = 1; j < aLineValues.Length; j++)
                    {
                        aFrame.addEncodedSize(double.Parse(aLineValues[j]), j - 1);
                    }
                    prerecordedTileAdaptationSets[i].Add(aFrame);
                    aFrame = new AdaptationSet();
                }
            }
            //xxxshishir load the tile geometry csv files
            for (int i = 0; i < prerecordedTileGeometrySets.Length; i++)
            {
                string csvFilename = System.IO.Path.Combine(rootFolder, tileFolder[i], "tilegeometry.csv");
                prerecordedTileGeometrySets[i] = new List<TileGeometry>();
                FileInfo tileDescFile = new FileInfo(csvFilename);
                if (!tileDescFile.Exists)
                {
                    prerecordedTileGeometrySets = null; // Delete tile geometry datastructure to forestall further errors
                    throw new System.Exception($"Tile geometry description not found for tile {i} at {csvFilename}");
                }
                StreamReader tileDescReader = tileDescFile.OpenText();
                //Skip header
                var gLine = tileDescReader.ReadLine();
                TileGeometry gFrame = new TileGeometry();
                while ((gLine = tileDescReader.ReadLine()) != null)
                {
                    var gLineValues = gLine.Split(',');
                    gFrame.PCframe = gLineValues[0];
                    gFrame.PCCentroidX = float.Parse(gLineValues[1]);
                    gFrame.PCCentroidY = float.Parse(gLineValues[2]);
                    gFrame.PCCentroidZ = float.Parse(gLineValues[3]);
                    gFrame.PCBBXmin = float.Parse(gLineValues[4]);
                    gFrame.PCBBYmin = float.Parse(gLineValues[5]);
                    gFrame.PCBBZmin = float.Parse(gLineValues[6]);
                    gFrame.PCBBXmax = float.Parse(gLineValues[7]);
                    gFrame.PCBBYmax = float.Parse(gLineValues[8]);
                    gFrame.PCBBZmax = float.Parse(gLineValues[9]);
                    gFrame.PCBBXCentroid = float.Parse(gLineValues[10]);
                    gFrame.PCBBYCentroid = float.Parse(gLineValues[11]);
                    gFrame.PCBBZCentroid = float.Parse(gLineValues[12]);
                    prerecordedTileGeometrySets[i].Add(gFrame);
                    gFrame = new TileGeometry();
                }
            }

        }

        protected override double[][] getBandwidthUsageMatrix(long currentFrameNumber)
        {
            //Debug.Log($"xxxjack frameNumber={currentFrameNumber}");
            double[][] bandwidthUsageMatrix = new double[nTiles][];
            for (int ti = 0; ti < nTiles; ti++)
            {
                var thisTile = prerecordedTileAdaptationSets[ti];
                var thisFrame = thisTile[(int)currentFrameNumber];
                var thisBandwidth = thisFrame.encodedSize.ToArray();
                bandwidthUsageMatrix[ti] = thisBandwidth;
            }
            return bandwidthUsageMatrix;
        }
        protected override double getBitrateBudget()
        {
            return bitRatebudget;
        }
        protected override long getCurrentFrameIndex()
        {
            return curIndex;
        }
        protected override Vector3 getCameraForward()
        {
            // xxxjack currently returns camera viedw angle (as the name implies)
            // but maybe camera position is better. Or both.
            var cam = FindObjectOfType<Camera>().gameObject;
            if (cam == null)
                Debug.LogError("Camera not found!");
            //Debug.Log("<color=red> Camera Transform </color>" + cameraForward.x + " " + cameraForward.y + " " + cameraForward.z);
            Transform cameraTransform = cameraTransform = cam.transform;
            return cameraTransform.forward;

        }

        public Vector3 getCameraPosition()
        {
            // xxxjack currently returns camera viedw angle (as the name implies)
            // but maybe camera position is better. Or both.
            var cam = FindObjectOfType<Camera>().gameObject;
            if (cam == null)
                Debug.LogError("Camera not found!");
            Transform cameraTransform = cam.transform;
            return cameraTransform.position;

        }

        protected override Vector3 getPointcloudPosition(long currentFrameNumber)
        {
            return new Vector3(0, 0, 0);
        }

        public override int[] getTileOrder(Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            int[] tileOrder = new int[nTiles];
            int[] tileOrderDistances = new int[nTiles];
            //Initialize index array
            for (int i = 0; i < nTiles; i++)
            {
                tileOrder[i] = i;
                tileOrderDistances[i] = i;
            }
            float[] tileUtilities = new float[nTiles];
            for (int i = 0; i < nTiles; i++)
            {
                tileUtilities[i] = Vector3.Dot(cameraForward, TileOrientation[i]);
            }
            //Reassign the utility values based on distance to tiles
            float[] tileDistances = new float[nTiles];
            Vector3 camPosition = new Vector3();
            camPosition = getCameraPosition();
            Vector3[] tileLocations = new Vector3[nTiles];
            for (int i =0;i < nTiles; i++)
            {
                var thisTile = prerecordedTileGeometrySets[i];
                var thisFrame = thisTile[(int)curIndex];
                Vector3 tilePosition = new Vector3(thisFrame.PCBBXCentroid, thisFrame.PCBBYCentroid, thisFrame.PCBBZCentroid);
                //xxxshishir trying to apply the randomly generated initial rotation to the tile bounding box centroid to compute the correct distances from camera to tile centroid
                tilePosition = Quaternion.Euler(0.0f, yRotation, 0.0f) * tilePosition;
                tileDistances[i] = Vector3.Distance(camPosition, tilePosition);
            }
            //Modify utility values so the sign is determined by the distance
            float[] hybridTileUtilities = new float[nTiles];
            for (int i=0;i<nTiles;i++)
            {
                hybridTileUtilities[i] = -1 * Math.Abs(tileUtilities[i]);
            }
            for(int i=0;i<nTiles;i++)
            {
                for(int j =0;j<nTiles;j++)
                {
                    if (i != j && hybridTileUtilities[i] == hybridTileUtilities[j] && tileDistances[i] < tileDistances[j])
                        hybridTileUtilities[i] *= -1;
                }
            }
            //Array.Sort(tileDistances, tileOrderDistances);
            string statMsg = $"currentstimuli={currentStimuli}, currentFrame={curIndex}, Utilitytile0={hybridTileUtilities[0]},";
            for (int i = 1; i < nTiles; i++)
            {
                statMsg += $", Utilitytile{i}={hybridTileUtilities[i]}";
            }
            BaseStats.Output(Name(), statMsg);
            //Sort tile utilities and apply the same sort to tileOrder
            Array.Sort(hybridTileUtilities, tileOrder);
            Array.Reverse(tileOrder);
            return tileOrder;
        }
    }
    public class LiveTileSelector : BaseTileSelector
    {

        //
        // Temporary variable (should be measured from internet connection): total available bitrate for this run.
        //
        public double bitRatebudget = 1000000;
       
        TilingConfig tilingConfig;

        string Name()
        {
            return "LiveTileSelector";
        }


        public void Init(PointCloudPipeline _pipeline, int _nQualities, int _nTiles, TilingConfig? _tilingConfig)
        {
            if (_tilingConfig == null)
            {
                throw new System.Exception($"{Name()}: Must have tilingConfig argument");
            }
            tilingConfig = (TilingConfig)_tilingConfig;
            pipeline = _pipeline;
            nQualities = _nQualities;
            Debug.Log($"{Name()}: PrerecordedTileSelector nQualities={nQualities}, nTiles={nTiles}");
            nTiles = _nTiles;
            TileOrientation = new Vector3[_nTiles];
            for (int i = 0; i < _nTiles; i++)
            {
                TileOrientation[i] = tilingConfig.tiles[i].orientation;
            }
        }

        protected override double[][] getBandwidthUsageMatrix(long currentFrameNumber)
        {
            if (currentFrameNumber != 0)
            {
                Debug.LogError($"{Name()}: Programmer error: currentFrameNumber={currentFrameNumber}");
            }
            int nTiles = tilingConfig.tiles.Length;
            if (nTiles == 0) return null; // Not yet initialized
            int nQualities = tilingConfig.tiles[0].qualities.Length;
            double[][] bandwidthUsageMatrix = new double[nTiles][];
            for (int ti = 0; ti < nTiles; ti++)
            {
                var thisTile = tilingConfig.tiles[ti];
                var thisBandwidth = new double[nQualities];
                for (int qi = 0; qi < nQualities; qi++)
                {
                    var thisQuality = thisTile.qualities[qi];
                    thisBandwidth[qi] = thisQuality.bandwidthRequirement;
                }

                bandwidthUsageMatrix[ti] = thisBandwidth;
            }
            return bandwidthUsageMatrix;
        }
        protected override double getBitrateBudget()
        {
            return bitRatebudget;
        }
        protected override long getCurrentFrameIndex()
        {
            return 0;
        }


        protected override Vector3 getCameraForward()
        {
            // xxxjack currently returns camera viedw angle (as the name implies)
            // but maybe camera position is better. Or both.
            var cam = FindObjectOfType<Camera>().gameObject;
            if (cam == null)
                Debug.LogError("Camera not found!");
            //Debug.Log("<color=red> Camera Transform </color>" + cameraForward.x + " " + cameraForward.y + " " + cameraForward.z);
            Transform cameraTransform = cameraTransform = cam.transform;
            return cameraTransform.forward;

        }

        protected override Vector3 getPointcloudPosition(long currentFrameNumber)
        {
            return new Vector3(0, 0, 0);
        }


    }
}