using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FindObjectsWithScriptEditor : EditorWindow
{
    [MenuItem("Tools/Find Objects With VideoQualityRating")]
    private static void FindObjectsWithScript()
    {
        List<GameObject> foundObjects = new List<GameObject>();
        foreach (QuestionManager script in GameObject.FindObjectsOfType<QuestionManager>())
        {
            foundObjects.Add(script.gameObject);
        }

        // Print found objects in the console
        Debug.Log("Found " + foundObjects.Count + " GameObjects with VideoQualityRating:");
        foreach (GameObject obj in foundObjects)
        {
            Debug.Log(obj.name, obj);
        }
    }
}
