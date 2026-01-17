using System;
using UnityEngine;

[Serializable]
public class PositionAxisCondition : MotionCondition
{
    public Transform skeletonRoot;
    public JointIndices point1;
    public JointIndices point2;
    public AxisSelector axis;
    public AxisComparison comparison;
    public float threshold;

    public override bool Evaluate()
    {
        Transform root = skeletonRoot != null ? skeletonRoot : SkeletonRootProvider.CurrentRoot;
        Transform t1 = JointLookup.GetJoint(root, point1);
        Transform t2 = JointLookup.GetJoint(root, point2);
        if (t1 == null || t2 == null)
            return false;

        float v1 = GetAxisValue(t1.position, axis);
        float v2 = GetAxisValue(t2.position, axis);
        float diff = v1 - v2;
        return Compare(diff, threshold, comparison);
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
