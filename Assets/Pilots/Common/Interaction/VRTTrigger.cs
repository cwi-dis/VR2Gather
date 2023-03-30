using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace VRT.Pilots.Common
{
	/// <summary>
	/// Behaviour of an object that the user can touch (either through direct interaction or ray-based interaction).
	/// </summary>
	public class VRTTrigger : MonoBehaviour
	{
		[Tooltip("Component that communicates triggers to other instances of the experience")]
		public NetworkTriggerBase networkTrigger;
		[Tooltip("Local callbacks to do when triggered.")]
		public UnityEvent localTrigger;

		public float TimeOutBetweenTriggers = 1f;
		private float _ButtonLastTriggered;

        private void Awake()
        {
			Debug.LogError("VRTTrigger is obsolete. See issue #43.");
            if (networkTrigger == null)
			{
				networkTrigger = GetComponent<NetworkTrigger>();
				if (networkTrigger == null && localTrigger.GetPersistentEventCount() == 0)
				{
					Debug.LogError($"{name}: no NetworkTrigger and no local triggers on GameObject");
				}
			}
        }

        /// <summary>
        /// Called by Unity on collider activity.
        /// Also called by VRT NoHandInteraction with a null collider on mouse-ray interaction.
        /// </summary>
        /// <param name="other"></param>
        public void OnTriggerEnter(Collider other)
		{
			if (Time.realtimeSinceStartup - _ButtonLastTriggered > TimeOutBetweenTriggers)
			{
				if (other == null)
                {
					Debug.Log($"Trigger({name}): Triggered by mouse-ray");
				}
				else
                {
					string layer = LayerMask.LayerToName(other.gameObject.layer);
					Debug.Log($"Trigger({name}): Triggered by collider {other.name} on layer {other.gameObject.layer} name {layer}");

					if (layer != "TouchCollider")
					{
						return;
					}

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
			if (networkTrigger != null)
			{
				networkTrigger.Trigger();
			}
			localTrigger.Invoke();
		}
	}
}
