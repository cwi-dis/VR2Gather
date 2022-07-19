﻿using UnityEngine;
using UnityEngine.Events;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	public class NetworkTrigger : NetworkIdBehaviour
	{
		public class NetworkTriggerData : BaseMessage
		{
			public string NetworkBehaviourId;
		}

		public bool MasterOnlyTrigger = false;

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

		public virtual void Trigger()
		{
			if (MasterOnlyTrigger && !OrchestratorController.Instance.UserIsMaster)
			{
				return;
			}

			Debug.Log($"[NetworkTrigger] Trigger called on NetworkTrigger with id = {NetworkId} and name = {gameObject.name}.");
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
				VRT.Core.BaseStats.Output("NetworkTrigger", $"name={name}, sessionId={OrchestratorController.Instance.MySession.sessionId}");
				OrchestratorController.Instance.SendTypeEventToAll(triggerData);
			}
		}

		void OnNetworkTrigger(NetworkTriggerData data)
		{
			if (NeedsAction(data.NetworkBehaviourId))
			{
				VRT.Core.BaseStats.Output("NetworkTrigger", $"name={name}, sessionId={OrchestratorController.Instance.MySession.sessionId}");

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