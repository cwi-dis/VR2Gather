using UnityEngine;
using System.Collections.Generic;

public class TilingConfigDistributor : MonoBehaviour
{
    // Note there is an AddTypeIdMapping(420, typeof(TilingConfigDistributor.TilingConfigMessage))
    // in MessageForwarder that is part of the magic to make this work.
    public class TilingConfigMessage : BaseMessage
	{
		public TilingConfig data;
	}
    private int interval = 1;    // How many seconds between transmissions of the data
    private System.DateTime earliestNextTransmission;    // Earliest time we want to do the next transmission, if non-null.
    private string selfUserId;
    private Dictionary<string, EntityPipeline> pipelines = new Dictionary<string, EntityPipeline>();
    const bool debug = false;

    public TilingConfigDistributor Init(string _selfUserId)
    {
        selfUserId = _selfUserId;
        return this;
    }

    public void RegisterPipeline(string userId, EntityPipeline pipeline)
    {
        if (pipelines.ContainsKey(userId))
        {
            Debug.LogError($"Programmer error: TilingConfigDistributor: registering duplicate userId {userId}");
        }
        pipelines[userId] = pipeline;
    }

    void Start()
    {
		//Subscribe to incoming data of the type we're interested in. 
		OrchestratorController.Instance.Subscribe<TilingConfigMessage>(OnTilingConfig);
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
            return;
        }
        earliestNextTransmission = System.DateTime.Now + System.TimeSpan.FromSeconds(interval);
        if (interval < 10) interval = interval * 2;
        // Find EntityPipeline belonging to self user.
        var pipeline = pipelines[selfUserId];
        // Get data from self EntityPipeline.
        TilingConfig tilingConfig = pipeline.GetTilingConfig();
        if (debug) Debug.Log($"TilingConfigDistributor: sending tiling information for user {selfUserId} with {tilingConfig.tiles.Length} tiles to receivers");
        var data = new TilingConfigMessage { data = tilingConfig };

		if (OrchestratorController.Instance.UserIsMaster)
		{
			//I'm the master, so I can directly send to all other users
			OrchestratorController.Instance.SendTypeEventToAll<TilingConfigMessage>(data);
		}
		else
		{
			//I'm not the master, so unfortunately the API forces me to send via the master
			//The master can then forward it to all. 
			OrchestratorController.Instance.SendTypeEventToMaster<TilingConfigMessage>(data);
		}

    }

	private void OnTilingConfig(TilingConfigMessage receivedData)
	{

        if (OrchestratorController.Instance.UserIsMaster)
		{
			//I'm the master, so besides handling the data, I should also make sure to forward it. 
			//This is because the API, to ensure authoritative decisions, doesn't allow users to directly address others. 
			//Same kind of call as usual, but with the extra "true" argument, which ensures we forward without overwriting the SenderId
			OrchestratorController.Instance.SendTypeEventToAll<TilingConfigMessage>(receivedData, true);
		}
        // We need to check whether we're getting our own data back (due to forwarding by master). Drop if so.
        if (receivedData.SenderId == selfUserId) return;
        // Find EntityPipeline belonging to receivedData.SenderId.
        if (!pipelines.ContainsKey(receivedData.SenderId))
        {
            Debug.LogWarning($"TilingConfigDistributor: received data for unknown userId {receivedData.SenderId}");
            return;
        }
        var pipeline = pipelines[receivedData.SenderId];
        // Give reveicedData.data to that EntityPipeline.
        TilingConfig tilingConfig = receivedData.data;
        if (debug) Debug.Log($"TilingConfigDistributor: received tiling information from user {selfUserId} with {tilingConfig.tiles.Length} tiles");
        pipeline.SetTilingConfig(tilingConfig);
    }
}
