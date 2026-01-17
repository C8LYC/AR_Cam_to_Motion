using System;
using System.Collections.Generic;
using UnityEngine;

public static class JointLookup
{
    static readonly Dictionary<int, Dictionary<JointIndices, Transform>> s_Cache =
        new Dictionary<int, Dictionary<JointIndices, Transform>>();

    static readonly Dictionary<string, JointIndices> s_NameToJoint =
        BuildNameMap();

    public static Transform GetJoint(Transform root, JointIndices joint)
    {
        if (root == null || joint == JointIndices.Invalid)
            return null;

        int id = root.GetInstanceID();
        if (!s_Cache.TryGetValue(id, out var map) || map == null)
        {
            map = BuildMap(root);
            s_Cache[id] = map;
        }

        if (map.TryGetValue(joint, out var transform))
            return transform;

        return null;
    }

    static Dictionary<string, JointIndices> BuildNameMap()
    {
        var map = new Dictionary<string, JointIndices>(StringComparer.Ordinal);
        Array values = Enum.GetValues(typeof(JointIndices));
        for (int i = 0; i < values.Length; i++)
        {
            var joint = (JointIndices)values.GetValue(i);
            map[joint.ToString()] = joint;
        }
        return map;
    }

    static Dictionary<JointIndices, Transform> BuildMap(Transform root)
    {
        var map = new Dictionary<JointIndices, Transform>();
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform current = all[i];
            if (s_NameToJoint.TryGetValue(current.name, out var joint))
                map[joint] = current;
        }
        return map;
    }
}
