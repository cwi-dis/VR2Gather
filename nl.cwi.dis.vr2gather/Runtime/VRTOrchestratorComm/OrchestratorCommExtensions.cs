using System;
using UnityEngine;
using VRT.Orchestrator.Wrapping;
using VRT.Orchestrator.Responses;

namespace VRT.OrchestratorComm
{

	/// <summary>
	/// A typed message
	/// </summary>
	public class TypedMessage
	{
		/// <summary>
		/// Integer type Id for the type, registered in the MessageForwarderManager
		/// </summary>
		public int TypeId;

		/// <summary>
		/// json representation of the underlying typed data
		/// </summary>
		public string Data;
	}

	/// <summary>
	/// Extension methods for IVRTOrchestratorComm to facilitate the exchange of
	/// messages with a type Id as well as subscription to messages of a specific type.
	/// </summary>
	public static class OrchestratorCommExtensions
	{
		// All extension methods operate on IVRTOrchestratorComm since they
		// are purely within-session communication helpers.
		private static MessageForwarderManager _MessageForwarderManager = new MessageForwarderManager();


		public static void RegisterEventType(this IVRTOrchestratorComm controller, MessageTypeID typeId, Type T)
		{
			_MessageForwarderManager.AddTypeIdMapping(typeId, T);
		}

		/// <summary>
		/// Send a TypedMessage from the Master to all Users
		/// </summary>
		/// <typeparam name="T">Exact type of the derived BaseMessage type to send</typeparam>
		/// <param name="controller">The orchestrator comm interface on which this extension method is called</param>
		/// <param name="data">The data of type T to send</param>
		/// <param name="forward">Whether or not we forward this message. Defaults to false. Should be set to true if it's a message received by the Master, to be forwarded to all users</param>
		public static void SendTypeEventToAll<T>(this IVRTOrchestratorComm controller, T data, bool forward = false) where T : BaseMessage
		{
			if (controller == null)
			{
				return;
			}
			if (controller.SelfUser == null)
			{
				Debug.LogWarning("OrchestratorCommExtensions: SendTypeEventToAll: controller.SelfUser is null");
				return;
			}

			if (!forward)
			{
				//If we're forwarding this data from another user, don't replace the SenderId
				data.SenderId = controller.SelfUser.userId;
			}

			string json = JsonUtility.ToJson(data);
			int TypeId;

			if (!_MessageForwarderManager.IdFromType.TryGetValue(typeof(T), out TypeId))
			{
				Debug.LogError($"Programmer error: [OrchestratorManager] Cannot find TypeId for type {typeof(T).Name}! Has the type mapping been added?");
				return;
			}

			TypedMessage message = new TypedMessage
			{
				TypeId = TypeId,
				Data = json
			};

			controller.SendEventToAll(JsonUtility.ToJson(message));
		}

		/// <summary>
		/// Send a TypedMessage from a user to the Master
		/// </summary>
		/// <typeparam name="T">Exact type of the derived BaseMessage type to send</typeparam>
		/// <param name="controller">The orchestrator comm interface on which this extension method is called</param>
		/// <param name="data">The data of type T to send</param>
		public static void SendTypeEventToMaster<T>(this IVRTOrchestratorComm controller, T data) where T : BaseMessage
		{
			if (controller == null)
			{
				return;
			}
			if (controller.SelfUser == null)
			{
				Debug.LogWarning("OrchestratorCommExtensions: SendTypeEventToMaster: controller.SelfUser is null");
				return;
			}

			data.SenderId = controller.SelfUser.userId;
			data.TimeStamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss.fff");

			string json = JsonUtility.ToJson(data);
			int TypeId;

			if (!_MessageForwarderManager.IdFromType.TryGetValue(typeof(T), out TypeId))
			{
				Debug.LogError($"Programmer error: [OrchestratorManager] Cannot find TypeId for type {typeof(T).Name}! Has the type mapping been added?");
				return;
			}

			TypedMessage message = new TypedMessage
			{
				TypeId = TypeId,
				Data = json
			};

			controller.SendEventToMaster(JsonUtility.ToJson(message));
		}

		/// <summary>
		/// Send a TypedMessage from the Master to a specific user
		/// </summary>
		/// <typeparam name="T">Exact type of the derived BaseMessage type to send</typeparam>
		/// <param name="controller">The orchestrator comm interface on which this extension method is called</param>
		/// <param name="userId">The userId of the user to which the master wants to send the message</param>
		/// <param name="data">The data of type T to send</param>
		public static void SendTypeEventToUser<T>(this IVRTOrchestratorComm controller, string userId, T data) where T : BaseMessage
		{
			if (controller == null)
			{
				Debug.LogWarning("OrchestratorCommExtensions: SendTypeEventToUser: controller==null");
				return;
			}
			if (controller.SelfUser == null)
			{
				Debug.LogWarning("OrchestratorCommExtensions: SendTypeEventToUser: controller.SelfUser is null");
				return;
			}

			data.SenderId = controller.SelfUser.userId;
			data.TimeStamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss.fff");

			string json = JsonUtility.ToJson(data);
			int TypeId;

			if (!_MessageForwarderManager.IdFromType.TryGetValue(typeof(T), out TypeId))
			{
				Debug.LogError($"Programmer error: [OrchestratorManager] Cannot find TypeId for type {typeof(T).Name}! Has the type mapping been added?");
				return;
			}

			TypedMessage message = new TypedMessage
			{
				TypeId = TypeId,
				Data = json
			};

			controller.SendEventToUser(userId, JsonUtility.ToJson(message));
		}

		/// <summary>
		/// Subscribe to messages of a specific type
		/// </summary>
		/// <typeparam name="T">The typename of what messages you want to subscribe to</typeparam>
		/// <param name="controller">The comm interface on which to call this extension method</param>
		/// <param name="callback">The callback to call upon receiving a message of the specific type</param>
		public static void Subscribe<T>(this IVRTOrchestratorComm controller, Action<T> callback) where T : BaseMessage
		{
			_MessageForwarderManager.Subscribe(callback);
		}

		/// <summary>
		/// Unsubscribe from messages of a specific type
		/// </summary>
		/// <typeparam name="T">The typename of what messages you want to unsubscribe from</typeparam>
		/// <param name="controller">The comm interface on which to call this extension method</param>
		/// <param name="callback">The callback to unsubscribe</param>
		public static void Unsubscribe<T>(this IVRTOrchestratorComm controller, Action<T> callback) where T : BaseMessage
		{
			_MessageForwarderManager.Unsubscribe(callback);
		}

		public static void RegisterMessageForwarder(this IVRTOrchestratorComm controller)
		{
			// Remove before adding to guarantee no double-registration.
			controller.OnUserEventReceivedEvent -= ForwardUserEvent;
			controller.OnUserEventReceivedEvent += ForwardUserEvent;
			controller.OnMasterEventReceivedEvent -= ForwardMasterEvent;
			controller.OnMasterEventReceivedEvent += ForwardMasterEvent;
		}

		public static void UnregisterMessageForwarder(this IVRTOrchestratorComm controller)
		{
			controller.OnUserEventReceivedEvent -= ForwardUserEvent;
			controller.OnMasterEventReceivedEvent -= ForwardMasterEvent;
		}

		private static void ForwardMasterEvent(UserEvent userEvent)
		{
			_MessageForwarderManager.Forward(userEvent.sceneEventData);
		}

		private static void ForwardUserEvent(UserEvent userEvent)
		{
			_MessageForwarderManager.Forward(userEvent.sceneEventData);
		}
	}
}
