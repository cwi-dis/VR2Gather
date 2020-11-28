using System.Collections.Generic;
using UnityEngine;

namespace Pilots
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
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_HandGrabEvent, typeof(HandInteractionManager.HandGrabEvent));
			OrchestratorController.Instance.Subscribe<HandInteractionManager.HandGrabEvent>(OnHandGrabEvent);
		}

		public void OnDisable()
		{
			OrchestratorController.Instance.Unsubscribe<HandInteractionManager.HandGrabEvent>(OnHandGrabEvent);
		}

		public static void RegisterGrabbable(Grabbable grabbable)
		{
			Instance.RegisterGrabbableInternal(grabbable);
		}

		public static void UnregisterGrabbable(Grabbable grabbable)
		{
			Instance.UnregisterGrabbableInternal(grabbable);
		}

		private void RegisterGrabbableInternal(Grabbable grabbable)
		{
			_GrabbableObjects[grabbable.NetworkId] = grabbable;
		}

		private void UnregisterGrabbableInternal(Grabbable grabbable)
		{
			_GrabbableObjects.Remove(grabbable.NetworkId);
		}

		private void OnHandGrabEvent(HandInteractionManager.HandGrabEvent handGrabEvent)
		{
			HandleHandGrabEvent(handGrabEvent);
		}

		public void HandleHandGrabEvent(HandInteractionManager.HandGrabEvent handGrabEvent)
		{
			Grabbable grabbable = _GrabbableObjects[handGrabEvent.GrabbableObjectId];
			NetworkPlayer player = SessionPlayersManager.Instance.Players[handGrabEvent.UserId];
			HandInteractionManager handInteractionManager = player.GetHandInteractionManager(handGrabEvent.Handedness);

			if (handGrabEvent.EventType == HandInteractionManager.HandInteractionEventType.Grab)
			{
				grabbable.OnGrab(handInteractionManager);
			}
			else
			{
				grabbable.OnRelease(handInteractionManager);
			}

			if (OrchestratorController.Instance.UserIsMaster)
			{
				OrchestratorController.Instance.SendTypeEventToAll(handGrabEvent, true);
			}
		}
	}
}