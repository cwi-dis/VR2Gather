using System.Collections.Generic;
using UnityEngine;

namespace VRT.Pilots.Common
{
	public class NetworkIdManager
	{
		private Dictionary<string, NetworkIdBehaviour> _NetworkIdBehaviours;

		static NetworkIdManager _Instance;

		private NetworkIdManager()
		{
			_NetworkIdBehaviours = new Dictionary<string, NetworkIdBehaviour>();
		}

		public static bool Add(NetworkIdBehaviour behaviour)
		{
			if (_Instance == null)
			{
				_Instance = new NetworkIdManager();
			}

			return _Instance.InternalAdd(behaviour);
		}

		public static void Remove(NetworkIdBehaviour behaviour)
		{
			if (_Instance == null)
			{
				_Instance = new NetworkIdManager();
			}

			_Instance.InternalRemove(behaviour);
		}

		private bool InternalAdd(NetworkIdBehaviour behaviour)
		{
			if (!_NetworkIdBehaviours.ContainsKey(behaviour.NetworkId))
			{
				_NetworkIdBehaviours.Add(behaviour.NetworkId, behaviour);
				return true;
			}

			var existingBehaviour = _NetworkIdBehaviours[behaviour.NetworkId];
			if (existingBehaviour != null && existingBehaviour != behaviour)
			{
				if (Application.isPlaying)
				{
					Debug.LogError($"NetworkIdManager: Colliding network Id {behaviour.NetworkId} Couldn't add NetworkIdBehaviour {behaviour}");
				}
				Debug.Log($"NetworkIdManager: id {behaviour.NetworkId} registered to {behaviour}");
				return false;
			}

			return true;
		}

		private void InternalRemove(NetworkIdBehaviour behaviour)
		{
			_NetworkIdBehaviours.Remove(behaviour.NetworkId);
		}
	}
}