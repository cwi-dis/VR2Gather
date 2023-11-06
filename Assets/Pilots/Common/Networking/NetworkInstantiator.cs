using UnityEngine;
using UnityEngine.Events;
using VRT.Orchestrator.Wrapping;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Pilots.Common
{
	/// <summary>
	/// Component that instantiates prefabs across all instances of the player in the experience.
    /// OnTrigger callback is invoked after the prefab has been instantiated.
	/// </summary>
	public class NetworkInstantiator : NetworkTriggerBase
	{
		public class NetworkInstantiatorData : BaseMessage
		{
			public string NetworkBehaviourId;
			public string InstantiatedObjectId;
		}

		//public bool MasterOnlyInstantiator = false;

		[Tooltip("The prefab to instantiate")]
		public GameObject templateObject;
		[Tooltip("The location at which the object is instantiated")]
		public Transform location;

	
		protected override void Awake()
		{
			base.Awake();
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_NetworkInstantiatorData, typeof(NetworkInstantiatorData));
			if (location == null) location = transform;

		}
		public void OnEnable()
		{
			OrchestratorController.Instance.Subscribe<NetworkInstantiatorData>(OnNetworkTrigger);
		}


		public void OnDisable()
		{
			OrchestratorController.Instance.Unsubscribe<NetworkInstantiatorData>(OnNetworkTrigger);
		}

		/// <summary>
		/// Call this method locally when the user interaction has happened. It will transmit the event to
		/// other participants, and all participants (including the local one) will call the OnTrigger callback.
		/// </summary>
		public override void Trigger()
		{
			var instantiatorRequest = new NetworkInstantiatorData()
			{
				NetworkBehaviourId = NetworkId,
				InstantiatedObjectId = ""
			};
			if (!OrchestratorController.Instance.UserIsMaster)
			{
				// We are not the master, so we ask the master to do the actual instantiation

#if VRT_WITH_STATS
				Statistics.Output("NetworkInstantiator", $"name={name}, local=1, master=0, instantiatorId={NetworkId}");
#endif
				OrchestratorController.Instance.SendTypeEventToMaster(instantiatorRequest);
				return;
			}
			var newId = InstantiateNetworkObject(null);
			instantiatorRequest.InstantiatedObjectId = newId;
			OrchestratorController.Instance.SendTypeEventToAll(instantiatorRequest);

#if VRT_WITH_STATS
            Statistics.Output("NetworkInstantiator", $"name={name}, local=1, master=1, instantiatorId={NetworkId}, newId={newId}");
#endif
		}

		void OnNetworkTrigger(NetworkInstantiatorData data)
		{
			if (!NeedsAction(data.NetworkBehaviourId)) return;
			if (data.SenderId == OrchestratorController.Instance.SelfUser.userId) return;
			if (OrchestratorController.Instance.UserIsMaster)
            {
				if (data.InstantiatedObjectId != "")
                {
					Debug.LogError($"NetworkInstantiator({name}): master received request with newId={data.InstantiatedObjectId}");
                }
				var newId = InstantiateNetworkObject(null);
				var instantiatorRequest = new NetworkInstantiatorData()
				{
					NetworkBehaviourId = NetworkId,
					InstantiatedObjectId = newId
				};
				OrchestratorController.Instance.SendTypeEventToAll(instantiatorRequest);

#if VRT_WITH_STATS
				Statistics.Output("NetworkInstantiator", $"name={name}, local=0, master=1, instantiatorId={NetworkId}, newId={newId}");
#endif
			}
			else
            {
				if (data.InstantiatedObjectId == "")
				{
					Debug.LogError($"NetworkInstantiator({name}): non-master received request with empty newId");
				}
				var newId = data.InstantiatedObjectId;
				InstantiateNetworkObject(newId);
#if VRT_WITH_STATS
				Statistics.Output("NetworkInstantiator", $"name={name}, local=0, master=1, instantiatorId={NetworkId}, newId={newId}");
#endif

			}
#if VRT_WITH_STATS
			Statistics.Output("NetworkInstantiator", $"name={name}, sessionId={OrchestratorController.Instance.CurrentSession.sessionId}");
#endif
		}

		/// <summary>
		/// Create a local copy of the new object and return it. May be overridden by subclasses,
		/// for example to initialize the new object correctly.
		/// Note that if the content of the object is dynamic (for example a photograph) then there
		/// is no guaraantee that all copies are completely identical (because each photo will be taken using
		/// the camera in that instance of VR2Gather)
		/// </summary>
		/// <returns></returns>
		protected virtual GameObject InstantiateTemplateObject()
		{
			return Instantiate(templateObject, location.transform.position, location.transform.rotation);
        }

		/// <summary>
		/// Instantiate the object and assign its network IDs.
		/// </summary>
		/// <param name="newNetworkId"></param>
		/// <returns></returns>
        protected string InstantiateNetworkObject(string newNetworkId)
		{
			GameObject newObject = InstantiateTemplateObject();
			NetworkIdBehaviour[] netIdBehaviours = newObject.GetComponentsInChildren<NetworkIdBehaviour>(true);
			if (netIdBehaviours.Length > 1 && !string.IsNullOrEmpty(newNetworkId))
            {
				Debug.LogWarning($"NetworkInstantiator({name}): Multiple NetworkIdBehaviours in prefab");
            }
			foreach (var nib in netIdBehaviours)
			{
				if (string.IsNullOrEmpty(newNetworkId))
                {
					nib.CreateNetworkId();
					newNetworkId = nib.NetworkId;
				}
				else
                {
					nib.NetworkId = newNetworkId;
					NetworkIdManager.Add(nib);
                }
			}
			return newNetworkId;
		}
	}
}