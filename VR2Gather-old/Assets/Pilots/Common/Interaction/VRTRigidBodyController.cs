using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	/// <summary>
	/// Useful for autonomous objects (think: robots) that need to behave the same
	/// in all instance of the experience. 
	/// Master VR2Gather synchronizes position if the rigidbody is sleeping.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(RigidBodyNetworkController))]
	public class VRTRigidBodyController : MonoBehaviour
	{
		private Rigidbody _Rigidbody;
		private RigidBodyNetworkController _TransformSync;
		[Tooltip("Set to kinematic if sleeping (for master user only)")]
		public bool defaultKinematic;
		[Tooltip("Set to gravity if sleeping (for master user only)")]
		public bool defaultGravity;
		public void Awake()
		{
			_TransformSync = GetComponent<RigidBodyNetworkController>();
			_Rigidbody = GetComponent<Rigidbody>();
		}

		public void Update()
		{
			if (_Rigidbody.IsSleeping())
			{
			
				if (OrchestratorController.Instance.UserIsMaster)
				{
					if (defaultKinematic)
					{
						_Rigidbody.isKinematic = true;
						_Rigidbody.useGravity = false;
					}
					else if (defaultGravity)
					{
						_Rigidbody.isKinematic = false;
						_Rigidbody.useGravity = true;
					}
					_TransformSync.DoSync();
				}
				else
                {
					// For non-master participants the object is always kinematic
					// (controlled by the master).
					_Rigidbody.isKinematic = true;
					_Rigidbody.useGravity = false;
				}
			}
		}
	}
}