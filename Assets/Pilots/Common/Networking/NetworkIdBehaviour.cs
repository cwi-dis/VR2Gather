using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#endif

public class NetworkIdBehaviour : MonoBehaviour
{
	[NetworkId]
	public string NetworkId;

	void Awake()
	{
		CreateNetworkId();
	}

	public void OnDestroy()
	{
		NetworkIdManager.Remove(this);
	}

	public bool NeedsAction(string networkId)
	{
		return (networkId == NetworkId);
	}


	// Create a new NetworkId when necessary
	void CreateNetworkId()
	{
		if (string.IsNullOrEmpty(NetworkId))
		{
#if UNITY_EDITOR
			// if in editor, make sure we aren't a prefab of some kind
			if (IsAssetOnDisk())
			{
				return;
			}
			Undo.RecordObject(this, "Added GUID");
#endif
			NetworkId = System.Guid.NewGuid().ToString();

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
			CreateNetworkId();
		}
	}
#endif
}