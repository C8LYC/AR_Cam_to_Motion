using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TimeDifferentCondition : MotionCondition
{
    public Transform skeletonRoot;
    public JointIndices point;
    public AxisSelector axis;
    public TimeReference reference;
    public TimeComparison comparison;
    public float threshold;
    public float windowSeconds = 1f;

    readonly List<Sample> m_Samples = new List<Sample>();

    struct Sample
    {
        public float time;
        public float value;
    }

    public override bool Evaluate()
    {
        Transform root = skeletonRoot != null ? skeletonRoot : SkeletonRootProvider.CurrentRoot;
        Transform t = JointLookup.GetJoint(root, point);
        if (t == null)
            return false;

        float current = GetAxisValue(t.position, axis);
        float now = Time.time;
        m_Samples.Add(new Sample { time = now, value = current });

        float cutoff = now - Mathf.Max(windowSeconds, 0.0001f);
        for (int i = m_Samples.Count - 1; i >= 0; i--)
        {
            if (m_Samples[i].time < cutoff)
                m_Samples.RemoveAt(i);
        }

        if (m_Samples.Count == 0)
            return false;

        float refValue = m_Samples[0].value;
        for (int i = 1; i < m_Samples.Count; i++)
        {
            float value = m_Samples[i].value;
            if (reference == TimeReference.Max && value > refValue)
                refValue = value;
            else if (reference == TimeReference.Min && value < refValue)
                refValue = value;
        }

        switch (comparison)
        {
            case TimeComparison.BelowBy:
                return current <= refValue - threshold;
            case TimeComparison.AboveBy:
                return current >= refValue + threshold;
            default:
                return false;
        }
    }

    static float GetAxisValue(Vector3 v, AxisSelector axis)
    {
        switch (axis)
        {
            case AxisSelector.X:
                return v.x;
            case AxisSelector.Y:
                return v.y;
            default:
                return v.z;
        }
    }
}
