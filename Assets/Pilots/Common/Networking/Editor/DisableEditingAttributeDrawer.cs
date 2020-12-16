using UnityEngine;
using UnityEditor;
using VRT.Pilots.Common;

[CustomPropertyDrawer(typeof(DisableEditingAttribute))]
public class DisableEditingAttributeDrawer : PropertyDrawer
{
	// Draw the property inside the given rect
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// First get the attribute since it contains the range for the slider
		NetworkIdAttribute networkIdAttribute = attribute as NetworkIdAttribute;

		bool enabledCache = GUI.enabled;
		GUI.enabled = false;

		EditorGUI.PropertyField(position, property, label);

		GUI.enabled = enabledCache;
	}
}