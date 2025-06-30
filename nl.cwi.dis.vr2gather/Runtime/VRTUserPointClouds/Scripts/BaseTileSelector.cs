using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using System;
using System.Linq;
using System.Collections;
using System.Runtime.InteropServices;
using VRT.Core;
using System.Diagnostics.Tracing;

namespace VRT.UserRepresentation.PointCloud
{
    public abstract class BaseTileSelector : MonoBehaviour
    {
        // Object where we send our quality selection decisions. Initialized by subclass.
        protected PointCloudPipelineOther pipeline;

        // Number of tiles available. Initialized by subclass.
        protected int nTiles;

        // Number of qualities available per tile. Initialized by subclass.
        protected int nQualities;

        // Where the individual tiles are facing. Initialized by subclass.
        protected Vector3[] TileOrientation;

        // Most-recently selected tile qualities (if non-null)
        protected int[] previousSelectedTileQualities;

        public enum SelectionAlgorithm { none, interactive, alwaysBest, frontTileBest, greedy, uniform, hybrid, weightedHybrid };
        //
        // Set by overall controller (in production): which algorithm to use for this scene run.
        //
        public struct AlgorithmParameters {
            public double budget;
            public Transform cameraTransform;
            public Transform pointcloudTransform;
            
        };

        AlgorithmParameters currentParameters;
        AlgorithmParameters previousParameters;

        [Tooltip("The algorithm to use to do tile selection")]
        public SelectionAlgorithm algorithm = SelectionAlgorithm.none;
        //
        // Can be set (in scene editor) to print all decision made by the algorithms.
        //
        [Tooltip("Set to true to print decisions")]
        public bool debugDecisions = false;
        //
        // For quality measurement purposes this can be set to delay the implementation of the decision
        //
        [Tooltip("Delay before implementing decisions (for quality measurement purposes)")]
        public float delayDecisions = 0;

        //Adaptation quality difference cap between tiles
        protected int maxAdaptation = 30;
        //xxxshishir Debug flags
        //greedy prime version of hybrid tile selsction
        protected bool altHybridTileSelection = false;
        //Tile utility weights, used only for weighted hybrid tile utility
        protected float[] hybridTileWeights;
         static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        
#if VRT_WITH_STATS
        bool didOutputStatsIdentity = false;
#endif
        public string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
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
        // Get viewer forward-facing vector.
        //
        protected Transform getCameraTransform()
        {
            // xxxjack currently returns camera viedw angle (as the name implies)
            // but maybe camera position is better. Or both.

            Transform cameraTransform = Camera.main.transform;
            if (cameraTransform == null)
            {
                Debug.LogError($"{Name()}: Camera not found");
                return gameObject.transform;
            }
            return cameraTransform;

        }
        //
        // Get best known position for viewed pointcloud.
        //


        protected Transform getPointCloudTransform(long currentFrameNumber)
        {
            // NOTE: this only works if this MonoBehaviour is attached to the point cloud GameObject,
            // with the exact same transform.
            return gameObject.transform;
        }

        protected void Start()
        {
            // Initialize from config.json, if there are overrides
            var settings = VRTConfig.Instance.TileSelector;
            if (settings != null) {
                if (!string.IsNullOrEmpty(settings.algorithm)) {
                    if (!Enum.TryParse<SelectionAlgorithm>(settings.algorithm, out algorithm))
                    {
                        Debug.LogError($"{Name()}: Unknown algorithm \"{settings.algorithm}\". See log for known algorithms.");
                        foreach (var alg in Enum.GetNames(typeof(SelectionAlgorithm)))
                        {
                            Debug.Log($"{Name()}: Known algorithm: {alg}");
                        }
                    }
                }
            }
            debugDecisions = settings.debugDecisions;
            if (algorithm == SelectionAlgorithm.none) {
                Debug.Log($"{Name()}: algorithm = none, disabling");
                base.enabled = false;
            }
        }

        bool getCurrentAlgorithmParameters(long currentFrameIndex) {
            currentParameters.budget = getBitrateBudget();
            currentParameters.cameraTransform = getCameraTransform();
            currentParameters.pointcloudTransform = getPointCloudTransform(currentFrameIndex);
            bool rv = !currentParameters.Equals(previousParameters);
            previousParameters = currentParameters;
            return rv;
        }

