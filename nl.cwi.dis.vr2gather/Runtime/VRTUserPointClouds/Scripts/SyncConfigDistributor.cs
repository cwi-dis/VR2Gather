using UnityEngine;
using Cwipc;
using VRT.Pilots.Common;
using VRT.Orchestrator;
using VRT.OrchestratorComm;

namespace VRT.UserRepresentation.PointCloud
{
    public class SyncConfigDistributor : BaseConfigDistributor
    {
        // Note there is an AddTypeIdMapping(42X, typeof(SyncConfigDistributor.SyncConfigMessage))
        // in MessageForwarder that is part of the magic to make this work.
        public class SyncConfigMessage : BaseMessage
        {
            public SyncConfig data;
        }
        private int interval = 1;    // How many seconds between transmissions of the data
        private System.DateTime earliestNextTransmission;    // Earliest time we want to do the next transmission, if non-null.
        const bool debug = true;

        public void Awake()
        {
            VRTOrchestratorSingleton.Comm.RegisterEventType(MessageTypeID.TID_SyncConfigMessage, typeof(SyncConfigMessage));
        }

        void Start()
        {
            if (debug) Debug.Log($"SyncConfigDistributor: Started");
        }

        public void OnEnable()
        {
            VRTOrchestratorSingleton.Comm.Subscribe<SyncConfigMessage>(OnSyncConfig);
        }

        public void OnDisable()
        {
            VRTOrchestratorSingleton.Comm?.Unsubscribe<SyncConfigMessage>(OnSyncConfig);
        }

        void Update()
        {
            if (PilotController.Instance == null || PilotController.Instance.IsLeavingSession) return;
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
            SyncConfig syncConfig = pipeline.GetSyncConfig();
            if (debug) Debug.Log($"SyncConfigDistributor: sending sync information for user {selfUserId}");
            var data = new SyncConfigMessage { data = syncConfig };

            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
            {
                //I'm the master, so I can directly send to all other users
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(data);
            }
            else
            {
                //I'm not the master, so unfortunately the API forces me to send via the master
                //The master can then forward it to all. 
                VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(data);
            }

        }

        private void OnSyncConfig(SyncConfigMessage receivedData)
        {

            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
            {
                //I'm the master, so besides handling the data, I should also make sure to forward it. 
                //This is because the API, to ensure authoritative decisions, doesn't allow users to directly address others. 
                //Same kind of call as usual, but with the extra "true" argument, which ensures we forward without overwriting the SenderId
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(receivedData, true);
            }
            // We need to check whether we're getting our own data back (due to forwarding by master). Drop if so.
            if (receivedData.SenderId == selfUserId) return;
            // Find PointCloudPipeline belonging to receivedData.SenderId.
            if (!pipelines.ContainsKey(receivedData.SenderId))
            {
                Debug.LogWarning($"SyncConfigDistributor: received data for unknown userId {receivedData.SenderId}");
                return;
            }
            PointCloudPipelineOther pipeline = (PointCloudPipelineOther)pipelines[receivedData.SenderId];
            if (pipeline == null)
            {
                return;
            }
            // Give reveicedData.data to that PointCloudPipeline.
            SyncConfig syncConfig = receivedData.data;
            if (debug) Debug.Log($"SyncConfigDistributor: received sync information from user {receivedData.SenderId}");
            pipeline.SetSyncConfig(syncConfig);
        }
    }
}