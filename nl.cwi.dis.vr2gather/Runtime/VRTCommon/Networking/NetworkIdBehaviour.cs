using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#endif


namespace VRT.Pilots.Common
{
	/// <summary>
	/// Base class for components that exist in multiple players (instances of a VR2Gather experience).
	/// It basically caters for NetworkId's being the same across multiple instances, but different for
	/// each different GameObject. This is done with nifty code that fills the NetworkId field whenever 
	/// a NetworkIdBehaviour is instantiated, *except when this is in a prefab or something* (in which
	/// case it will be instantianted when 
	/// </summary>
	public class NetworkIdBehaviour : MonoBehaviour
	{
		[Tooltip("Don't auto-create a network ID")]
		[SerializeField] private bool noAutoCreateNetworkId;
		[NetworkId]
		public string NetworkId;

		protected virtual void Awake()
		{
			Debug.LogError($"{gameObject.name}: VR2Gather shared objects should not be used in VR2Gather-Fishnet");
			CreateNetworkId(false);
		}

		public void OnDestroy()
		{
			NetworkIdManager.Remove(this);
		}

		public bool NeedsAction(string networkId)
		{
			return networkId == NetworkId;
		}


		// Create a new NetworkId when necessary
		// xxxjack: some of this code should also be executed for non-auto-created networkIDs.
		public void CreateNetworkId(bool forceCreate=true)
		{
			if (string.IsNullOrEmpty(NetworkId))
			{
				if (noAutoCreateNetworkId && !forceCreate)
				{
					Debug.Log($"NetworkIdBehaviour({name}): not creating networkID");
					return;
				}
#if UNITY_EDITOR
				// if in editor, make sure we aren't a prefab of some kind
				if (IsAssetOnDisk())
				{
					return;
				}
				Undo.RecordObject(this, "Added GUID");
#endif
				NetworkId = System.Guid.NewGuid().ToString();
				Debug.Log($"NetworkIdBehaviour({name}: invented {NetworkId}");

#if UNITY_EDITOR
				if (PrefabUtility.IsPartOfNonAssetPrefabInstance(this))
				{
					PrefabUtility.RecordPrefabInstancePropertyModifications(this);
				}
#endif
			}

			if (!string.IsNullOrEmpty(NetworkId))
			{
				if (!NetworkIdManager.Add(this))
				{
					NetworkId = string.Empty;
					CreateNetworkId();
				}
			}
		}

#if UNITY_EDITOR
		private bool IsEditingInPrefabMode()
		{
			if (EditorUtility.IsPersistent(this))
			{
				// if the game object is stored on disk, it is a prefab of some kind, despite not returning true for IsPartOfPrefabAsset =/
				return true;
			}
			else
			{
				// If the GameObject is not persistent let's determine which stage we are in first because getting Prefab info depends on it
				var mainStage = StageUtility.GetMainStageHandle();
				var currentStage = StageUtility.GetStageHandle(gameObject);
				if (currentStage != mainStage)
				{
					var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
					if (prefabStage != null)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool IsAssetOnDisk()
		{
			return PrefabUtility.IsPartOfPrefabAsset(this) || IsEditingInPrefabMode();
		}

		public void OnValidate()
		{
#if UNITY_EDITOR
			// similar to on Serialize, but gets called on Copying a Component or Applying a Prefab
			// at a time that lets us detect what we are
			if (IsAssetOnDisk())
			{
				NetworkId = string.Empty;
			}
			else
#endif
			{
				CreateNetworkId(false);
			}
		}
#endif
	}
}