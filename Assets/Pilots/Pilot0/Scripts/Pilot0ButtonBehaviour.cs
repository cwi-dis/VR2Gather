using VRTPilots;
using UnityEngine;

public class Pilot0ButtonBehaviour : MonoBehaviour
{
	public NetworkTrigger Pilot0ButtonTrigger;

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

			Debug.Log($"[Pilot0ButtonBehaviour] Triggered by {other.name} on layer {other.gameObject.layer}");

			Pilot0ButtonTrigger.Trigger();

			_ButtonLastTriggered = Time.realtimeSinceStartup;
		}
	}
}
