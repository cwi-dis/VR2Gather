using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.Core;
using System;
using System.Linq;
using System.Collections;

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

        public enum SelectionAlgorithm { interactive, alwaysBest, frontTileBest, greedy, uniform, hybrid, weightedHybrid };
        //
        // Set by overall controller (in production): which algorithm to use for this scene run.
        //
        [Tooltip("The algorithm to use to do tile selection")]
        public SelectionAlgorithm algorithm = SelectionAlgorithm.interactive;
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
                //Debug.LogWarning($"{Name()}: Update() called, but no pipeline set yet");
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
            if (changed && selectedTileQualities != null)
            {
                // xxxjack: we could do this in stats: format too, may help analysis.
                if (debugDecisions) Debug.Log($"{Name()}: tileQualities: {String.Join(", ", selectedTileQualities)}");
                StartCoroutine(_doSelectTileQualities(selectedTileQualities));
                previousSelectedTileQualities = selectedTileQualities;
                string statMsg = $"tile0={selectedTileQualities[0]}";
                for (int i = 1; i < selectedTileQualities.Length; i++)
                {
                    statMsg += $", tile{i}={selectedTileQualities[i]}";
                }
                BaseStats.Output(Name(), statMsg);
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
                case SelectionAlgorithm.weightedHybrid:
                    return getTileQualities_WeightedHybrid(bandwidthUsageMatrix, budget, cameraForward, pointcloudPosition);
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
                Debug.Log($"{Name()}: lowest quality for all tiles");
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = nQualities - 1;
                Debug.Log($"{Name()}: highest quality for all tiles");
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha1) && nTiles > 0)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[0] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 0");
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && nTiles > 1)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[1] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 1");
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && nTiles > 2)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[2] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 2");
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4) && nTiles > 3)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[3] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 3");
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha5) && nTiles > 4)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[4] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 4");
                return selectedQualities;
            }
            if (Input.GetKeyDown(KeyCode.Alpha6) && nTiles > 5)
            {
                for (int i = 0; i < nTiles; i++) selectedQualities[i] = 0;
                selectedQualities[5] = nQualities - 1;
                Debug.Log($"{Name()}: high quality for tile 5");
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
        //xxxshishir new weighted hybrid utility calculation
        int [] getTileQualities_WeightedHybrid(double[][] bandwidthUsageMatrix, double budget, Vector3 cameraForward, Vector3 pointcloudPosition)
        {
            double spent = 0;
            double[] tileSpent = new double[nTiles];
            int[] tileOrder = getTileOrder(cameraForward, pointcloudPosition);
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
                weightedTileBudgets[i] = budget * hybridTileWeights[i];
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
                    double savings = budget - spent;
                    UnityEngine.Debug.Log("Savings " + savings + " Budget:" + budget + "Spent: " + spent);
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
            return selectedQualities;
        }
        bool[] getTileVisibility(Vector3 cameraForward, Vector3 pointcloudPosition)
        {
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

            return tileVisibility;
        }

    }
}