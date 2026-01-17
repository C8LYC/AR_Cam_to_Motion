using System;
using UnityEngine;

[Serializable]
public class AngleCondition : MotionCondition
{
    public Transform skeletonRoot;
    public JointIndices startJoint;
    public JointIndices middleJoint;
    public JointIndices endJoint;
    [Range(0f, 180f)]
    public float minAngle;
    [Range(0f, 180f)]
    public float maxAngle = 180f;

    public override bool Evaluate()
    {
        Transform root = skeletonRoot != null ? skeletonRoot : SkeletonRootProvider.CurrentRoot;
        Transform start = JointLookup.GetJoint(root, startJoint);
        Transform middle = JointLookup.GetJoint(root, middleJoint);
        Transform end = JointLookup.GetJoint(root, endJoint);
        if (start == null || middle == null || end == null)
            return false;

        Vector3 v1 = start.position - middle.position;
        Vector3 v2 = end.position - middle.position;
        float angle = Vector3.Angle(v1, v2);
        return angle >= minAngle && angle <= maxAngle;
    }
}
