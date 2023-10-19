using UnityEngine;
using UnityEngine.Events;
using VRT.Orchestrator.Wrapping;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Pilots.Common
{
	/// <summary>
    /// Base class for NetworkTrigger and NetworkInstantiator
    /// </summary>
	public abstract class NetworkTriggerBase : NetworkIdBehaviour
    {
		public abstract void Trigger();
    }

	/// <summary>
	/// Component that sends triggers (think: button presses) to other instances of the VR2Gather experience.
	/// </summary>
	public class NetworkTrigger : NetworkTriggerBase
	{
		public class NetworkTriggerData : BaseMessage
		{
			public string NetworkBehaviourId;
		}

		[Tooltip("If true only the master participant can make this trigger happen")]
		public bool MasterOnlyTrigger = false;

		[Tooltip("Event called when either a local or remote trigger happens.")]
		public UnityEvent OnTrigger;

		protected override void Awake()
		{
			base.Awake();
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_NetworkTriggerData, typeof(NetworkTriggerData));

		}
		public virtual void OnEnable()
		{
			OrchestratorController.Instance.Subscribe<NetworkTriggerData>(OnNetworkTrigger);
		}


		public virtual void OnDisable()
		{
			OrchestratorController.Instance.Unsubscribe<NetworkTriggerData>(OnNetworkTrigger);
		}

		/// <summary>
		/// Call this method locally when the user interaction has happened. It will transmit the event to
		/// other participants, and all participants (including the local one) will call the OnTrigger callback.
		/// </summary>
		public override void Trigger()
		{
			if (MasterOnlyTrigger && !OrchestratorController.Instance.UserIsMaster)
			{
				return;
			}
			if (NetworkId == null || NetworkId == "")
			{
				Debug.LogError($"{name}: Trigger with empty NetworkId");
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
                Statistics.Output("NetworkTrigger", $"name={name}, sessionId={OrchestratorController.Instance.CurrentSession.sessionId}");
#endif
				OrchestratorController.Instance.SendTypeEventToAll(triggerData);
			}
		}

		void OnNetworkTrigger(NetworkTriggerData data)
		{
			if (NeedsAction(data.NetworkBehaviourId))
			{
#if VRT_WITH_STATS
                Statistics.Output("NetworkTrigger", $"name={name}, sessionId={OrchestratorController.Instance.CurrentSession.sessionId}");
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