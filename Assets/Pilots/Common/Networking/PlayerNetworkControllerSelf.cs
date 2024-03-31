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
			if (playerController == null)
			{
				Debug.LogError($"{Name()}: SendPlayerData with no playerController. Probably SetupPlayerNetworkController was not called.");
				return;
			}
			float BodySize = 0;
			GameObject currentRepresentation = playerController.GetRepresentationGameObject();
            if (currentRepresentation != null && currentRepresentation.activeInHierarchy)
            {
                BodySize = currentRepresentation.transform.localScale.y;
            }
            if (AlternativeUserRepresentation != null && AlternativeUserRepresentation.activeInHierarchy)
            {
                BodySize = AlternativeUserRepresentation.transform.localScale.y;
            }
            // For debugging mainly: also copy head position/orientation for the self user
            if (Head3TransformAlsoMove != null)
			{
				Head3TransformAlsoMove.rotation = camTransform.rotation;
				Head3TransformAlsoMove.position = camTransform.position;
			}
			var data = new NetworkPlayerData
			{
				BodyPosition = BodyTransform.position,
				BodyOrientation = BodyTransform.rotation,
				HeadPosition = camTransform.position,
				HeadOrientation = camTransform.rotation,
				LeftHandPosition = LeftHandTransform.position,
				LeftHandOrientation = LeftHandTransform.rotation,
				RightHandPosition = RightHandTransform.position,
				RightHandOrientation = RightHandTransform.rotation,
				representation = playerController.userRepresentation,
				BodySize = BodySize
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
