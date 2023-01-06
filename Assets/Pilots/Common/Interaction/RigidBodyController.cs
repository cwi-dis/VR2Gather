using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	/// <summary>
	/// Useful for autonomous objects (think: robots) that need to behave the same
	/// in all instance of the experience.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(RigidBodyNetworkController))]
	public class RigidBodyController : MonoBehaviour
	{
		private Rigidbody _Rigidbody;
		private RigidBodyNetworkController _TransformSync;

		public void Awake()
		{
			_TransformSync = GetComponent<RigidBodyNetworkController>();
			_Rigidbody = GetComponent<Rigidbody>();
		}

		public void Update()
		{
			if (_Rigidbody.IsSleeping())
			{
				_Rigidbody.isKinematic = true;
				_Rigidbody.useGravity = false;
				if (OrchestratorController.Instance.UserIsMaster)
				{
					_TransformSync.DoSync();
				}
			}
		}
	}
}