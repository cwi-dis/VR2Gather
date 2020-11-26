using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pilots
{

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

		public MessageForwarderManager()
		{
			//Map a type to a specific integer ID. IDs can be chosen freely, as long as all clients have the same ID assignments
			AddTypeIdMapping(100, typeof(NetworkPlayer.NetworkPlayerData));
			AddTypeIdMapping(101, typeof(HandController.HandControllerData));
			AddTypeIdMapping(102, typeof(NetworkTrigger.NetworkTriggerData));
			AddTypeIdMapping(103, typeof(SessionPlayersManager.PlayerLocationData));
			AddTypeIdMapping(104, typeof(SessionPlayersManager.PlayerLocationDataRequest));
			AddTypeIdMapping(105, typeof(SessionPlayersManager.PlayerLocationChangeRequest));
			AddTypeIdMapping(106, typeof(HandInteractionManager.HandGrabEvent));
			AddTypeIdMapping(107, typeof(NetworkTransformSyncBehaviour.NetworkTransformSyncData));
			AddTypeIdMapping(108, typeof(Grabbable.RigidbodySyncMessage));
			AddTypeIdMapping(109, typeof(TextChatManager.TextChatDataMessage));
			AddTypeIdMapping(110, typeof(TilingConfigDistributor.TilingConfigMessage));
			AddTypeIdMapping(111, typeof(Pilot3ExperienceController.InitCompleteMessage));
			AddTypeIdMapping(112, typeof(KeywordResponseListener.KeywordsResponseData));
			AddTypeIdMapping(113, typeof(PlayerTransformSyncBehaviour.PlayerTransformSyncData));
			AddTypeIdMapping(114, typeof(Pilot3SequenceController.AddPlayerToSequenceData));
		}

		private void AddTypeIdMapping(int typeId, Type type)
		{
			if (!type.IsSubclassOf(typeof(BaseMessage)))
			{
				Debug.LogError($"Programmer error: [MessageForwarder] The type {type.ToString()} is not derived from BaseMessage. Please ensure all types used in type mappings derived from BaseMessage.");
				return;
			}

			if (TypeFromId.ContainsKey(typeId))
			{
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
				return;
			}

			((MessageForwarder<T>)forwarder).Unsubscribe(callback);
		}

		public void Forward(string jsonMessage)
		{
			TypedMessage message = JsonUtility.FromJson<TypedMessage>(jsonMessage);
			if (!TypeFromId.TryGetValue(message.TypeId, out Type messageType))
			{
				Debug.LogError($"Programmer error: [MessageForwarder] No type known with TypeId = {message.TypeId}! Has the type mapping been added to MessageForwarder.cs?");
				return;
			}

			if (MessageForwarders.ContainsKey(messageType))
			{
				var forwarder = MessageForwarders[messageType];
				if (forwarder != null)
				{
					forwarder.Forward(message.Data);
				}
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

		public void Forward(T message)
		{
			_ev(message);
		}

		void IMessageForwarder.Forward(string message)
		{
			T deserialized_message = JsonUtility.FromJson<T>(message);
			Forward(deserialized_message);
		}

		public void Subscribe(Action<T> callback)
		{
			_ev += callback;
		}

		public void Unsubscribe(Action<T> callback)
		{
			_ev -= callback;
		}
	}
}