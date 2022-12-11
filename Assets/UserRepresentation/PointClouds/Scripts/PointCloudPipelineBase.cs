#define NO_VOICE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.UserRepresentation.Voice;
using VRT.Core;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;
using Cwipc;
using VRT.Pilots.Common;

namespace VRT.UserRepresentation.PointCloud
{
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;
    using EncoderStreamDescription = Cwipc.StreamSupport.EncoderStreamDescription;
    using PointCloudNetworkTileDescription = Cwipc.StreamSupport.PointCloudNetworkTileDescription;
    using static VRT.Core.Config._User;

    public abstract class PointCloudPipelineBase : BasePipeline
    {
        protected static int pcDecoderQueueSize = 10;  // Was: 2.
        protected static int pcPreparerQueueSize = 15; // Was: 2.

        [Tooltip("Object responsible for tile quality adaptation algorithm")]
        public BaseTileSelector tileSelector = null;
        [Tooltip("Object responsible for synchronizing playout")]
        public ISynchronizer synchronizer = null;
        protected AsyncReader reader;
        protected AbstractPointCloudEncoder encoder;
        protected List<AbstractPointCloudDecoder> decoders = new List<AbstractPointCloudDecoder>();
        protected AsyncWriter writer;
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

        public static void Register()
        {
            RegisterPipelineClass(true, UserRepresentationType.__PCC_CWIK4A_, AddPointCloudPipelineComponent);
            RegisterPipelineClass(true, UserRepresentationType.__PCC_CWI_, AddPointCloudPipelineComponent);
            RegisterPipelineClass(true, UserRepresentationType.__PCC_PROXY__, AddPointCloudPipelineComponent);
            RegisterPipelineClass(true, UserRepresentationType.__PCC_PRERECORDED__, AddPointCloudPipelineComponent);
            RegisterPipelineClass(true, UserRepresentationType.__PCC_SYNTH__, AddPointCloudPipelineComponent);

            RegisterPipelineClass(false, UserRepresentationType.__PCC_CWIK4A_, AddPointCloudPipelineComponent);
            RegisterPipelineClass(false, UserRepresentationType.__PCC_CWI_, AddPointCloudPipelineComponent);
            RegisterPipelineClass(false, UserRepresentationType.__PCC_PROXY__, AddPointCloudPipelineComponent);
            RegisterPipelineClass(false, UserRepresentationType.__PCC_PRERECORDED__, AddPointCloudPipelineComponent);
            RegisterPipelineClass(false, UserRepresentationType.__PCC_SYNTH__, AddPointCloudPipelineComponent);
        }

        public static BasePipeline AddPointCloudPipelineComponent(GameObject dst, UserRepresentationType i)
        {
            return dst.AddComponent<PointCloudPipelineBase>();
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
        /// <param name="cfg"> Config file json </param>
        /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
        /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
        /// <param name="calibrationMode"> Bool to enter in calib mode and don't encode and send your own PC </param>
        public override BasePipeline Init(bool isLocalPlayer, object _user, Config._User cfg, bool preview = false)
        {
            //
            // Decoder queue size needs to be large for tiled receivers, so we never drop a packet for one
            // tile (because it would mean that the other tiles with the same timestamp become useless)
            //
            if (CwipcConfig.Instance.decoderQueueSizeOverride > 0) pcDecoderQueueSize = CwipcConfig.Instance.decoderQueueSizeOverride;
            //
            // PreparerQueueSize needs to be large enough that there is enough storage in it to handle the
            // largest conceivable latency needed by the Synchronizer.
            //
            if (CwipcConfig.Instance.preparerQueueSizeOverride > 0) pcPreparerQueueSize = CwipcConfig.Instance.preparerQueueSizeOverride;
            user = (User)_user;

            // xxxjack this links synchronizer for all instances, including self. Is that correct?
            if (synchronizer == null)
            {
                synchronizer = FindObjectOfType<VRTSynchronizer>();
            }
            // xxxjack this links tileSelector for all instances, including self. Is that correct?
            // xxxjack also: it my also reuse tileSelector for all instances. That is definitely not correct.
            if (tileSelector == null)
            {
                tileSelector = FindObjectOfType<LiveTileSelector>();
            }
            switch (cfg.sourceType)
            {
                case "self":
                    if (!isLocalPlayer) Debug.LogError($"{Name()}: sourceType==self but not isLocalPlayer");
#if VRT_WITH_STATS
                    Statistics.Output(Name(), $"self=1, userid={user.userId}, representation={(int)user.userData.userRepresentationType}");
#endif
                    _InitForSelfUser(cfg.PCSelfConfig, preview);
                    break;
                case "prerecorded":
                    _InitForPrerecordedPlayer(cfg.PCSelfConfig);
                    break;
                case "remote":
                    if (isLocalPlayer) Debug.LogError($"{Name()}: sourceType!=self but isLocalPlayer==true");
#if VRT_WITH_STATS
                    Statistics.Output(Name(), $"self=0, userid={user.userId}");
#endif
                    //
                    // Determine how many tiles (and therefore decode/render pipelines) we need
                    //
                    Debug.Log($"{Name()}: delay _InitForOtherUser until tiling information received");
                    break;
                default:
                    Debug.LogError($"Programmer error: {Name()}: unknown sourceType {cfg.sourceType}");
                    break;
            }
            return this;
        }

        protected virtual void _InitForSelfUser(Config._User._PCSelfConfig PCSelfConfig, bool preview)
        {
            Debug.LogError($"{Name()}: _InitForSelfUser called but not self user");
        }

        protected virtual void _InitForPrerecordedPlayer(Config._User._PCSelfConfig PCSelfConfig)
        {
            Debug.LogError($"{Name()}: _InitForPrerecordedPlayer called but is self user");
        }

        protected virtual void _InitForOtherUser()
        {
            Debug.LogError($"{Name()}: _InitForOtherUser called but is self user");
        }

        protected QueueThreadSafe _CreateRendererAndPreparer(int curTile = -1)
        {
            CwipcConfig PCs = CwipcConfig.Instance;
            if (PCs == null) throw new System.Exception($"{Name()}: missing PCs config");
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

        void OnDestroy()
        {
            reader?.StopAndWait();
            encoder?.StopAndWait();
            foreach (var decoder in decoders)
            {
                decoder?.StopAndWait();
            }
            writer?.StopAndWait();
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