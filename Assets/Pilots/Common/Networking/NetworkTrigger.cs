using UnityEngine;
using UnityEngine.Events;
using VRT.Orchestrator.Wrapping;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Pilots.Common
{
	/// <summary>
	/// Component that sends triggers (think: button presses) to other instances of the VR2Gather experience.
	/// </summary>
	public class NetworkTrigger : NetworkIdBehaviour
	{
		public class NetworkTriggerData : BaseMessage
		{
			public string NetworkBehaviourId;
		}

		public bool MasterOnlyTrigger = false;

		[Tooltip("Event called when either a local or remote trigger happens.")]
		public UnityEvent OnTrigger;

		public void Awake()
		{
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_NetworkTriggerData, typeof(NetworkTriggerData));

		}
		public void OnEnable()
		{
			OrchestratorController.Instance.Subscribe<NetworkTriggerData>(OnNetworkTrigger);
		}


		public void OnDisable()
		{
			OrchestratorController.Instance.Unsubscribe<NetworkTriggerData>(OnNetworkTrigger);
		}

		/// <summary>
		/// Call this method locally when the user interaction has happened. It will transmit the event to
		/// other participants, and all participants (including the local one) will call the OnTrigger callback.
		/// </summary>
		public virtual void Trigger()
		{
			if (MasterOnlyTrigger && !OrchestratorController.Instance.UserIsMaster)
			{
				return;
			}

			Debug.Log($"NetworkTrigger({name}): Trigger id = {NetworkId}");
			var triggerData = new NetworkTriggerData()
			{
				NetworkBehaviourId = NetworkId,
			};

			if (!OrchestratorController.Instance.UserIsMaster)
			{
				OrchestratorController.Instance.SendTypeEventToMaster(triggerData);
			}
			else
			{
				OnTrigger.Invoke();
#if VRT_WITH_STATS
                Statistics.Output("NetworkTrigger", $"name={name}, sessionId={OrchestratorController.Instance.MySession.sessionId}");
#endif
				OrchestratorController.Instance.SendTypeEventToAll(triggerData);
			}
		}

		void OnNetworkTrigger(NetworkTriggerData data)
		{
			if (NeedsAction(data.NetworkBehaviourId))
			{
#if VRT_WITH_STATS
                Statistics.Output("NetworkTrigger", $"name={name}, sessionId={OrchestratorController.Instance.MySession.sessionId}");
#endif

				OnTrigger.Invoke();

				if (OrchestratorController.Instance.UserIsMaster)
				{
					OrchestratorController.Instance.SendTypeEventToAll(data);
				}
			}
		}

#if UNITY_EDITOR
		[ContextMenu("Force Trigger (Editor-only hack)")]
		private void ForceTrigger()
		{
			OnTrigger.Invoke();
		}
#endif
	}
}