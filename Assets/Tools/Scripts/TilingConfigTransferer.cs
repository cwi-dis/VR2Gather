using UnityEngine;

public class TilingConfigTransferer : MonoBehaviour
{
	// Note there is an AddTypeIdMapping(420, typeof(TilingConfigTransferer.TilingConfigMessage))
    // in MessageForwarder that is part of the magic to make this work.
	public class TilingConfigMessage : BaseMessage
	{
		public TilingConfig data;
	}
    const int interval = 10;    // How many seconds between transmissions of the data

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
		// xxxjack quick return if interval hasn't expired since last transmission.
        // xxxjack find EntityPipeline belonging to self user.
        // xxxjack get data from self EntityPipeline.
		var data = new TilingConfigMessage { data = new TilingConfig() };

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

		if(OrchestratorController.Instance.UserIsMaster)
		{
			//I'm the master, so besides handling the data, I should also make sure to forward it. 
			//This is because the API, to ensure authoritative decisions, doesn't allow users to directly address others. 
			//Same kind of call as usual, but with the extra "true" argument, which ensures we forward without overwriting the SenderId
			OrchestratorController.Instance.SendTypeEventToAll<TilingConfigMessage>(receivedData, true);
		}
        // xxxjack we need to check whether we're getting our own data back (due to forwarding by master). Drop if so.
        // xxxjack find EntityPipeline belonging to receivedData.SenderId.
        // xxxjack give reveicedData.data to that EntityPipeline.
        string idOfUserIRepresent = "";
        if (receivedData.SenderId != idOfUserIRepresent)
        {
            //Nothing to do with us, so ignore the data
            return;
        }

        //Depending on the use-case we might want to ignore data that gets reflected back to us
        //This happens when sending to the master. If it forwards to all, the sender will also receive it
        //if(OrchestratorController.Instance.SelfUser.userId == idOfUserIRepresent)
        //{
        //  return;
        //}

        //From here on out, do whatever you want with the data. 
    }
}
