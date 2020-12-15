using UnityEngine;
using UnityEngine.SpatialTracking;
using VRT.Orchestrator.Wrapping;

namespace VRTPilots
{
    public class NetworkPlayer : MonoBehaviour
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

		private bool _IsLocalPlayer;
		public bool IsLocalPlayer
		{
			get
			{
				return _IsLocalPlayer;
			}
		}

		[DisableEditing]
		public string UserId;

		public Transform HeadTransform;
		public Transform LeftHandTransform;
		public Transform RightHandTransform;
		public TrackedPoseDriver LeftHandPoseDriver;
		public TrackedPoseDriver RightHandPoseDriver;

		public int SendRate = 10; //Send out 10 "frames" per second
		private float _SendDelta;
		private float _LastSendTime;

		private NetworkPlayerData _PreviousReceivedData;
		private NetworkPlayerData _LastReceivedData;
		private float _LastReceiveTime;

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

		public void SetIsLocalPlayer(bool local)
		{
			_IsLocalPlayer = local;
			LeftHandPoseDriver.enabled = _IsLocalPlayer;
			RightHandPoseDriver.enabled = _IsLocalPlayer;
		}

		void SendPlayerData()
		{
			//Send out 
			var data = new NetworkPlayerData
			{
				HeadPosition = HeadTransform.parent.localPosition,
				HeadOrientation = HeadTransform.parent.localRotation,
				LeftHandPosition = LeftHandTransform.localPosition,
				LeftHandOrientation = LeftHandTransform.localRotation,
				RightHandPosition = RightHandTransform.localPosition,
				RightHandOrientation = RightHandTransform.localRotation
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

					HeadTransform.localPosition = Vector3.Lerp(_PreviousReceivedData.HeadPosition, _LastReceivedData.HeadPosition, t);
					LeftHandTransform.localPosition = Vector3.Lerp(_PreviousReceivedData.LeftHandPosition, _LastReceivedData.LeftHandPosition, t);
					RightHandTransform.localPosition = Vector3.Lerp(_PreviousReceivedData.RightHandPosition, _LastReceivedData.RightHandPosition, t);

					HeadTransform.localRotation = Quaternion.Slerp(_PreviousReceivedData.HeadOrientation, _LastReceivedData.HeadOrientation, t);
					LeftHandTransform.localRotation = Quaternion.Slerp(_PreviousReceivedData.LeftHandOrientation, _LastReceivedData.LeftHandOrientation, t);
					RightHandTransform.localRotation = Quaternion.Slerp(_PreviousReceivedData.RightHandOrientation, _LastReceivedData.RightHandOrientation, t);
				}
			}
		}

		public HandInteractionManager GetHandInteractionManager(HandController.Handedness handedness)
		{
			if (handedness == HandController.Handedness.Left)
			{
				return LeftHandTransform.GetComponent<HandInteractionManager>();
			}
			else
			{
				return RightHandTransform.GetComponent<HandInteractionManager>();
			}
		}
	}
}