using UnityEngine;
using VRT.Pilots.Common;

namespace VRT.Pilots.UserDelayPilot0
{
	public class UserDelayPilot0ButtonBehaviour : MonoBehaviour
	{
		public NetworkTrigger UserDelayPilot0ButtonTrigger;

		public float TimeOutBetweenTriggers = 1f;
		private float _ButtonLastTriggered;

		private void OnTriggerEnter(Collider other)
		{
			if (Time.realtimeSinceStartup - _ButtonLastTriggered > TimeOutBetweenTriggers)
			{
				string layer = LayerMask.LayerToName(other.gameObject.layer);
				if (layer != "TouchCollider")
				{
					return;
				}

				Debug.Log($"[UserDelayPilot0ButtonBehaviour] Triggered by {other.name} on layer {other.gameObject.layer}");

				UserDelayPilot0ButtonTrigger.Trigger();

				_ButtonLastTriggered = Time.realtimeSinceStartup;
			}
		}
	}
}