using System;
using UnityEngine;

[Serializable]
public class PositionCondition : MotionCondition
{
    public Transform skeletonRoot;
    public JointIndices targetJoint;
    public JointIndices referenceJoint;
    public float minDistance;
    public float maxDistance = 1f;

    public override bool Evaluate()
    {
        Transform root = skeletonRoot != null ? skeletonRoot : SkeletonRootProvider.CurrentRoot;
        Transform target = JointLookup.GetJoint(root, targetJoint);
        Transform reference = JointLookup.GetJoint(root, referenceJoint);
        if (target == null || reference == null)
            return false;

        float distance = Vector3.Distance(target.position, reference.position);
        return distance >= minDistance && distance <= maxDistance;
    }
}
