using UnityEngine;
using VRT.Orchestrator;
using VRT.OrchestratorComm;

namespace VRT.Pilots.Common
{
    public class PlayerNetworkControllerSelf : PlayerNetworkControllerBase
    {
		private float _LastSendTime;
		[Tooltip("Where to get head orientation from")]
		public Transform camTransform;

		
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
			if (_LastSendTime + (1.0f / SendRate) <= Time.realtimeSinceStartup)
			{
				SendPlayerData();
			}
		}
		void SendPlayerData()
		{
			if (PilotController.Instance == null || PilotController.Instance.IsLeavingSession) return;
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
            // Also copy head/hand position/orientation locally so avatar representations can track
            if (HeadPositionOrientation != null)
			{
				HeadPositionOrientation.rotation = camTransform.rotation;
				HeadPositionOrientation.position = camTransform.position;
			}
            if (LeftHandPositionOrientation != null)
            {
                LeftHandPositionOrientation.position = LeftHandTransform.position;
                LeftHandPositionOrientation.rotation = LeftHandTransform.rotation;
            }
            if (RightHandPositionOrientation != null)
            {
                RightHandPositionOrientation.position = RightHandTransform.position;
                RightHandPositionOrientation.rotation = RightHandTransform.rotation;
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

			if (VRTOrchestratorSingleton.Comm.UserIsMaster)
			{
				VRTOrchestratorSingleton.Comm.SendTypeEventToAll(data);
			}
			else
			{
				VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(data);
			}
			// Print a warning if it was preposterously long ago that we last sent this message
			if (_LastSendTime > 0 && Time.realtimeSinceStartup > _LastSendTime + 10.0)
            {
				Debug.LogWarning($"{Name()}: No SendPlayerData() calls in {Time.realtimeSinceStartup - _LastSendTime} seconds");
            }
			_LastSendTime = Time.realtimeSinceStartup;
		}
	}
}
