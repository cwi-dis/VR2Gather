using UnityEngine;

namespace VRTPilots
{

	/// <summary>
	/// A Unity PropertyAttribute to enable the DisableEditingAttributeDrawer
	/// to provide a custom inspector drawer which disables editing of the field
	/// </summary>
	public class DisableEditingAttribute : PropertyAttribute
	{
		public DisableEditingAttribute()
		{
		}
	}
}