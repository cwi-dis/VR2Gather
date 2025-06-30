using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.UserRepresentation.Voice;
using VRT.Core;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;
using System;
using System.Linq;
using VRT.Pilots.Common;
#if WITH_QUALITY_ASSESSMENT
using QualityAssesment;
#endif

namespace VRT.UserRepresentation.PointCloud
{
    using PointCloudNetworkTileDescription = Cwipc.StreamSupport.PointCloudNetworkTileDescription;

    public class PrerecordedTileSelector : BaseTileSelector
    {

        //Keep track of stimuli being played back
        public string currentStimuli;
        //Disable randomized initial rotation 
        public bool disableRotation = true;
        //
        // Temporary public variable, set by PrerecordedReader: next pointcloud we are expecting to show.
        //
        public static long curIndex;
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
        //
        // Transform for the prerecorded point cloud object (position,scale and rotation from this game object can be applied to other geometry metadata)
        //
        private Transform prerecordedPCTransform;

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

        public void Init(PointCloudPipelineOther _prerecordedPointcloud, int _nQualities, int _nTiles, PointCloudNetworkTileDescription? tilingConfig)
        {
            if (tilingConfig != null)
            {
                throw new System.Exception($"{Name()}: Cannot handle tilingConfig argument");
            }
            //xxxshishir randomize initial orientation of prerecorded pointcloud
            var prerecordedGameObject = GameObject.Find("PrerecordedPosition");
            if (!disableRotation)
                yRotation = UnityEngine.Random.Range(0, 360);
            else
                yRotation = 90.0f;
            prerecordedGameObject.transform.Rotate(0.0f, yRotation, 0.0f, Space.World);
            //xxxshishir we assume no more modifications are made to the game object transform after this point, we use this transform on all tile geometry meta data
            prerecordedPCTransform = prerecordedGameObject.transform;

            pipeline = _prerecordedPointcloud;
            nQualities = _nQualities;
            Debug.Log($"{Name()}: PrerecordedTileSelector nQualities={nQualities}, nTiles={nTiles}");
            nTiles = _nTiles;
            TileOrientation = new Vector3[nTiles];
            for (int ti = 0; ti < nTiles; ti++)
            {
                double angle = ti * Math.PI / 2;
                Vector3 InitalVector = new Vector3((float)Math.Sin(angle), 0, (float)-Math.Cos(angle));
                TileOrientation[ti] = prerecordedPCTransform.TransformDirection(InitalVector);
                TileOrientation[ti] = Vector3.Normalize(TileOrientation[ti]);
            }
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
            VRTConfig._User realUser = VRTConfig.Instance.LocalUser;
#if WITH_QUALITY_ASSESSMENT
            currentStimuli = StimuliController.getCurrentStimulus();
            bitRatebudget = StimuliController.getBitrateBudget();
            int codec = StimuliController.getCodec();
            maxAdaptation = Config.Instance.maxAdaptation;
            switch(codec)
            {
                case 3:
                    algorithm = SelectionAlgorithm.greedy;
                    break;
                case 4:
                    algorithm = SelectionAlgorithm.hybrid;
                    altHybridTileSelection = false;
                    break;
                case 5:
                    algorithm = SelectionAlgorithm.uniform;
                    break;
                case 6:
                    algorithm = SelectionAlgorithm.hybrid;
                    altHybridTileSelection = true;
                    break;
                case 7:
                    algorithm = SelectionAlgorithm.weightedHybrid;
                    break;
                    //xxxshishir ToDo: Refactor to gracefully handle tiled playback of reference content
                default:
                    algorithm = SelectionAlgorithm.alwaysBest;
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
#endif

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
        protected override Vector3 getCameraTransform()
        {
            PlayerControllerSelf player = gameObject.GetComponentInParent<PlayerControllerSelf>();
            Transform cameraTransform = player?.getCameraTransform();
            if (cameraTransform == null)
            {
                Debug.LogError($"Programmer error: {Name()}: no Camera object for self user");
                return new Vector3();
            }
            return cameraTransform.forward;
        }

        public Vector3 getCameraPosition()
        {
            // The camera object is nested in another object on our parent object, so getting at it is difficult:
            PlayerControllerSelf player = gameObject.GetComponentInParent<PlayerControllerSelf>();
            Transform cameraTransform = player?.getCameraTransform();
            if (cameraTransform == null)
            {
                Debug.LogError($"Programmer error: {Name()}: no Camera object for self user");
                return new Vector3();
            }
            return cameraTransform.position;
        }

        protected override Vector3 getPointCloudTransform(long currentFrameNumber)
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
                tileUtilities[i] = Vector3.Dot(cameraForward.normalized, TileOrientation[i].normalized);
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
                tilePosition = prerecordedPCTransform.TransformPoint(tilePosition);
                //xxxshishir drop all the points on the floor before measuring distances
                tilePosition.y = 0;
                camPosition.y = 0;
                tileDistances[i] = Vector3.Distance(camPosition, tilePosition);
                tileLocations[i] = tilePosition;
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
            hybridTileUtilities[0] *= -1;
            hybridTileUtilities[2] *= -1;
            string statMsg = $"currentstimuli={currentStimuli}, currentFrame={curIndex}, initialrotation={yRotation}, bitratebudget={bitRatebudget}, cameraforwardx={cameraForward.x}, cameraforwardy={cameraForward.y}, cameraforwardz={cameraForward.z}, camerapositionx={camPosition.x}, camerapositiony={camPosition.y}, camerapositionz={camPosition.z}";
            for (int i = 0; i < nTiles; i++)
            {
                statMsg += $", Tile{i}Orientationx={TileOrientation[i].x}, Tile{i}Orientationy={TileOrientation[i].y}, Tile{i}Orientationz={TileOrientation[i].z}, Tile{i}BBCentroidLocationx={tileLocations[i].x}, Tile{i}BBCentroidLocationy={tileLocations[i].y}, Tile{i}BBCentroidLocationz={tileLocations[i].z}, Distancetile{i}={tileDistances[i]}, Utilitytile{i}={hybridTileUtilities[i]}, LegacyUtilitytile{i}={tileUtilities[i]}";
            }
#if VRT_WITH_STATS
            Statistics.Output(Name(), statMsg);
#endif
            //xxxshishir modified here for weighted hybrid tile selction
            hybridTileWeights = new float[nTiles];
            Array.Copy(hybridTileUtilities, hybridTileWeights, nTiles);
            //Sort tile utilities and apply the same sort to tileOrder
            Array.Sort(hybridTileUtilities, tileOrder);
            Array.Reverse(tileOrder);
            return tileOrder;
        }
    }
}