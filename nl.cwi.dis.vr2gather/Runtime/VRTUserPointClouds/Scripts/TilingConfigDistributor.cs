using UnityEngine;
using System.Collections.Generic;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
using VRT.Pilots.Common;
using Cwipc;

namespace VRT.UserRepresentation.PointCloud
{
    using PointCloudNetworkTileDescription = Cwipc.StreamSupport.PointCloudNetworkTileDescription;

    public class TilingConfigDistributor : BaseConfigDistributor
    {
        // Note there is an AddTypeIdMapping(420, typeof(TilingConfigDistributor.TilingConfigMessage))
        // in MessageForwarder that is part of the magic to make this work.
        public class TilingConfigMessage : BaseMessage
        {
            public PointCloudNetworkTileDescription data;
        }
        private int interval = 1;    // How many seconds between transmissions of the data
        private System.DateTime earliestNextTransmission;    // Earliest time we want to do the next transmission, if non-null.
        const bool debug = true;
        bool started = false;

        public void Awake()
        {
            if (debug) Debug.Log($"TilingConfigDistributor: Awake");
            OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_TilingConfigMessage, typeof(TilingConfigMessage));
            OrchestratorController.Instance.Subscribe<TilingConfigMessage>(OnTilingConfig);
        }

        void Start()
        {
            if (debug) Debug.Log($"TilingConfigDistributor: Started");
            started = true;
            //Subscribe to incoming data of the type we're interested in. 
        }

        private void OnDestroy()
        {
            //If we no longer exist, we should unsubscribe. 
            OrchestratorController.Instance.Unsubscribe<TilingConfigMessage>(OnTilingConfig);
        }

        void Update()
        {
            // If we haven't been inited yet return.
            if (selfUserId == null || !pipelines.ContainsKey(selfUserId)) return;
            // Quick return if interval hasn't expired since last transmission.
            if (earliestNextTransmission != null && System.DateTime.Now < earliestNextTransmission)
            {
                return; // xxxjack should we print an error?
            }
            earliestNextTransmission = System.DateTime.Now + System.TimeSpan.FromSeconds(interval);
            if (interval < 10) interval = interval * 2;
            // Find PointCloudPipeline belonging to self user.
            PointCloudPipelineSelf pipeline = (PointCloudPipelineSelf)pipelines[selfUserId];
            // Get data from self PointCloudPipeline.
            if (pipeline == null)
            {
                return;
            }
            PointCloudNetworkTileDescription tilingConfig = pipeline.GetTilingConfig();
            if (tilingConfig.tiles == null)
            {
                Debug.LogWarning($"TilingConfigDistributor: no tiling information yet for user {selfUserId}");
                return;
            }
            if (debug) Debug.Log($"TilingConfigDistributor: sending tiling information for user {selfUserId} with {tilingConfig.tiles.Length} tiles to receivers");
            var data = new TilingConfigMessage { data = tilingConfig };

            if (OrchestratorController.Instance.UserIsMaster)
            {
                //I'm the master, so I can directly send to all other users
                OrchestratorController.Instance.SendTypeEventToAll(data);
            }
            else
            {
                //I'm not the master, so unfortunately the API forces me to send via the master
                //The master can then forward it to all. 
                OrchestratorController.Instance.SendTypeEventToMaster(data);
            }

        }

        private void OnTilingConfig(TilingConfigMessage receivedData)
        {
            Debug.Log($"TilingConfigDistributor: xxxjack received tiling info from {receivedData.SenderId}");
            if (!started)
            {
                Debug.LogWarning($"TilingConfigDistributor: received tiling information before Start()ed");
            }

            if (OrchestratorController.Instance.UserIsMaster)
            {
                Debug.Log($"TilingConfigDistributor: xxxjack forwarding because we are master");
                //I'm the master, so besides handling the data, I should also make sure to forward it. 
                //This is because the API, to ensure authoritative decisions, doesn't allow users to directly address others. 
                //Same kind of call as usual, but with the extra "true" argument, which ensures we forward without overwriting the SenderId
                OrchestratorController.Instance.SendTypeEventToAll(receivedData, true);
            }
            // We need to check whether we're getting our own data back (due to forwarding by master). Drop if so.
            if (receivedData.SenderId == selfUserId)
            {
                Debug.Log($"TilingConfigDistributor: xxxjack ignoring because it is from self");
                return;
            }
            // Find PointCloudPipeline belonging to receivedData.SenderId.
            if (!pipelines.ContainsKey(receivedData.SenderId))
            {
                Debug.LogWarning($"TilingConfigDistributor: received data for unknown userId {receivedData.SenderId}");
                return;
            }
            PointCloudPipelineOther pipeline = (PointCloudPipelineOther)pipelines[receivedData.SenderId];
            if (pipeline == null)
            {
                return;
            }
            // Give reveicedData.data to that PointCloudPipeline.
            PointCloudNetworkTileDescription tilingConfig = receivedData.data;
            if (debug) Debug.Log($"TilingConfigDistributor: received tiling information from user {receivedData.SenderId} with {tilingConfig.tiles.Length} tiles");
            pipeline.SetTilingConfig(tilingConfig);
        }
    }
}