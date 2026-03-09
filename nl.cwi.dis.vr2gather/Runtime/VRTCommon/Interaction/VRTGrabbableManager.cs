using System.Collections.Generic;
using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	public class VRTGrabbableManager : MonoBehaviour
	{
		private Dictionary<string, VRTGrabbableController> _GrabbableObjects = new Dictionary<string, VRTGrabbableController>();

		private static VRTGrabbableManager _Instance;

		public bool debug = true;
		public static VRTGrabbableManager Instance
		{
			get
			{
				if (_Instance == null)
				{
					_Instance = FindObjectOfType<VRTGrabbableManager>();
					if (_Instance == null)
					{
						_Instance = new GameObject("VRTGrabbableManager").AddComponent<VRTGrabbableManager>();
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

		public static void RegisterGrabbable(VRTGrabbableController grabbable)
		{
			Instance.RegisterGrabbableInternal(grabbable);
		}

		public static void UnregisterGrabbable(VRTGrabbableController grabbable)
		{
			if (_Instance == null) return;
			Instance.UnregisterGrabbableInternal(grabbable);
		}

		private void RegisterGrabbableInternal(VRTGrabbableController grabbable)
		{
			if (_GrabbableObjects.ContainsKey(grabbable.NetworkId) && _GrabbableObjects[grabbable.NetworkId] != grabbable)
            {
				Debug.LogWarning($"VRTGrabbableManager: RegisterGrabbable: NetworkID={grabbable.NetworkId} registered to Grabbable={grabbable}, overriding old Grabbable={_GrabbableObjects[grabbable.NetworkId]}");
            }
			if (debug)
			{
				Debug.Log($"VRTGrabbableManager: Register id={grabbable.NetworkId}, grabbable={grabbable}");
			}
			_GrabbableObjects[grabbable.NetworkId] = grabbable;
		}

		private void UnregisterGrabbableInternal(VRTGrabbableController grabbable)
		{
            if (debug)
            {
                Debug.Log($"VRTGrabbableManager: Unregister id={grabbable.NetworkId}");
            }
            bool ok = _GrabbableObjects.Remove(grabbable.NetworkId);
			if (!ok)
			{
				Debug.LogWarning($"VRTGrabbableManager: Unregister non-existent id={grabbable.NetworkId}");
			}
		}

		private void OnHandGrabEvent(HandNetworkControllerBase.HandGrabEvent handGrabEvent)
		{
			HandleHandGrabEvent(handGrabEvent);
		}

		public void HandleHandGrabEvent(HandNetworkControllerBase.HandGrabEvent handGrabEvent)
		{
			if (debug)
			{
				Debug.Log($"VRTGrabbableManager: HandleHandGrabEvent: event={handGrabEvent.EventType}, hand={handGrabEvent.Handedness}, id={handGrabEvent.GrabbableObjectId}");
			}
			if (!_GrabbableObjects.ContainsKey(handGrabEvent.GrabbableObjectId))
            {
				Debug.LogError($"VRTGrabbableManager: Grabbing object with unknown ObjectID {handGrabEvent.GrabbableObjectId}");
				return;
            }
			VRTGrabbableController grabbable = _GrabbableObjects[handGrabEvent.GrabbableObjectId];
            PlayerNetworkControllerBase player = SessionPlayersManager.Instance.Players.GetValueOrDefault(handGrabEvent.UserId, null);
			if (player == null)
			{
				Debug.LogError($"VRTGrabbableManager: PlayerID {handGrabEvent.UserId} does not exist");
				return;
			}
			HandNetworkControllerBase handController = player.GetHandController(handGrabEvent.Handedness);
			if (handController == null)
            {
				Debug.LogError($"VRTGrabbableManager: {handGrabEvent.Handedness} hand network controller not found");
				return;
            }
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