#define NO_VOICE

using System.Collections.Generic;
using UnityEngine;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using VRT.Orchestrator.Elements;
using Cwipc;
using VRT.Core;
using VRT.Pilots.Common;

namespace VRT.UserRepresentation.PointCloud
{
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;
    using EncoderStreamDescription = Cwipc.StreamSupport.EncoderStreamDescription;
    using PointCloudNetworkTileDescription = Cwipc.StreamSupport.PointCloudNetworkTileDescription;
    using static VRT.Core.VRTConfig._User;

    public abstract class PointCloudPipelineBase : BasePipeline
    {
        protected static int pcDecoderQueueSize = 10;  // Was: 2.
        protected static int pcPreparerQueueSize = 15; // Was: 2.

        [Tooltip("Object responsible for tile quality adaptation algorithm")]
        public BaseTileSelector tileSelector = null;
        [Tooltip("Object responsible for synchronizing playout")]
        public ISynchronizer synchronizer = null;
        
        protected List<AsyncPointCloudPreparer> preparers = new List<AsyncPointCloudPreparer>();
        protected List<PointCloudRenderer> renderers = new List<PointCloudRenderer>();

        protected List<QueueThreadSafe> preparerQueues = new List<QueueThreadSafe>();
        protected QueueThreadSafe encoderQueue;
        protected EncoderStreamDescription[] encoderStreamDescriptions; // octreeBits, tileNumber, queue encoder->writer
        protected OutgoingStreamDescription[] outgoingStreamDescriptions;  // queue encoder->writer, tileNumber, quality
        protected PointCloudNetworkTileDescription networkTileDescription;  // Information on pointcloud tiling and quality levels
        protected User user;
        // Mainly for debug messages:
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        protected void SetupConfigDistributors()
        {
            if (PilotController.Instance.sceneIsSingleUser) return;
            BaseConfigDistributor[] configDistributors = FindObjectsOfType<BaseConfigDistributor>();
            if (configDistributors != null)
            {
                if (configDistributors.Length == 0)
                {
                    Debug.LogError("Programmer Error: No ConfigDistributor, you may not be able to see other participants");
                }
                // Register for distribution of tiling and sync configurations
                foreach (var cd in configDistributors)
                {
                    cd?.RegisterPipeline(user.userId, this);
                }
            }
        }

        protected QueueThreadSafe _CreateRendererAndPreparer(int curTile = -1)
        {
            CwipcConfig PCs = CwipcConfig.Instance;
            if (PCs == null) throw new System.Exception($"{Name()}: missing PCs config");
            if (VRTConfig.Instance.PCs.preparerQueueSize > 0) {
                pcPreparerQueueSize = VRTConfig.Instance.PCs.preparerQueueSize;
                Statistics.Output(Name(), $"preparer_queue_size={pcPreparerQueueSize}");
            }            
            QueueThreadSafe preparerQueue = new QueueThreadSafe("PCPreparerQueue", pcPreparerQueueSize, false);
            preparerQueues.Add(preparerQueue);
            AsyncPointCloudPreparer preparer = new AsyncPointCloudPreparer(preparerQueue, PCs.defaultCellSize, PCs.cellSizeFactor);
            preparer.SetSynchronizer(synchronizer);
            preparers.Add(preparer);
            PointCloudRenderer render = gameObject.AddComponent<PointCloudRenderer>();
            string msg = $"preparer={preparer.Name()}, renderer={render.Name()}";
            if (curTile >= 0)
            {
                msg += $", tile={curTile}";
            }
#if VRT_WITH_STATS
            Statistics.Output(Name(), msg);
#endif
            renderers.Add(render);
            render.SetPreparer(preparer);
            return preparerQueue;
        }

        public void PausePlayback(bool paused)
        {
            foreach(var r in renderers)
            {
                r.PausePlayback(paused);
            }
        }

        protected void OnDestroy()
        {
            
            foreach (var preparer in preparers)
            {
                preparer?.StopAndWait();
            }
            foreach (var renderer in renderers)
            {
                Destroy(renderer);
            }
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"finished=1");
#endif
        }

        public new float GetBandwidthBudget()
        {
            return 999999.0f;
        }
    }
}