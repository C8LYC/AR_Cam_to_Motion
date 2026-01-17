using System;
using UnityEngine;

[Serializable]
public class VelocityCondition : MotionCondition
{
    public Transform skeletonRoot;
    public JointIndices targetJoint;
    public float minSpeed;
    public float maxSpeed = 10f;

    Vector3 m_LastPosition;
    bool m_HasLastPosition;

    public override bool Evaluate()
    {
        Transform root = skeletonRoot != null ? skeletonRoot : SkeletonRootProvider.CurrentRoot;
        Transform target = JointLookup.GetJoint(root, targetJoint);
        if (target == null)
            return false;

        Vector3 current = target.position;
        if (!m_HasLastPosition)
        {
            m_LastPosition = current;
            m_HasLastPosition = true;
            return false;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = Vector3.Distance(current, m_LastPosition) / dt;
        m_LastPosition = current;
        return speed >= minSpeed && speed <= maxSpeed;
    }
}
