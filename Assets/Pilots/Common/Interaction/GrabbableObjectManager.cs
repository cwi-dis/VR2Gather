using System.Collections.Generic;
using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	public class GrabbableObjectManager : MonoBehaviour
	{
		private Dictionary<string, Grabbable> _GrabbableObjects = new Dictionary<string, Grabbable>();

		private static GrabbableObjectManager _Instance;

		public static GrabbableObjectManager Instance
		{
			get
			{
				if (_Instance is null)
				{
					_Instance = FindObjectOfType<GrabbableObjectManager>();
					if (_Instance == null)
					{
						_Instance = new GameObject("GrabbableObjectManager").AddComponent<GrabbableObjectManager>();
					}
				}
				return _Instance;
			}
		}

		public void Awake()
		{
			DontDestroyOnLoad(this);
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_HandGrabEvent, typeof(HandNetworkControllerBase.HandGrabEvent));
			OrchestratorController.Instance.Subscribe<HandNetworkControllerBase.HandGrabEvent>(OnHandGrabEvent);
		}

		public void OnDisable()
		{
            if(!this.gameObject.scene.isLoaded) return;
			OrchestratorController.Instance.Unsubscribe<HandNetworkControllerBase.HandGrabEvent>(OnHandGrabEvent);
		}

		public static void RegisterGrabbable(Grabbable grabbable)
		{
			Instance.RegisterGrabbableInternal(grabbable);
		}

		public static void UnregisterGrabbable(Grabbable grabbable)
		{
			if (_Instance == null) return;
			Instance.UnregisterGrabbableInternal(grabbable);
		}

		private void RegisterGrabbableInternal(Grabbable grabbable)
		{
			if (_GrabbableObjects.ContainsKey(grabbable.NetworkId) && _GrabbableObjects[grabbable.NetworkId] != grabbable)
            {
				Debug.LogWarning($"GrabbableObjectManager: RegisterGrabbable: NetworkID={grabbable.NetworkId} registered to Grabbable={grabbable}, overriding old Grabbable={_GrabbableObjects[grabbable.NetworkId]}");
            }
			_GrabbableObjects[grabbable.NetworkId] = grabbable;
		}

		private void UnregisterGrabbableInternal(Grabbable grabbable)
		{
			_GrabbableObjects.Remove(grabbable.NetworkId);
		}

		private void OnHandGrabEvent(HandNetworkControllerBase.HandGrabEvent handGrabEvent)
		{
			HandleHandGrabEvent(handGrabEvent);
		}

		public void HandleHandGrabEvent(HandNetworkControllerBase.HandGrabEvent handGrabEvent)
		{
			Grabbable grabbable = _GrabbableObjects[handGrabEvent.GrabbableObjectId];
            PlayerNetworkControllerBase player = SessionPlayersManager.Instance.Players[handGrabEvent.UserId];
			HandNetworkControllerBase handController = player.GetHandController(handGrabEvent.Handedness);

			if (handGrabEvent.EventType == HandNetworkControllerBase.HandInteractionEventType.Grab)
			{
				handController.OnNetworkGrab(grabbable);
			}
			else
			{
				handController.OnNetworkRelease(grabbable);
			}

			if (OrchestratorController.Instance.UserIsMaster)
			{
				OrchestratorController.Instance.SendTypeEventToAll(handGrabEvent, true);
			}
		}
	}
}