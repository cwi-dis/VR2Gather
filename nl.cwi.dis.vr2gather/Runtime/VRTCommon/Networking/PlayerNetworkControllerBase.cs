using UnityEngine;
using UnityEngine.SpatialTracking;
using VRT.Orchestrator.Wrapping;
using VRT.Core;

namespace VRT.Pilots.Common
{
	public abstract class PlayerNetworkControllerBase : MonoBehaviour
	{
		#region NetworkPlayerData
		/// <summary>
		/// Message class to store and transmit hand and head positions and orientations and
		/// current representation.
		/// </summary>
		public class NetworkPlayerData : BaseMessage
		{
			public Vector3 BodyPosition;
			public Quaternion BodyOrientation;
			public Vector3 HeadPosition;
			public Quaternion HeadOrientation;
			public Vector3 LeftHandPosition;
			public Quaternion LeftHandOrientation;
			public Vector3 RightHandPosition;
			public Quaternion RightHandOrientation;
			public UserRepresentationType representation;
			public float BodySize;
		}
		#endregion

		[DisableEditing]
		public string UserId;

		[Tooltip("The body of the user")]
		public Transform BodyTransform;
		[Tooltip("The virtual head to rotate")]
		public Transform HeadTransform;
        [Tooltip("Another virtual head to rotate")]
        public Transform Head2Transform;
        [Tooltip("Another virtual head to rotate and also move")]
        public Transform Head3TransformAlsoMove;
        [Tooltip("Left hand for which to synchronise position/orientation")]
		public Transform LeftHandTransform;
		[Tooltip("Right hand for which to synchronise position/orientation")]
		public Transform RightHandTransform;
		[Tooltip("Alternative user representation (for example Mediascape costume)")]
		public GameObject AlternativeUserRepresentation;
		[Tooltip("How often position/orientation data is synchronized")]
		public int SendRate = 10; //Send out 10 "frames" per second

		[Header("Introspection/debugging")]
		[DisableEditing][SerializeField] protected bool _IsLocalPlayer = true;
		[DisableEditing][SerializeField] protected PlayerControllerBase playerController;
        public bool IsLocalPlayer
		{
			get
			{
				return _IsLocalPlayer;
			}
		}

		private NetworkPlayerData _PreviousReceivedData;
		private NetworkPlayerData _LastReceivedData;
		private float _LastReceiveTime;

		virtual public string Name()
		{
			return $"{GetType().Name}";
		}

		protected virtual void Start()
		{
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_NetworkPlayerData, typeof(NetworkPlayerData));
			OrchestratorController.Instance.Subscribe<NetworkPlayerData>(OnNetworkPlayerData);
		}

		public abstract void SetupPlayerNetworkController(PlayerControllerBase _playerController, bool local, string _userId);
	
		protected void OnNetworkPlayerData(NetworkPlayerData data)
		{
			if (!IsLocalPlayer && UserId == data.SenderId)
			{
				if (OrchestratorController.Instance.UserIsMaster)
				{
					//We're the master, so inform the others
					OrchestratorController.Instance.SendTypeEventToAll(data, true);
				}
				if (data.representation != playerController.userRepresentation)
				{
					// User representation has changed.
					Debug.Log($"{Name()}: Representation changed to {data.representation}");
					playerController.SetRepresentation(data.representation);
				}
                // Adjust size, if needed
                if (data.BodySize != 0)
                {
					float newSize = data.BodySize;
					GameObject currentRepresentation = playerController.GetRepresentationGameObject();
                    if (currentRepresentation != null && currentRepresentation.activeInHierarchy)
                    {
                        float oldSize = currentRepresentation.transform.transform.localScale.y;
                        if (newSize != oldSize)
                        {
                            Debug.Log($"{Name()}: Change size from {oldSize} to {newSize}");
                            currentRepresentation.transform.transform.localScale = new Vector3(newSize, newSize, newSize);
                        }
                    }
                    if (AlternativeUserRepresentation != null && AlternativeUserRepresentation.activeInHierarchy)
                    {
                        float oldSize = AlternativeUserRepresentation.transform.transform.localScale.y;
                        if (newSize != oldSize)
                        {
                            Debug.Log($"{Name()}: Change AlternativeUserRepresentation size from {oldSize} to {newSize}");
                            AlternativeUserRepresentation.transform.transform.localScale = new Vector3(newSize, newSize, newSize);
                        }
                    }
                }
                _PreviousReceivedData = _LastReceivedData;
				_LastReceivedData = data;
				_LastReceiveTime = Time.realtimeSinceStartup;

				if (_PreviousReceivedData != null)
				{
					//Dirty dirty interpolation. We can/should do better. 
					float t = Mathf.Clamp01((Time.realtimeSinceStartup - _LastReceiveTime) / (1.0f / SendRate));

					if (BodyTransform != null)
                    {
						BodyTransform.position = Vector3.Lerp(_PreviousReceivedData.BodyPosition, _LastReceivedData.BodyPosition, t);
						BodyTransform.rotation = Quaternion.Slerp(_PreviousReceivedData.BodyOrientation, _LastReceivedData.BodyOrientation, t);
					}
					if (HeadTransform != null)
                    {
						//HeadTransform.position = Vector3.Lerp(_PreviousReceivedData.HeadPosition, _LastReceivedData.HeadPosition, t);
						HeadTransform.rotation = Quaternion.Slerp(_PreviousReceivedData.HeadOrientation, _LastReceivedData.HeadOrientation, t);
					}
                    if (Head2Transform != null)
                    {
                        //Head2Transform.position = Vector3.Lerp(_PreviousReceivedData.HeadPosition, _LastReceivedData.HeadPosition, t);
                        Head2Transform.rotation = Quaternion.Slerp(_PreviousReceivedData.HeadOrientation, _LastReceivedData.HeadOrientation, t);
                    }
                    if (Head3TransformAlsoMove != null)
                    {
                        Head3TransformAlsoMove.position = Vector3.Lerp(_PreviousReceivedData.HeadPosition, _LastReceivedData.HeadPosition, t);
                        Head3TransformAlsoMove.rotation = Quaternion.Slerp(_PreviousReceivedData.HeadOrientation, _LastReceivedData.HeadOrientation, t);
                    }
                    if (LeftHandTransform != null)
                    {
						LeftHandTransform.position = Vector3.Lerp(_PreviousReceivedData.LeftHandPosition, _LastReceivedData.LeftHandPosition, t);
						LeftHandTransform.rotation = Quaternion.Slerp(_PreviousReceivedData.LeftHandOrientation, _LastReceivedData.LeftHandOrientation, t);
					}
					if (RightHandTransform != null)
                    {
						RightHandTransform.position = Vector3.Lerp(_PreviousReceivedData.RightHandPosition, _LastReceivedData.RightHandPosition, t);
						RightHandTransform.rotation = Quaternion.Slerp(_PreviousReceivedData.RightHandOrientation, _LastReceivedData.RightHandOrientation, t);
					}
				}
				
			}
		}

		public HandNetworkControllerBase GetHandController(HandNetworkControllerBase.Handedness handedness)
		{
			if (handedness == HandNetworkControllerBase.Handedness.Left)
			{
				return LeftHandTransform.GetComponentInParent<HandNetworkControllerBase>();
			}
			else
			{
				return RightHandTransform.GetComponentInParent<HandNetworkControllerBase>();
			}
		}
	}
}