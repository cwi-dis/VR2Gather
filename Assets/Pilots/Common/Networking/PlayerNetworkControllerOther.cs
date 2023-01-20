using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
    public class PlayerNetworkControllerOther : PlayerNetworkControllerBase
    {

		public override void SetupPlayerNetworkController(PlayerControllerBase _playerController, bool local, string _userId)
		{
			if (local)
			{
				Debug.LogError($"{Name()}: SetupPlayerNetworkControllerPlayer with local=true");
			}
			_IsLocalPlayer = false;
			UserId = _userId;
			playerController = _playerController;
		}

		private void OnDestroy()
		{
			OrchestratorController.Instance.Unsubscribe<NetworkPlayerData>(OnNetworkPlayerData);
		}
	}
}