        private void Update()
        {
            // Debug.Log($"{Name()}: xxxjack update called");
            if (pipeline == null)
            {
                // Not yet initialized
                //Debug.LogWarning($"{Name()}: Update() called, but no pipeline set yet");
                return;
            }
#if VRT_WITH_STATS
            if (!didOutputStatsIdentity) {
                Statistics.Output(Name(), $"enabled=1, pipeline={pipeline.Name()}, algorithm={algorithm.ToString()}");
                didOutputStatsIdentity = true;
            }
#endif            
            long currentFrameIndex = getCurrentFrameIndex();
            double[][] bandwidthUsageMatrix = getBandwidthUsageMatrix(currentFrameIndex);
            if (bandwidthUsageMatrix == null)
            {
                // Not yet initialized
                Debug.LogWarning($"{Name()}: Update() called, but no bandwidthUsageMatrix set yet");
                return;
            }
            bool changed = getCurrentAlgorithmParameters(currentFrameIndex);
            if (!changed) {
                //Debug.Log($"{Name()}: xxxjack nothing changed");
                return;
            }
            int[] selectedTileQualities = getTileQualities(bandwidthUsageMatrix, currentParameters);
            changed = previousSelectedTileQualities == null;
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
            if (changed && selectedTileQualities != null)
            {
                // xxxjack: we could do this in stats: format too, may help analysis.
                if (debugDecisions) {
                    string concatenated = string.Join(", ",
                          selectedTileQualities.Select(x => x.ToString()).ToArray());
                Debug.Log($"{Name()}: tileQualities: {concatenated}");
                }
                StartCoroutine(_doSelectTileQualities(selectedTileQualities));
                previousSelectedTileQualities = selectedTileQualities;
                string statMsg = $"tile0={selectedTileQualities[0]}";
                for (int i = 1; i < selectedTileQualities.Length; i++)
                {
                    statMsg += $", tile{i}={selectedTileQualities[i]}";
                }
#if VRT_WITH_STATS
                Statistics.Output(Name(), statMsg);
#endif
            }
#if XXXSHISHIR_REMOVED
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
#endif
        }

        IEnumerator _doSelectTileQualities(int[] selectedTileQualities)
        {
            if (delayDecisions > 0)
            {
                yield return new WaitForSeconds(delayDecisions);
            }
            pipeline.SelectTileQualities(selectedTileQualities);
            yield return null;
        }

        public virtual int[] getTileOrder(Transform cameraTransform, Transform pointcloudTransform)
        {
            // Get the camera forward vector
            Vector3 cameraPosition = cameraTransform.position;
            // Get the pointcloud position
            Vector3 pointcloudPosition = pointcloudTransform.position;

            Vector3 pcToCameraVector = (cameraPosition - pointcloudPosition).normalized;
          
            int[] tileOrder = new int[nTiles];
            //Initialize index array
            for (int i = 0; i < nTiles; i++)
            {
                tileOrder[i] = i;
            }
            float[] tileUtilities = new float[nTiles];
            for (int i = 0; i < nTiles; i++)
            {
                Vector3 thisTileOrientation = TileOrientation[i];
                thisTileOrientation = transform.TransformDirection(thisTileOrientation);
                tileUtilities[i] =  Vector3.Dot(pcToCameraVector, thisTileOrientation);
                if (debugDecisions) {
                    Debug.Log($"{Name()}: tile={i}, orientation={thisTileOrientation}, pcToCamVector={pcToCameraVector}, utility={tileUtilities[i]}");
                }
            }
            //Sort tile utilities and apply the same sort to tileOrder
            Array.Sort(tileUtilities, tileOrder);
            //The tile vectors represent the camera that sees the tile not the orientation of tile surface (ie dot product of 1 is highest utility tile, dot product of -1 is the lowest utility tile)
            Array.Reverse(tileOrder);
            return tileOrder;
        }

