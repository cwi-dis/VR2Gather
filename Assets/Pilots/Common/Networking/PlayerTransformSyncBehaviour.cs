using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	/// <summary>
	/// Component responsible for ensuring that position and orientation of a player is shared with the other players.
	/// 
	/// For the local player the positions and orientations are captured in Update() and broadcast to the other
	/// instances of the game through the orchestrator.
	/// For non-local players we listen to these messages and reflect them locally.
	/// </summary>
	public class PlayerTransformSyncBehaviour : MonoBehaviour
	{
		public class PlayerTransformSyncData : BaseMessage
		{
			public Vector3 BodyPosition;
			public Quaternion BodyRotation;

			public Vector3 HeadPosition;
			public Quaternion HeadRotation;

			public Vector3 HeadScreenPosition;
			public Quaternion HeadScreenRotation;
		}

		[Tooltip("The network controller for this player (used to send/receive messages)")]
		public PlayerNetworkController Player;
		[Tooltip("Toplevel GameObject for this player (used to get body position/rotation)")]
		public Transform BodyTransform;
		[Tooltip("Visible object representing head (used to get head position/rotation)")]
		public Transform HeadTransform;
		[Tooltip("Visible object representing another head (same as above, for the webcam representation)")]
		public Transform HeadScreenTransform;
		[Tooltip("Number of times to sync per second")]
		public float UpdateFrequency = 10;
		[Tooltip("Whether or not we should interpolate updates we receive. Cause a small delay but allows for data to be sent less frequently")]
		public bool InterpolateUpdates = false;

		private float _LastUpdateTime; //Last time we sent an update/sync
		private float _LastReceiveTime; //Last time we received an update/sync

		private PlayerTransformSyncData _PreviousReceivedData = null;
		private PlayerTransformSyncData _LastReceivedData = null;

		private bool _IsLocalPlayer = false;

		private void Awake()
		{
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_PlayerTransformSyncData, typeof(PlayerTransformSyncData));
		}

		void Start()
		{
			//prime the valus with some logical current data. 
			_PreviousReceivedData = new PlayerTransformSyncData()
			{
				BodyPosition = BodyTransform.position,
				BodyRotation = BodyTransform.rotation,
				HeadPosition = HeadTransform.position,
				HeadRotation = HeadTransform.rotation,
				HeadScreenPosition = HeadScreenTransform.position,
				HeadScreenRotation = HeadScreenTransform.rotation
			};
			_LastReceivedData = _PreviousReceivedData;
			_IsLocalPlayer = Player.IsLocalPlayer;
		}

		public void OnEnable()
		{
			OrchestratorController.Instance.Subscribe<PlayerTransformSyncData>(OnPlayerTransformSyncData);
		}

		public void OnDisable()
		{
			OrchestratorController.Instance.Unsubscribe<PlayerTransformSyncData>(OnPlayerTransformSyncData);
		}

		private void Update()
		{
			float updateDelta = 1.0f / UpdateFrequency;
			if (_IsLocalPlayer)
			{
				if (Time.realtimeSinceStartup - _LastUpdateTime > updateDelta)
				{
					DoSync();
				}
			}
			else
			{
				if (InterpolateUpdates)
				{
					float t = Mathf.Clamp01((Time.realtimeSinceStartup - _LastReceiveTime) / updateDelta);
					BodyTransform.position = Vector3.Lerp(_PreviousReceivedData.BodyPosition, _LastReceivedData.BodyPosition, t);
					BodyTransform.rotation = Quaternion.Slerp(_PreviousReceivedData.BodyRotation, _LastReceivedData.BodyRotation, t);

					if (HeadTransform.gameObject.activeSelf)
					{
						HeadTransform.position = Vector3.Lerp(_PreviousReceivedData.HeadPosition, _LastReceivedData.HeadPosition, t);
						HeadTransform.rotation = Quaternion.Slerp(_PreviousReceivedData.HeadRotation, _LastReceivedData.HeadRotation, t);
					}
					else if (HeadScreenTransform.gameObject.activeSelf)
					{
						HeadScreenTransform.position = Vector3.Lerp(_PreviousReceivedData.HeadScreenPosition, _LastReceivedData.HeadScreenPosition, t);
						HeadScreenTransform.rotation = Quaternion.Slerp(_PreviousReceivedData.HeadScreenRotation, _LastReceivedData.HeadScreenRotation, t);
					}
				}
			}
		}

		private void DoSync()
		{
			_LastUpdateTime = Time.realtimeSinceStartup;

			var data = new PlayerTransformSyncData
			{
				BodyPosition = BodyTransform.position,
				BodyRotation = BodyTransform.rotation,
				HeadPosition = HeadTransform.position,
				HeadRotation = HeadTransform.rotation,
				HeadScreenPosition = HeadScreenTransform.position,
				HeadScreenRotation = HeadScreenTransform.rotation
			};

			if (OrchestratorController.Instance.UserIsMaster)
			{
				OrchestratorController.Instance.SendTypeEventToAll(data);
			}
			else
			{
				OrchestratorController.Instance.SendTypeEventToMaster(data);
			}
		}

		void OnPlayerTransformSyncData(PlayerTransformSyncData data)
		{
			if (data.SenderId == Player.UserId)
			{
				if (OrchestratorController.Instance.UserIsMaster)
				{
					OrchestratorController.Instance.SendTypeEventToAll(data, true);
				}

				if (OrchestratorController.Instance.SelfUser.userId != Player.UserId)
				{
					_LastReceiveTime = Time.realtimeSinceStartup;
					_PreviousReceivedData = _LastReceivedData;
					_LastReceivedData = data;

					if (!InterpolateUpdates)
					{
						BodyTransform.position = data.BodyPosition;
						BodyTransform.rotation = data.BodyRotation;
						HeadTransform.position = data.HeadPosition;
						HeadTransform.rotation = data.HeadRotation;
						HeadScreenTransform.position = data.HeadScreenPosition;
						HeadScreenTransform.rotation = data.HeadScreenRotation;
					}
				}
			}
		}
	}
}