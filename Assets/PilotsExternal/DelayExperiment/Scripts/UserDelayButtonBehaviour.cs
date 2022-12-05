using UnityEngine;
using VRT.Pilots.Common;

namespace VRT.Pilots.UserDelay
{
	public class UserDelayButtonBehaviour : MonoBehaviour
	{
		public NetworkTrigger UserDelayButtonTrigger;

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

				Debug.Log($"[UserDelayButtonBehaviour] Triggered by {other.name} on layer {other.gameObject.layer}");

				UserDelayButtonTrigger.Trigger();

				_ButtonLastTriggered = Time.realtimeSinceStartup;
			}
		}
	}
}