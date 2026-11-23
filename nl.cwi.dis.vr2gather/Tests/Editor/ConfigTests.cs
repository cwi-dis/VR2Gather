using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using VRT.Core;

namespace VRT.Tests.Editor
{
    // The project's config.json (and config-user.json, if there is one: it is per machine and not
    // in git) loads into VRTConfig without errors, and every key in it is a field VRTConfig knows
    // (#416). JsonUtility silently ignores unknown keys, so a renamed setting (like
    // PositionTracker → PositionRecorderConfig, #246) would otherwise fall back to its default
    // without anyone noticing.
    public class ConfigTests
    {
        static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
        static string ConfigPath => Path.Combine(ProjectRoot, "config.json");
        static string UserConfigPath => Path.Combine(ProjectRoot, "config-user.json");

        [Test]
        public void ConfigJsonKeysAreKnown()
        {
            if (!File.Exists(ConfigPath)) Assert.Ignore($"No config.json in {ProjectRoot}");
            AssertKeysKnown(ConfigPath, typeof(VRTConfig));
        }

        [Test]
        public void UserConfigJsonKeysAreKnown()
        {
            if (!File.Exists(UserConfigPath)) Assert.Ignore($"No config-user.json in {ProjectRoot} (it is per machine)");
            AssertKeysKnown(UserConfigPath, typeof(VRTConfig.RepresentationConfigType));
        }

        // What VRTConfig.Initialize() does with the files, without its side effects (statistics,
        // cwipc settings, DontDestroyOnLoad). An error logged by _PostLoad() or _Validate() fails the test.
        [Test]
        public void ConfigLoads()
        {
            if (!File.Exists(ConfigPath)) Assert.Ignore($"No config.json in {ProjectRoot}");
            var go = new GameObject("ConfigTests");
            try
            {
                var config = go.AddComponent<VRTConfig>();
                config.configVersion = 0;
                JsonUtility.FromJsonOverwrite(File.ReadAllText(ConfigPath), config);
                if (File.Exists(UserConfigPath))
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(UserConfigPath), config.RepresentationConfig);
                config._PostLoad();
                config._Validate();
                Assert.AreEqual(VRTConfig.CurrentConfigVersion, config.configVersion, "configVersion in config.json");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void AssertKeysKnown(string path, Type type)
        {
            object json = MiniJson.Parse(File.ReadAllText(path));
            var problems = new List<string>();
            CheckKeys(json, type, "", problems);
            if (problems.Count > 0)
                Assert.Fail($"{Path.GetFileName(path)}: {problems.Count} key(s) {type.Name} doesn't have (ignored when loading):\n" +
                            string.Join("\n", problems.Select(p => "    " + p)));
        }

        // Walks the JSON alongside the type's fields as JsonUtility serializes them.
        static void CheckKeys(object json, Type type, string path, List<string> problems)
        {
            if (json is Dictionary<string, object> obj)
            {
                if (IsLeaf(type)) return;
                foreach (var kv in obj)
                {
                    var field = SerializedField(type, kv.Key);
                    if (field == null)
                        problems.Add(path + kv.Key);
                    else
                        CheckKeys(kv.Value, field.FieldType, path + kv.Key + ".", problems);
                }
            }
            else if (json is List<object> list)
            {
                Type element = type.IsArray ? type.GetElementType()
                    : type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? type.GetGenericArguments()[0]
                    : null;
                if (element == null) return;
                string prefix = path.EndsWith(".") ? path.Substring(0, path.Length - 1) : path;
                for (int i = 0; i < list.Count; i++)
                    CheckKeys(list[i], element, $"{prefix}[{i}].", problems);
            }
        }

        static bool IsLeaf(Type t) => t.IsPrimitive || t.IsEnum || t == typeof(string) || typeof(UnityEngine.Object).IsAssignableFrom(t) && t != typeof(VRTConfig);

        static FieldInfo SerializedField(Type type, string name)
        {
            for (var t = type; t != null && t != typeof(MonoBehaviour) && t != typeof(object); t = t.BaseType)
            {
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f == null) continue;
                if (f.IsNotSerialized) return null;
                if (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null) return f;
                return null;
            }
            return null;
        }

        // A minimal JSON reader: objects become Dictionary<string, object>, arrays List<object>,
        // everything else string, double, bool or null. Enough for config files.
        static class MiniJson
        {
            public static object Parse(string text)
            {
                int i = 0;
                object value = ParseValue(text, ref i);
                SkipWhite(text, ref i);
                if (i != text.Length) throw new FormatException($"JSON: unexpected text at {i}");
                return value;
            }

            static void SkipWhite(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

            static object ParseValue(string s, ref int i)
            {
                SkipWhite(s, ref i);
                if (i >= s.Length) throw new FormatException("JSON: unexpected end");
                char c = s[i];
                if (c == '{')
                {
                    var obj = new Dictionary<string, object>();
                    i++; SkipWhite(s, ref i);
                    if (s[i] == '}') { i++; return obj; }
                    while (true)
                    {
                        SkipWhite(s, ref i);
                        string key = ParseString(s, ref i);
                        SkipWhite(s, ref i);
                        Expect(s, ref i, ':');
                        obj[key] = ParseValue(s, ref i);
                        SkipWhite(s, ref i);
                        if (s[i] == ',') { i++; continue; }
                        Expect(s, ref i, '}');
                        return obj;
                    }
                }
                if (c == '[')
                {
                    var list = new List<object>();
                    i++; SkipWhite(s, ref i);
                    if (s[i] == ']') { i++; return list; }
                    while (true)
                    {
                        list.Add(ParseValue(s, ref i));
                        SkipWhite(s, ref i);
                        if (s[i] == ',') { i++; continue; }
                        Expect(s, ref i, ']');
                        return list;
                    }
                }
                if (c == '"') return ParseString(s, ref i);
                if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
                if (s.Substring(i).StartsWith("false")) { i += 5; return false; }
                if (s.Substring(i).StartsWith("null")) { i += 4; return null; }
                int start = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                if (start == i) throw new FormatException($"JSON: unexpected '{c}' at {i}");
                return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
            }

            static void Expect(string s, ref int i, char c)
            {
                if (i >= s.Length || s[i] != c) throw new FormatException($"JSON: expected '{c}' at {i}");
                i++;
            }

            static string ParseString(string s, ref int i)
            {
                Expect(s, ref i, '"');
                var sb = new StringBuilder();
                while (s[i] != '"')
                {
                    if (s[i] == '\\')
                    {
                        i++;
                        char e = s[i];
                        if (e == 'u') { sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                        else sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == 'r' ? '\r' : e == 'b' ? '\b' : e == 'f' ? '\f' : e);
                    }
                    else sb.Append(s[i]);
                    i++;
                }
                i++;
                return sb.ToString();
            }
        }
    }
}
