using Orchestrator;
using UnityEngine;

namespace Pilots
{
    [RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(NetworkTransformSyncBehaviour))]
	public class RigidBodyController : MonoBehaviour
	{
		private Rigidbody _Rigidbody;
		private NetworkTransformSyncBehaviour _TransformSync;

		public void Awake()
		{
			_TransformSync = GetComponent<NetworkTransformSyncBehaviour>();
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