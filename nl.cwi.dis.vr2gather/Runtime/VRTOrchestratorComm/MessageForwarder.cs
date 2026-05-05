using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.OrchestratorComm
{

	public enum MessageTypeID
	{
		TID_NetworkPlayerData = 100,
		TID_HandControllerData = 101,
		TID_NetworkTriggerData = 102,
		TID_PlayerLocationData = 103,
		TID_PlayerLocationDataRequest = 104,
		TID_PlayerLocationChangeRequest = 105,
		TID_HandGrabEvent = 106,
		TID_RigidBodyData = 107,
		TID_RigidbodySyncMessage = 108,
		TID_TextChatDataMessage = 109,
		TID_TilingConfigMessage = 110,
		TID_InitCompleteMessage = 111,
		TID_KeywordsResponseData = 112,
		TID_PlayerTransformSyncData = 113,
		TID_AddPlayerToSequenceData = 114,
		TID_SyncConfigMessage = 115,
		TID_NetworkInstantiatorData = 116,
		TID_PersistenceManagerData = 117,
	};

	/// <summary>
	/// Manager of message forwarders
	/// User code can subscribe to messages of a certain type
	/// and receive the message of that type when it arrives.
	/// Types are managed by assigning an integer Id in the constructor
	/// </summary>
	public class MessageForwarderManager
	{
		public Dictionary<int, Type> TypeFromId = new Dictionary<int, Type>();
		public Dictionary<Type, int> IdFromType = new Dictionary<Type, int>();

		public Dictionary<Type, IMessageForwarder> MessageForwarders = new Dictionary<Type, IMessageForwarder>();

		public bool WarnOnUnhandled = false;

		public MessageForwarderManager()
		{
		}

		public void AddTypeIdMapping(MessageTypeID _typeId, Type type)
		{
			int typeId = (int)_typeId;
			if (!type.IsSubclassOf(typeof(BaseMessage)))
			{
				Debug.LogError($"Programmer error: [MessageForwarder] The type {type.ToString()} is not derived from BaseMessage. Please ensure all types used in type mappings derived from BaseMessage.");
				return;
			}

			if (TypeFromId.ContainsKey(typeId))
			{
				if (TypeFromId[typeId] == type)
				{
					// Registered before. No problem: the class could have multiple instances
					return;
				}
				Debug.LogError($"Programmer error: [MessageForwarder] A type with typeId {typeId} already exists! Please ensure all type mappings in MessageForwarder.cs have unique typeIds!");
			}
			else
			{
				TypeFromId[typeId] = type;
			}

			if (IdFromType.ContainsKey(type))
			{
				Debug.LogError($"Programmer error: [MessageForwarder] The type {type.ToString()} has already been registered! Please ensure all BaseMessage derived types are only registered once in MessageForwarder.cs!");
			}
			else
			{
				IdFromType[type] = typeId;
			}
		}

		public void Subscribe<T>(Action<T> callback) where T : BaseMessage
		{
			if (!MessageForwarders.TryGetValue(typeof(T), out IMessageForwarder forwarder))
			{
				forwarder = new MessageForwarder<T>();
				MessageForwarders[typeof(T)] = forwarder;
			}

			((MessageForwarder<T>)forwarder).Subscribe(callback);
		}

		public void Unsubscribe<T>(Action<T> callback) where T : BaseMessage
		{
			if (!MessageForwarders.TryGetValue(typeof(T), out IMessageForwarder forwarder))
			{
				Debug.LogWarning($"MessageForwarder: Unsubscribe() for unknown message type");
				return;
			}

			((MessageForwarder<T>)forwarder).Unsubscribe(callback);
		}

		public void Forward(string jsonMessage)
		{
			TypedMessage message = JsonUtility.FromJson<TypedMessage>(jsonMessage);
			if (!TypeFromId.TryGetValue(message.TypeId, out Type messageType))
			{
				Debug.LogWarning($"MessageForwarder: Forward() for unknown message type {message.TypeId}");
				return;
			}

			if (MessageForwarders.ContainsKey(messageType))
			{
				var forwarder = MessageForwarders[messageType];
				if (forwarder != null)
				{
					forwarder.Forward(message.Data);
				}
				else
				{
					if (WarnOnUnhandled) Debug.LogWarning($"MessageForwarder: null forwarder for messageType {messageType.Name}");
				}
			}
			else
			{
				if (WarnOnUnhandled) Debug.LogWarning($"MessageForwarder: no forwarder for messageType {messageType.Name}");
			}
		}
	}

	public interface IMessageForwarder
	{
		void Forward(string message);
	}

	public class MessageForwarder<T> : IMessageForwarder where T : BaseMessage

	{
		event Action<T> _ev;
		int nSubscribed = 0;

		public void Forward(T message)
		{
			if (nSubscribed == 0)
			{
				Debug.LogWarning($"MessageForwarder: Forward() for {typeof(T).Name} but no-one subscribed");
				return;
			}
			_ev?.Invoke(message);
		}

		void IMessageForwarder.Forward(string message)
		{
			T deserialized_message = JsonUtility.FromJson<T>(message);
			Forward(deserialized_message);
		}

		public void Subscribe(Action<T> callback)
		{
			_ev += callback;
			nSubscribed++;
		}

		public void Unsubscribe(Action<T> callback)
		{
			_ev -= callback;
			nSubscribed--;
		}
	}
}
