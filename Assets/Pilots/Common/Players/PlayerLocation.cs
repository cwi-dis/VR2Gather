using UnityEngine;

namespace VRT.Pilots.Common
{
	public class PlayerLocation : NetworkIdBehaviour
	{
		public PlayerNetworkControllerBase CurrentPlayer;

		public bool IsEmpty
		{
			get
			{
				return CurrentPlayer == null;
			}
		}

		public void SetPlayer(PlayerNetworkControllerBase player)
		{
			CurrentPlayer = player;
			player.transform.parent = transform;
			player.transform.position = transform.position;
			player.transform.rotation = transform.rotation;
		}

		public void ClearPlayer()
		{
			CurrentPlayer.transform.parent = null;
			CurrentPlayer = null;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube(Vector3.up * 1.7f * 0.5f, new Vector3(0.5f, 1.7f, 0.5f));
		}

	}
}