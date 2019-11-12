using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class StringParser
{
    public static float GetParsedStringToFloat(string compareValue, float defaultValue)
    {
        float result = defaultValue;

        if (!string.IsNullOrEmpty(compareValue))
        {
            try
            {
                result = float.Parse(compareValue, CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                Debug.LogError("Float parsing error: " + e.Message);
            }
        }
        return result;
    }

    public static bool GetParsedStringToBool(string compareValue, bool defaultValue)
    {
        bool result = defaultValue;
        if (!string.IsNullOrEmpty(compareValue))
        {
            if (compareValue.Equals("true"))
            {
                result = true;
            }
            else if (compareValue.Equals("false"))
            {
                result = false;
            }
            else
            {
                Debug.LogError("Mistmatch for bool value: " + compareValue + ".");
            }
        }
        return result;
    }

    public static Vector3 GetParsedStringToVector3(string compareValue, Vector3 defaultValue)
    {
        Vector3 vec = defaultValue;

        if (!string.IsNullOrEmpty(compareValue))
        {
            int count = compareValue.Count(c => c == ';');
            if (count == 2)
            {
                List<string> strValues = compareValue.Split(';').ToList<string>();
                vec.x = float.Parse(strValues[0], CultureInfo.InvariantCulture);
                vec.y = float.Parse(strValues[1], CultureInfo.InvariantCulture);
                vec.z = float.Parse(strValues[2], CultureInfo.InvariantCulture);
            }
            else
            {
                Debug.LogError("Error parsing the Value for a Vector3: '" + compareValue + "'");
            }
        }
        return vec;
    }

    public static Vector2 GetParsedStringToVector2(string compareValue, Vector2 defaultValue)
    {
        Vector2 vec = defaultValue;
        if (!string.IsNullOrEmpty(compareValue))
        {
            int count = compareValue.Count(c => c == ';');
            if (count == 1)
            {
                List<string> strValues = compareValue.Split(';').ToList<string>();
                vec.x = float.Parse(strValues[0], CultureInfo.InvariantCulture);
                vec.y = float.Parse(strValues[1], CultureInfo.InvariantCulture);
            }
            else
            {
                Debug.LogError("Error parsing the Value for Vector2: '" + compareValue + "'");
            }
        }
        return vec;
    }
}
