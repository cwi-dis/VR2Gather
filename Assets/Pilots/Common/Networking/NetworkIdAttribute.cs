using UnityEngine;

/// <summary>
/// A Unity PropertyAttribute to enable the NetworkIdAttributeDrawer
/// to provide a custom inspector drawer for NetworkIds of a NetworkIdBehaviour
/// </summary>
public class NetworkIdAttribute : PropertyAttribute
{
	public NetworkIdAttribute()
	{
	}
}