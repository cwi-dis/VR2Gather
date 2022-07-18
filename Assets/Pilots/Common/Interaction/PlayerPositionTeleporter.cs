using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Teleporter;

namespace VRT.Pilots.Common
{
    public class PlayerPositionTeleporter : BaseTeleporter
    {
		[Tooltip("Renderer for teleporter line")]
		public LineRenderer TeleportLineRenderer;
		[Tooltip("Material to use for line if destination is teleportable")]
		public Material TeleportPossibleMaterial;
		[Tooltip("Material to use for line if destination is not teleportable")]
		public Material TeleportImpossibleMaterial;
		
		[Tooltip("don't update path when it's false.")]
        public bool displayActive = false;

		// Where we are teleporting to.
		private PlayerLocation _SelectedLocation;

		public override bool teleporterActive
        {
            get { return displayActive; }
        }

        // Teleport target transform to ground position
        public override void Teleport()
        {
			if (_SelectedLocation != null)
			{
				SessionPlayersManager.Instance.RequestLocationChange(_SelectedLocation.NetworkId);
			}

		}

		public override void TeleportHome()
		{
			Vector3 newPosition = Vector3.zero;
			// We search for our player by finding the PlayerManager
			var playerManager = GetComponentInParent<PlayerManager>();
			var player = playerManager?.gameObject;
			if (player != null)
            {
				player.transform.localPosition = newPosition;
				Debug.Log("xxxjack PlayerPositionTeleporter: teleported home");

			} else
            {
				Debug.LogWarning("PlayerPositionTeleporter: cannot teleportHome() because I cannot find my player");
            }
			SetActive(false);
		}

		public override bool canTeleport()
        {
            return _SelectedLocation != null;
        }

        // Enable (or disable) the teleport ray
        public override void SetActive(bool active)
        {
			TeleportLineRenderer.enabled = active;
			if (active && !displayActive)
            {
				_SelectedLocation = null;
				TeleportLineRenderer.material = TeleportImpossibleMaterial;
			}
			displayActive = active;
		}

		private void Awake()
        {
 
        }

        public override void UpdatePath()
        {
        }

        public override void CustomUpdatePath(Vector3? _origin, Vector3? _direction, float _strength)
        {
			Vector3 pos = _origin ?? transform.position;
			Vector3 dir = _direction ?? transform.forward;
			Debug.DrawLine(pos, pos + 1.0f * dir, Color.blue);
			Ray teleportRay = new Ray(pos, dir);
			RaycastHit hit;

			Vector3[] points = new Vector3[2];
			points[0] = pos;
			points[1] = pos + 25.0f * dir;
			LayerMask uimask = LayerMask.NameToLayer("UI");
			if (Physics.Raycast(teleportRay, out hit, uimask))
			{
				points[1] = hit.point;
				if (hit.collider.tag == "PlayerLocation")
				{
					var location = hit.collider.GetComponent<PlayerLocation>();
					if (location.IsEmpty)
					{
						TeleportLineRenderer.material = TeleportPossibleMaterial;
						_SelectedLocation = location;
					}
					else
					{
						TeleportLineRenderer.material = TeleportImpossibleMaterial;
						_SelectedLocation = null;
					}
				}
				else
				{
					TeleportLineRenderer.material = TeleportImpossibleMaterial;
					_SelectedLocation = null;
					Debug.Log($"PlayerPositionTeleporter: Hit unteleportable object: {hit.collider.gameObject.name}");

				}
			}
			else
			{
				TeleportLineRenderer.material = TeleportImpossibleMaterial;
			}
			TeleportLineRenderer.SetPositions(points);
		}
    }
}