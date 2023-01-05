using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRT.Pilots.Common
{
	/// <summary>
	/// Behaviour of an object that the user can touch (either through direct interaction or ray-based interaction).
	/// </summary>
	public class Trigger : MonoBehaviour
	{
		[Tooltip("Component that communicates triggers to other instances of the experience")]
		public NetworkTrigger networkTrigger;

		public float TimeOutBetweenTriggers = 1f;
		private float _ButtonLastTriggered;

		/// <summary>
		/// Called by Unity on collider activity.
		/// </summary>
		/// <param name="other"></param>
		private void OnTriggerEnter(Collider other)
		{
			if (Time.realtimeSinceStartup - _ButtonLastTriggered > TimeOutBetweenTriggers)
			{
				string layer = LayerMask.LayerToName(other.gameObject.layer);
				Debug.Log($"Trigger({name}): Triggered by collider {other.name} on layer {other.gameObject.layer} name {layer}");

				if (layer != "TouchCollider")
				{
					return;
				}


				OnActivate();

				_ButtonLastTriggered = Time.realtimeSinceStartup;
			}
		}

		/// <summary>
		/// Called from Interactable Activate event.
		/// </summary>
        public void OnActivate()
        {
			Debug.Log($"Trigger({name}): OnActivate()");
			networkTrigger.Trigger();
		}
	}
}
