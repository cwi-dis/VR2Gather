using UnityEngine;
using VRT.Orchestrator;
using VRT.OrchestratorComm;

namespace VRT.Pilots.Common
{
	public class RigidBodyNetworkController : NetworkIdBehaviour
	{
		public class RigidBodyData : BaseMessage
		{
			public string NetworkBehaviourId;
			public Vector3 Position;
			public Quaternion Rotation;
		}

		public enum RigidBodySyncMode
		{
			ServerOnly, //Only the server can sync this transform
			Any //Potentially any user could sync this transform
		}

		[Tooltip("Sync mode determines if DoSync can only be called by the Master or if any user is allowed to trigger a sync")]
		public RigidBodySyncMode Mode = RigidBodySyncMode.ServerOnly;
		[Tooltip("Automatically sync the transform at the rate indicated by UpdateFrequency")]
		public bool SyncAutomatically = true;
		[Tooltip("Number of times to sync per second when SyncAutomatically == true")]
		public float UpdateFrequency = 10;
		[Tooltip("Whether or not we should interpolate updates we receive. Cause a small delay but allows for data to be sent less frequently")]
		public bool InterpolateUpdates = false;

		private float _LastUpdateTime; //Last time we sent an update/sync
		private float _LastReceiveTime; //Last time we received an update/sync

		private RigidBodyData _PreviousReceivedData = null;
		private RigidBodyData _LastReceivedData = null;

		protected override void Awake()
		{
			Debug.LogError($"{gameObject.name}: VR2Gather RigidBodyNetworkController objects should not be used in VR2Gather-Fishnet");
			base.Awake();
			VRTOrchestratorSingleton.Comm.RegisterEventType(MessageTypeID.TID_RigidBodyData, typeof(RigidBodyData));
		}

		void Start()
		{
			//prime the valus with some logical current data. 
			_PreviousReceivedData = new RigidBodyData() { Position = transform.position, Rotation = transform.rotation };
			_LastReceivedData = _PreviousReceivedData;
		}

		public void OnEnable()
		{
			VRTOrchestratorSingleton.Comm.Subscribe<RigidBodyData>(OnRigidBodyData);
		}

		public void OnDisable()
		{
			VRTOrchestratorSingleton.Comm?.Unsubscribe<RigidBodyData>(OnRigidBodyData);
		}

		private void Update()
		{
			if (PilotController.Instance == null || PilotController.Instance.IsLeavingSession) return;
			float updateDelta = 1.0f / UpdateFrequency;
			if (SyncAutomatically)
			{
				if (Time.realtimeSinceStartup - _LastUpdateTime > updateDelta)
				{
					DoSync();
				}
			}

			if (InterpolateUpdates)
			{
				float t = Mathf.Clamp01((Time.realtimeSinceStartup - _LastReceiveTime) / updateDelta);
				transform.position = Vector3.Lerp(_PreviousReceivedData.Position, _LastReceivedData.Position, t);
				transform.rotation = Quaternion.Slerp(_PreviousReceivedData.Rotation, _LastReceivedData.Rotation, t);
			}
		}

		public void DoSync()
		{
			if (VRTOrchestratorSingleton.Comm.UserIsMaster && Mode == RigidBodySyncMode.ServerOnly)
			{
				_LastUpdateTime = Time.realtimeSinceStartup;

				VRTOrchestratorSingleton.Comm.SendTypeEventToAll
					(
						new RigidBodyData
						{
							NetworkBehaviourId = NetworkId,
							Position = transform.position,
							Rotation = transform.rotation
						}
					);
			}
			else if (Mode == RigidBodySyncMode.Any)
			{
				_LastUpdateTime = Time.realtimeSinceStartup;

				var data = new RigidBodyData
				{
					NetworkBehaviourId = NetworkId,
					Position = transform.position,
					Rotation = transform.rotation
				};

				if (VRTOrchestratorSingleton.Comm.UserIsMaster)
				{
					VRTOrchestratorSingleton.Comm.SendTypeEventToAll(data);
				}
				else
				{
					VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(data);
				}
			}
			else
			{
				Debug.LogWarning($"[NetworkTransformSyncBehaviour] Call to DoSync on {gameObject.name} not executed. User isn't master or sync mode is not set to Any");
			}
		}

		void OnRigidBodyData(RigidBodyData data)
		{
			if (data.NetworkBehaviourId == NetworkId && data.SenderId != VRTOrchestratorSingleton.Comm.SelfUser.userId)
			{
				if (SyncAutomatically && Mode == RigidBodySyncMode.Any)
				{
					Debug.LogWarning($"[NetworkTransformSyncBehaviour] {name} is set to sync automatically, but also receives sync data from another client! This is indicative of a bug!");
				}

				if (VRTOrchestratorSingleton.Comm.UserIsMaster)
				{
					VRTOrchestratorSingleton.Comm.SendTypeEventToAll(data, true);
				}

				_LastReceiveTime = Time.realtimeSinceStartup;
				_PreviousReceivedData = _LastReceivedData;
				_LastReceivedData = data;

				if (!InterpolateUpdates)
				{
					transform.position = data.Position;
					transform.rotation = data.Rotation;
				}
			}
		}
	}
}