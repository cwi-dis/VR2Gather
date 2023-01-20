using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
    public class PlayerNetworkControllerSelf : PlayerNetworkControllerBase
    {
		private float _SendDelta;
		private float _LastSendTime;
		[Tooltip("Where to get head orientation from")]
		public Transform camTransform;

		protected override void Awake()
		{
			base.Awake();
			_SendDelta = 1.0f / SendRate;
		}
		public override void SetupPlayerNetworkController(PlayerControllerBase _playerController, bool local, string _userId)
		{
			if (!local)
            {
				Debug.LogError($"{Name()}: SetupPlayerNetworkControllerPlayer with local=false");
            }
			_IsLocalPlayer = true;
			UserId = _userId;
			playerController = _playerController;
		}

		void Update()
		{
			if (_LastSendTime + _SendDelta <= Time.realtimeSinceStartup)
			{
				SendPlayerData();
			}
		}
		void SendPlayerData()
		{
			var data = new NetworkPlayerData
			{
				BodyPosition = BodyTransform.position,
				BodyOrientation = BodyTransform.rotation,
				//HeadPosition = camTransform.position,
				HeadOrientation = camTransform.rotation,
				LeftHandPosition = LeftHandTransform.position,
				LeftHandOrientation = LeftHandTransform.rotation,
				RightHandPosition = RightHandTransform.position,
				RightHandOrientation = RightHandTransform.rotation,
				representation = playerController.userRepresentation
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
	}
}
