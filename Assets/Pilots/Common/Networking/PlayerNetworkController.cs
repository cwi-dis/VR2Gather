using UnityEngine;
using UnityEngine.SpatialTracking;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	public class PlayerNetworkController : MonoBehaviour
	{
		#region NetworkPlayerData
		/// <summary>
		/// Message class to store and transmit hand and head positions and orientations
		/// </summary>
		public class NetworkPlayerData : BaseMessage
		{
			public Vector3 HeadPosition;
			public Quaternion HeadOrientation;
			public Vector3 LeftHandPosition;
			public Quaternion LeftHandOrientation;
			public Vector3 RightHandPosition;
			public Quaternion RightHandOrientation;
		}
		#endregion

		[DisableEditing]
		public string UserId;

		public Transform HeadTransform;
		public Transform LeftHandTransform;
		public Transform RightHandTransform;

		public int SendRate = 10; //Send out 10 "frames" per second

		[Header("Introspection/debugging")]
		[DisableEditing][SerializeField] private bool _IsLocalPlayer = true;
		public bool IsLocalPlayer
		{
			get
			{
				return _IsLocalPlayer;
			}
		}

		private float _SendDelta;
		private float _LastSendTime;

		private NetworkPlayerData _PreviousReceivedData;
		private NetworkPlayerData _LastReceivedData;
		private float _LastReceiveTime;

		virtual public string Name()
		{
			return $"{GetType().Name}";
		}

		private void Awake()
		{
			_SendDelta = 1.0f / SendRate;
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_NetworkPlayerData, typeof(NetworkPlayerData));
			OrchestratorController.Instance.Subscribe<NetworkPlayerData>(OnNetworkPlayerData);
		}

		private void OnDestroy()
		{
			OrchestratorController.Instance.Unsubscribe<NetworkPlayerData>(OnNetworkPlayerData);
		}

		void Update()
		{
			if (IsLocalPlayer && _LastSendTime + _SendDelta <= Time.realtimeSinceStartup)
			{
				SendPlayerData();
			}
		}

		public void SetupPlayerNetworkControllerPlayer(bool local, string _userId)
		{
			_IsLocalPlayer = local;
			UserId = _userId;
		}

		void SendPlayerData()
		{
			if (!_IsLocalPlayer)
            {
				Debug.LogError($"{Name()}: SendPlayerData called but not IsLocalPlayer");
            }
			//Send out 
			var data = new NetworkPlayerData
			{
				HeadPosition = HeadTransform.position,
				HeadOrientation = HeadTransform.rotation,
				LeftHandPosition = LeftHandTransform.position,
				LeftHandOrientation = LeftHandTransform.rotation,
				RightHandPosition = RightHandTransform.position,
				RightHandOrientation = RightHandTransform.rotation
			};

			if (OrchestratorController.Instance.UserIsMaster)
			{
				OrchestratorController.Instance.SendTypeEventToAll(data);
			}
			else
			{
				OrchestratorController.Instance.SendTypeEventToMaster(data);
			}

			_LastSendTime = Time.realtimeSinceStartup;
		}

		void OnNetworkPlayerData(NetworkPlayerData data)
		{
			if (!IsLocalPlayer && UserId == data.SenderId)
			{
				if (OrchestratorController.Instance.UserIsMaster)
				{
					//We're the master, so inform the others
					OrchestratorController.Instance.SendTypeEventToAll(data, true);
				}

				_PreviousReceivedData = _LastReceivedData;
				_LastReceivedData = data;
				_LastReceiveTime = Time.realtimeSinceStartup;

				if (_PreviousReceivedData != null)
				{
					//Dirty dirty interpolation. We can/should do better. 
					float t = Mathf.Clamp01((Time.realtimeSinceStartup - _LastReceiveTime) / (1.0f / SendRate));

					if (HeadTransform != null) HeadTransform.position = Vector3.Lerp(_PreviousReceivedData.HeadPosition, _LastReceivedData.HeadPosition, t);
					LeftHandTransform.position = Vector3.Lerp(_PreviousReceivedData.LeftHandPosition, _LastReceivedData.LeftHandPosition, t);
					RightHandTransform.position = Vector3.Lerp(_PreviousReceivedData.RightHandPosition, _LastReceivedData.RightHandPosition, t);

					if (HeadTransform != null) HeadTransform.rotation = Quaternion.Slerp(_PreviousReceivedData.HeadOrientation, _LastReceivedData.HeadOrientation, t);
					LeftHandTransform.rotation = Quaternion.Slerp(_PreviousReceivedData.LeftHandOrientation, _LastReceivedData.LeftHandOrientation, t);
					RightHandTransform.rotation = Quaternion.Slerp(_PreviousReceivedData.RightHandOrientation, _LastReceivedData.RightHandOrientation, t);
				}
			}
		}

		public HandNetworkControllerBase GetHandController(HandNetworkControllerBase.Handedness handedness)
		{
			if (handedness == HandNetworkControllerBase.Handedness.Left)
			{
				return LeftHandTransform.GetComponent<HandNetworkControllerBase>();
			}
			else
			{
				return RightHandTransform.GetComponent<HandNetworkControllerBase>();
			}
		}
	}
}