        // Get array of per-tile quality wanted, based on current timestamp/framenumber, budget
        // and algorithm
        int[] getTileQualities(double[][] bandwidthUsageMatrix, AlgorithmParameters parameters)
        {
            switch (algorithm)
            {
                case SelectionAlgorithm.none:
                    Debug.LogError($"{Name()}: algorithm==none, should not happen");
                    return null;
                case SelectionAlgorithm.interactive:
                    return getTileQualities_Interactive(bandwidthUsageMatrix, parameters);
                case SelectionAlgorithm.alwaysBest:
                    return getTileQualities_AlwaysBest(bandwidthUsageMatrix, parameters);
                case SelectionAlgorithm.frontTileBest:
                    return getTilesFrontTileBest(bandwidthUsageMatrix, parameters);
                case SelectionAlgorithm.greedy:
                    return getTileQualities_Greedy(bandwidthUsageMatrix, parameters);
                case SelectionAlgorithm.uniform:
                    return getTileQualities_Uniform(bandwidthUsageMatrix, parameters);
                case SelectionAlgorithm.hybrid:
                    return getTileQualities_Hybrid(bandwidthUsageMatrix, parameters);
                case SelectionAlgorithm.weightedHybrid:
                    return getTileQualities_WeightedHybrid(bandwidthUsageMatrix, parameters);
                default:
                    Debug.LogError($"{Name()}: Unknown algorithm");
                    return null;
            }
        }

