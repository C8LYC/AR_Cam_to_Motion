using System;
using UnityEngine;

[Serializable]
public class VelocityAxisCondition : MotionCondition
{
    public Transform skeletonRoot;
    public JointIndices point;
    public AxisSelector axis;
    public AxisComparison comparison;
    public float threshold;
    public float requiredTimeSeconds;

    float m_LastValue;
    bool m_HasLastValue;
    float m_AccumulatedTime;

    public override bool Evaluate()
    {
        Transform root = skeletonRoot != null ? skeletonRoot : SkeletonRootProvider.CurrentRoot;
        Transform t = JointLookup.GetJoint(root, point);
        if (t == null)
            return false;

        float current = GetAxisValue(t.position, axis);
        if (!m_HasLastValue)
        {
            m_LastValue = current;
            m_HasLastValue = true;
            m_AccumulatedTime = 0f;
            return false;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float velocity = (current - m_LastValue) / dt;
        m_LastValue = current;

        bool isMatch = Compare(velocity, threshold, comparison);
        if (!isMatch)
        {
            m_AccumulatedTime = 0f;
            return false;
        }

        if (requiredTimeSeconds <= 0f)
            return true;

        m_AccumulatedTime += Time.deltaTime;
        return m_AccumulatedTime >= requiredTimeSeconds;
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

    static bool Compare(float value, float threshold, AxisComparison comparison)
    {
        switch (comparison)
        {
            case AxisComparison.Less:
                return value < threshold;
            case AxisComparison.Greater:
                return value > threshold;
            case AxisComparison.LessOrEqual:
                return value <= threshold;
            case AxisComparison.GreaterOrEqual:
                return value >= threshold;
            case AxisComparison.Equal:
                return Mathf.Approximately(value, threshold);
            case AxisComparison.NotEqual:
                return !Mathf.Approximately(value, threshold);
            default:
                return false;
        }
    }
}
