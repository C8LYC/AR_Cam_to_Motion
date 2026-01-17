using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MiniJSON;
using UnityEditor;
using UnityEngine;

public class MotionConfigImporterWindow : EditorWindow
{
    TextAsset m_ConfigJson;
    string m_OutputFolder = "Assets/MotionJudge/Generated";

    [MenuItem("Tools/MotionJudge/Import Motion Config")]
    static void ShowWindow()
    {
        GetWindow<MotionConfigImporterWindow>("Motion Config Importer");
    }

    void OnGUI()
    {
        m_ConfigJson = (TextAsset)EditorGUILayout.ObjectField("Config JSON", m_ConfigJson, typeof(TextAsset), false);
        m_OutputFolder = EditorGUILayout.TextField("Output Folder", m_OutputFolder);
        DrawPreview();
        using (new EditorGUI.DisabledScope(m_ConfigJson == null))
        {
            if (GUILayout.Button("Import"))
                ImportConfig();
        }
    }

    void ImportConfig()
    {
        if (m_ConfigJson == null)
        {
            Debug.LogError("No config JSON assigned.");
            return;
        }

        string jsonText = m_ConfigJson.text;
        if (!string.IsNullOrEmpty(jsonText) && jsonText[0] == '\uFEFF')
            jsonText = jsonText.Substring(1);

        object data = Json.Deserialize(jsonText);
        if (!(data is Dictionary<string, object> root))
        {
            string snippet = jsonText ?? string.Empty;
            snippet = snippet.Replace("\r", "").Replace("\n", "\\n");
            string leading = snippet.Length > 200 ? snippet.Substring(0, 200) : snippet;
            string trailing = snippet.Length > 200
                ? snippet.Substring(Mathf.Max(0, snippet.Length - 200))
                : snippet;
            string typeName = data == null ? "null" : data.GetType().FullName;
            Debug.LogError($"Config JSON is not an object. Type: {typeName}. Length: {snippet.Length}. Leading: \"{leading}\" Trailing: \"{trailing}\"");
            return;
        }

        EnsureFolder(m_OutputFolder);
        string actionsFolder = $"{m_OutputFolder}/Actions";
        string statesFolder = $"{m_OutputFolder}/States";
        EnsureFolder(actionsFolder);
        EnsureFolder(statesFolder);

        List<object> actions = GetList(root, "actions");
        if (actions == null)
        {
            Debug.LogError("No actions array found.");
            return;
        }

        foreach (object actionObj in actions)
        {
            if (!(actionObj is Dictionary<string, object> actionDict))
                continue;

            string actionName = GetString(actionDict, "action_name");
            if (string.IsNullOrEmpty(actionName))
                continue;

            string initialStateName = GetString(actionDict, "initial_state");
            Dictionary<string, object> states = GetDict(actionDict, "states");
            if (states == null || states.Count == 0)
                continue;

            List<string> stateNames = BuildStateNameList(states, initialStateName);
            string actionAssetPath = $"{actionsFolder}/{SanitizeFileName(actionName)}.asset";
            Action actionAsset = CreateOrLoadAsset<Action>(actionAssetPath);
            if (actionAsset == null)
                continue;

            actionAsset.actionName = actionName;
            actionAsset.IsActive = true;
            actionAsset.motionStates.Clear();

            string actionStatesFolder = $"{statesFolder}/{SanitizeFileName(actionName)}";
            EnsureFolder(actionStatesFolder);

            var nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var stateKeysInOrder = new List<string>();
            for (int i = 0; i < stateNames.Count; i++)
            {
                string stateKey = stateNames[i];
                if (!states.TryGetValue(stateKey, out var stateObj) || !(stateObj is Dictionary<string, object> stateDict))
                    continue;

                MotionState stateAsset = CreateOrLoadState(actionStatesFolder, stateKey);
                if (stateAsset == null)
                    continue;

                stateAsset.stateName = GetString(stateDict, "state_name", stateKey);
                stateAsset.exit_time = (float)GetNumber(stateDict, "exit_time");
                stateAsset.hold_time = (float)GetNumber(stateDict, "hold_time");
                stateAsset.conditions.Clear();

                AddAngleConditions(stateAsset, GetList(stateDict, "angle_conditions"));
                AddPositionConditions(stateAsset, GetList(stateDict, "position_conditions"));
                AddVelocityConditions(stateAsset, GetList(stateDict, "velocity_conditions"));
                AddTimeDifferentConditions(stateAsset, GetList(stateDict, "time_different_condition"));

                EditorUtility.SetDirty(stateAsset);
                actionAsset.motionStates.Add(stateAsset);
                nameToIndex[stateKey] = actionAsset.motionStates.Count - 1;
                stateKeysInOrder.Add(stateKey);
            }

            for (int i = 0; i < actionAsset.motionStates.Count && i < stateKeysInOrder.Count; i++)
            {
                MotionState state = actionAsset.motionStates[i];
                if (state == null)
                    continue;

                string stateKey = stateKeysInOrder[i];
                if (!states.TryGetValue(stateKey, out var stateObj) || !(stateObj is Dictionary<string, object> stateDict))
                    continue;

                List<object> nextStates = GetList(stateDict, "next_states");
                state.nextStateIndexs = BuildNextStateIndexes(nextStates, nameToIndex);
                EditorUtility.SetDirty(state);
            }

            EditorUtility.SetDirty(actionAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Motion config import completed.");
    }

    static MotionState CreateOrLoadState(string folder, string stateName)
    {
        string path = $"{folder}/{SanitizeFileName(stateName)}.asset";
        return CreateOrLoadAsset<MotionState>(path);
    }

    static void AddAngleConditions(MotionState state, List<object> list)
    {
        if (list == null)
            return;

        foreach (object obj in list)
        {
            if (!(obj is Dictionary<string, object> dict))
                continue;

            string angleName = GetString(dict, "angle_name");
            if (!TryParseAngleName(angleName, out int start, out int middle, out int end))
                continue;

            if (!MediaPipeJointMap.TryGetJoint(start, out var startJoint) ||
                !MediaPipeJointMap.TryGetJoint(middle, out var middleJoint) ||
                !MediaPipeJointMap.TryGetJoint(end, out var endJoint))
            {
                continue;
            }

            var condition = new AngleCondition
            {
                startJoint = startJoint,
                middleJoint = middleJoint,
                endJoint = endJoint,
                minAngle = (float)GetNumber(dict, "min_angle"),
                maxAngle = (float)GetNumber(dict, "max_angle")
            };
            state.conditions.Add(condition);
        }
    }

    static void AddPositionConditions(MotionState state, List<object> list)
    {
        if (list == null)
            return;

        foreach (object obj in list)
        {
            if (!(obj is Dictionary<string, object> dict))
                continue;

            int p1 = (int)GetNumber(dict, "point1");
            int p2 = (int)GetNumber(dict, "point2");
            if (!MediaPipeJointMap.TryGetJoint(p1, out var j1) ||
                !MediaPipeJointMap.TryGetJoint(p2, out var j2))
            {
                continue;
            }

            var condition = new PositionAxisCondition
            {
                point1 = j1,
                point2 = j2,
                axis = ParseAxis(GetString(dict, "axis")),
                comparison = ParseComparison(GetString(dict, "comparison")),
                threshold = (float)GetNumber(dict, "threshold")
            };
            state.conditions.Add(condition);
        }
    }

    static void AddVelocityConditions(MotionState state, List<object> list)
    {
        if (list == null)
            return;

        foreach (object obj in list)
        {
            if (!(obj is Dictionary<string, object> dict))
                continue;

            int point = (int)GetNumber(dict, "point");
            if (!MediaPipeJointMap.TryGetJoint(point, out var joint))
                continue;

            var condition = new VelocityAxisCondition
            {
                point = joint,
                axis = ParseAxis(GetString(dict, "axis")),
                comparison = ParseComparison(GetString(dict, "comparison")),
                threshold = (float)GetNumber(dict, "threshold"),
                requiredTimeSeconds = (float)GetNumber(dict, "required_time_span")
            };
            state.conditions.Add(condition);
        }
    }

    static void AddTimeDifferentConditions(MotionState state, List<object> list)
    {
        if (list == null)
            return;

        foreach (object obj in list)
        {
            if (!(obj is Dictionary<string, object> dict))
                continue;

            int point = (int)GetNumber(dict, "point");
            if (!MediaPipeJointMap.TryGetJoint(point, out var joint))
                continue;

            var condition = new TimeDifferentCondition
            {
                point = joint,
                axis = ParseAxis(GetString(dict, "axis")),
                reference = ParseTimeReference(GetString(dict, "reference")),
                comparison = ParseTimeComparison(GetString(dict, "comparison")),
                threshold = (float)GetNumber(dict, "threshold"),
                windowSeconds = (float)GetNumber(dict, "window_seconds")
            };
            state.conditions.Add(condition);
        }
    }

    static int[] BuildNextStateIndexes(List<object> nextStates, Dictionary<string, int> nameToIndex)
    {
        if (nextStates == null || nextStates.Count == 0)
            return new int[0];

        var indexes = new List<int>();
        for (int i = 0; i < nextStates.Count; i++)
        {
            string name = nextStates[i] as string;
            if (string.IsNullOrEmpty(name))
                continue;

            if (nameToIndex.TryGetValue(name, out int index))
                indexes.Add(index);
        }
        return indexes.ToArray();
    }

    static List<string> BuildStateNameList(Dictionary<string, object> states, string initialStateName)
    {
        var names = new List<string>(states.Keys);
        names.Sort(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(initialStateName) && names.Remove(initialStateName))
            names.Insert(0, initialStateName);
        return names;
    }

    static AxisSelector ParseAxis(string axis)
    {
        switch ((axis ?? string.Empty).ToLowerInvariant())
        {
            case "x":
                return AxisSelector.X;
            case "y":
                return AxisSelector.Y;
            default:
                return AxisSelector.Z;
        }
    }

    static AxisComparison ParseComparison(string comparison)
    {
        switch ((comparison ?? string.Empty).ToLowerInvariant())
        {
            case "less":
                return AxisComparison.Less;
            case "greater":
                return AxisComparison.Greater;
            case "less_or_equal":
            case "less_equal":
                return AxisComparison.LessOrEqual;
            case "greater_or_equal":
            case "greater_equal":
                return AxisComparison.GreaterOrEqual;
            case "equal":
                return AxisComparison.Equal;
            case "not_equal":
                return AxisComparison.NotEqual;
            default:
                return AxisComparison.Greater;
        }
    }

    static TimeReference ParseTimeReference(string reference)
    {
        switch ((reference ?? string.Empty).ToLowerInvariant())
        {
            case "min":
                return TimeReference.Min;
            default:
                return TimeReference.Max;
        }
    }

    static TimeComparison ParseTimeComparison(string comparison)
    {
        switch ((comparison ?? string.Empty).ToLowerInvariant())
        {
            case "above_by":
                return TimeComparison.AboveBy;
            default:
                return TimeComparison.BelowBy;
        }
    }

    static bool TryParseAngleName(string angleName, out int start, out int middle, out int end)
    {
        start = middle = end = -1;
        if (string.IsNullOrEmpty(angleName))
            return false;

        MatchCollection matches = Regex.Matches(angleName, @"\d+");
        if (matches.Count < 3)
            return false;

        start = int.Parse(matches[0].Value);
        middle = int.Parse(matches[1].Value);
        end = int.Parse(matches[2].Value);
        return true;
    }

    static double GetNumber(Dictionary<string, object> dict, string key)
    {
        if (dict != null && dict.TryGetValue(key, out var value) && value != null)
            return Convert.ToDouble(value);
        return 0d;
    }

    static string GetString(Dictionary<string, object> dict, string key, string fallback = "")
    {
        if (dict != null && dict.TryGetValue(key, out var value) && value is string s)
            return s;
        return fallback;
    }

    static List<object> GetList(Dictionary<string, object> dict, string key)
    {
        if (dict != null && dict.TryGetValue(key, out var value) && value is List<object> list)
            return list;
        return null;
    }

    static Dictionary<string, object> GetDict(Dictionary<string, object> dict, string key)
    {
        if (dict != null && dict.TryGetValue(key, out var value) && value is Dictionary<string, object> result)
            return result;
        return null;
    }

    void DrawPreview()
    {
        if (m_ConfigJson == null)
            return;

        string jsonText = m_ConfigJson.text ?? string.Empty;
        if (!string.IsNullOrEmpty(jsonText) && jsonText[0] == '\uFEFF')
            jsonText = jsonText.Substring(1);

        string snippet = jsonText.Replace("\r", "").Replace("\n", "\\n");
        if (snippet.Length > 200)
            snippet = snippet.Substring(0, 200);

        EditorGUILayout.LabelField("Preview (first 200 chars)");
        EditorGUILayout.HelpBox(snippet, MessageType.None);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent))
            parent = "Assets";

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static string SanitizeFileName(string name)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