        int[] getTileQualities_Interactive(double[][] bandwidthUsageMatrix, AlgorithmParameters parameters)
        {
            int[] selectedQualities = new int[nTiles];
            if (Keyboard.current[Key.Digit0].IsPressed())
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                Debug.Log($"{Name()}: lowest quality for all tiles");
                return selectedQualities;
            }
            if (Keyboard.current[Key.Digit9].IsPressed())
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = nQualities - 1;
                Debug.Log($"{Name()}: highest quality for all tiles");
                return selectedQualities;
            }
            if (Keyboard.current[Key.Digit1].IsPressed() && nTiles > 0)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[0] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 0");
                return selectedQualities;
            }
            if (Keyboard.current[Key.Digit2].IsPressed() && nTiles > 1)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[1] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 1");
                return selectedQualities;
            }
            if (Keyboard.current[Key.Digit3].IsPressed() && nTiles > 2)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[2] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 2");
                return selectedQualities;
            }
            if (Keyboard.current[Key.Digit4].IsPressed() && nTiles > 3)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[3] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 3");
                return selectedQualities;
            }
            if (Keyboard.current[Key.Digit5].IsPressed() && nTiles > 4)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[4] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 4");
                return selectedQualities;
            }
            if (Keyboard.current[Key.Digit6].IsPressed() && nTiles > 5)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[5] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 5");
                return selectedQualities;
            }
            return null;
        }
        int[] getTileQualities_AlwaysBest(double[][] bandwidthUsageMatrix, AlgorithmParameters parameters)
        {
            if (debugDecisions) {
                Debug.Log($"{Name()}: AlwaysBest: select quality {nQualities-1} for all tiles");
            }
            int[] selectedQualities = new int[nTiles];

            for (int i = 0; i < nTiles; i++) selectedQualities[i] = nQualities - 1;
            if (debugDecisions) {
                string concatenated = string.Join(", ",
                          selectedQualities.Select(x => x.ToString()).ToArray());
                Debug.Log($"{Name()}: AlwaysBest: selected qualities: {concatenated}");
            }
            return selectedQualities;
        }
        int[] getTilesFrontTileBest(double[][] bandwidthUsageMatrix, AlgorithmParameters parameters)
        {
            if (debugDecisions) {
                Debug.Log($"{Name()}: FrontTileBest: cameraPosition={parameters.cameraTransform.position}, cameraForward={parameters.cameraTransform.forward}, pointCloudPosition={parameters.pointcloudTransform.position}, pointCloudForward={parameters.pointcloudTransform.forward}");
            }
            int[] tileOrder = getTileOrder(parameters.cameraTransform, parameters.pointcloudTransform);
            int[] selectedQualities = new int[nTiles];
            for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
            selectedQualities[tileOrder[0]] = nQualities - 1;
            if (debugDecisions) {
                string concatenated = string.Join(", ",
                          selectedQualities.Select(x => x.ToString()).ToArray());
                Debug.Log($"{Name()}: FrontTileBest: selected qualities: {concatenated}");
            }
            return selectedQualities;
        }

        //xxxshishir actual tile selection strategies used for evaluation
        int[] getTileQualities_Greedy(double[][] bandwidthUsageMatrix, AlgorithmParameters parameters)
        {
            if (debugDecisions) {
                Debug.Log($"{Name()}: Greedy: cameraPosition={parameters.cameraTransform.position}, cameraForward={parameters.cameraTransform.forward}, pointCloudPosition={parameters.pointcloudTransform.position}, pointCloudForward={parameters.pointcloudTransform.forward}");
            }
            double spent = 0;
            int[] tileOrder = getTileOrder(parameters.cameraTransform, parameters.pointcloudTransform);
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
                        if ((spent + nextSpend) <= parameters.budget)
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
                    double savings = parameters.budget - spent;
                    // UnityEngine.Debug.Log("<color=green> XXXDebug Budget" + budget + " spent " + spent + " savings " + savings + " </color> ");
                }
            }
            if (debugDecisions) {
                string concatenated = string.Join(", ",
                          selectedQualities.Select(x => x.ToString()).ToArray());
                Debug.Log($"{Name()}: Greedy: selected qualities: {concatenated}");
            }
            return selectedQualities;
        }
        int[] getTileQualities_Uniform(double[][] bandwidthUsageMatrix, AlgorithmParameters parameters)
        {
            if (debugDecisions) {
                Debug.Log($"{Name()}: Uniform: cameraPosition={parameters.cameraTransform.position}, cameraForward={parameters.cameraTransform.forward}, pointCloudPosition={parameters.pointcloudTransform.position}, pointCloudForward={parameters.pointcloudTransform.forward}");
            }
            double spent = 0;
            int[] tileOrder = getTileOrder(parameters.cameraTransform, parameters.pointcloudTransform);
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
                        if ((spent + nextSpend) <= parameters.budget)
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
                    double savings = parameters.budget - spent;
                }
            }
            if (debugDecisions) {
                string concatenated = string.Join(", ",
                          selectedQualities.Select(x => x.ToString()).ToArray());
                Debug.Log($"{Name()}: Uniform: selected qualities: {concatenated}");
            }
            return selectedQualities;
        }
        int[] getTileQualities_Hybrid(double[][] bandwidthUsageMatrix, AlgorithmParameters parameters)
        {
            if (debugDecisions) {
                Debug.Log($"{Name()}: Hybrid: cameraPosition={parameters.cameraTransform.position}, cameraForward={parameters.cameraTransform.forward}, pointCloudPosition={parameters.pointcloudTransform.position}, pointCloudForward={parameters.pointcloudTransform.forward}");
            }
            bool[] tileVisibility = getTileVisibility(parameters.cameraTransform, parameters.pointcloudTransform);
            double spent = 0;
            int[] tileOrder = getTileOrder(parameters.cameraTransform, parameters.pointcloudTransform);
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
                        if ((spent + nextSpend) <= parameters.budget && tileVisibility[tileOrder[i]] == true && (selectedQualities[tileOrder[i]] - selectedQualities.Min()) < maxAdaptation)
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
                            if ((spent + nextSpend) < parameters.budget && tileVisibility[tileOrder[i]] == false && (selectedQualities[tileOrder[i]] - selectedQualities.Min()) < maxAdaptation)
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
                    double savings = parameters.budget - spent;
                }
            }
            if (debugDecisions) {
                string concatenated = string.Join(", ",
                          selectedQualities.Select(x => x.ToString()).ToArray());
                Debug.Log($"{Name()}: Hybrid: selected qualities: {concatenated}");
            }
            return selectedQualities;
        }
        //xxxshishir new weighted hybrid utility calculation
        int [] getTileQualities_WeightedHybrid(double[][] bandwidthUsageMatrix, AlgorithmParameters parameters)
        {
            if (debugDecisions) {
                Debug.Log($"{Name()}: WeightedHybrid: cameraPosition={parameters.cameraTransform.position}, cameraForward={parameters.cameraTransform.forward}, pointCloudPosition={parameters.pointcloudTransform.position}, pointCloudForward={parameters.pointcloudTransform.forward}");
            }
            double spent = 0;
            double[] tileSpent = new double[nTiles];
            int[] tileOrder = getTileOrder(parameters.cameraTransform, parameters.pointcloudTransform);
            // Start by selecting minimal quality for each tile
            int[] selectedQualities = new int[nTiles];
            // Assume we spend at least minimal quality badnwidth requirements for each tile
            for (int i = 0; i < nTiles; i++)
            {
                spent += bandwidthUsageMatrix[i][0];
                tileSpent[i] = bandwidthUsageMatrix[i][0];
                hybridTileWeights[i] += 1; 
            }
            //Weighted sbudget split
            double[] weightedTileBudgets = new double[nTiles];
            float wsum = hybridTileWeights.Sum();
            for (int i = 0; i < nTiles; i++)
            {
                hybridTileWeights[i] = hybridTileWeights[i] / wsum;
                weightedTileBudgets[i] = parameters.budget * hybridTileWeights[i];
            }
            //UnityEngine.Debug.Log("Weighted budgets: " + weightedTileBudgets[0] + ", " + weightedTileBudgets[1] + ", " + weightedTileBudgets[2] + ", " + weightedTileBudgets[3] + ", Total: " + weightedTileBudgets.Sum());
            //UnityEngine.Debug.Log("Weights: " + hybridTileWeights[0] + ", " + hybridTileWeights[1] + ", " + hybridTileWeights[2] + ", " + hybridTileWeights[3] + ", Total: " + hybridTileWeights.Sum());
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
                        if ((tileSpent[tileOrder[i]] + nextSpend) <= weightedTileBudgets[tileOrder[i]])
                        {
                            selectedQualities[tileOrder[i]]++;
                            stepComplete = true;
                            spent += nextSpend;
                            tileSpent[tileOrder[i]] += nextSpend;
                            break;
                        }
                    }
                }
                if (!stepComplete)
                {
                    double savings = parameters.budget - spent;
                    UnityEngine.Debug.Log("Savings " + savings + " Budget:" + parameters.budget + "Spent: " + spent);
                    //xxx shsihir greedily spend the remaining budget
                    for(int i=0; i < nTiles; i++)
                    {
                        if(selectedQualities[tileOrder[i]] < nQualities -1)
                        {
                            double nextSpend = bandwidthUsageMatrix[tileOrder[i]][(selectedQualities[tileOrder[i]] + 1)] - bandwidthUsageMatrix[tileOrder[i]][selectedQualities[tileOrder[i]]];
                            if(nextSpend <= savings)
                            {
                                selectedQualities[tileOrder[i]]++;
                                stepComplete = true;
                                savings -= nextSpend;
                                break;
                            }
                        }
                    }
                }
                if (!stepComplete)
                    representationSet = true;
            }
            if (debugDecisions) {
                string concatenated = string.Join(", ",
                          selectedQualities.Select(x => x.ToString()).ToArray());
                Debug.Log($"{Name()}: WeightedHybrid: selected qualities: {concatenated}");
            }
            return selectedQualities;
        }
        bool[] getTileVisibility(Transform cameraTransform, Transform pointcloudTransform)
        {
            // Get the camera forward vector
            Vector3 cameraForward = cameraTransform.forward;
            // Get the pointcloud position
            Vector3 pointcloudPosition = pointcloudTransform.position;

            // xxxjack currently ignores pointcloud position, which is probably wrong...
            bool[] tileVisibility = new bool[nTiles];
            float[] tileDirection = new float[nTiles];
            //Tiles with dot product > 0 have the tile cameras facing in the same direction as the current scene camera (Note: TileC1-C4 contain the orientation of tile cameras NOT tile surfaces)
            for (int i = 0; i < nTiles; i++)
            {
                if (!altHybridTileSelection)
                    tileVisibility[i] = Vector3.Dot(cameraForward, TileOrientation[i]) > 0;
                else
                {
                    tileDirection[i] = Vector3.Dot(cameraForward, TileOrientation[i]);
                    tileVisibility[i] = true;
                }
            }
            //xxxshishir the greedy prime bit, only the worst tile is set to not visible
            if (altHybridTileSelection)
                tileVisibility[Array.IndexOf(tileDirection, tileDirection.Min())] = false;

            if (debugDecisions) {
                string concatenated = string.Join(", ",
                          tileVisibility.Select(x => x.ToString()).ToArray());
                Debug.Log($"{Name()}: getTileVisibility: per-tile visibility: {concatenated}");
            }
            return tileVisibility;
        }

    }
}