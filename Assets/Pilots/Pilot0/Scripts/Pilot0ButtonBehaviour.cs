using UnityEngine;
using VRT.Pilots.Common;

namespace VRT.Pilots.Pilot0
{
	public class Pilot0ButtonBehaviour : MonoBehaviour
	{
		public NetworkTrigger Pilot0ButtonTrigger;

		public float TimeOutBetweenTriggers = 1f;
		private float _ButtonLastTriggered;

        private void Awake()
        {
            Debug.LogError($"Pilot0ButtonBehaviour is obsolete on {name}. See issue #43.");
        }
        private void OnTriggerEnter(Collider other)
		{
			if (Time.realtimeSinceStartup - _ButtonLastTriggered > TimeOutBetweenTriggers)
			{
				string layer = LayerMask.LayerToName(other.gameObject.layer);
				if (layer != "TouchCollider")
				{
					return;
				}

				Debug.Log($"[Pilot0ButtonBehaviour] Triggered by {other.name} on layer {other.gameObject.layer}");

				Pilot0ButtonTrigger.Trigger();

				_ButtonLastTriggered = Time.realtimeSinceStartup;
			}
		}
	}
}