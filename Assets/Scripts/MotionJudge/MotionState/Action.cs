using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MotionJudge/Action")]
public class Action : ScriptableObject
{
    public string actionName;
    public bool IsActive = true;
    public List<MotionState> motionStates = new List<MotionState>();

    public int[] GetNextStateIndexes(int currentStateIndex)
    {
        if (motionStates != null && motionStates.Count > 0)
        {
            if (currentStateIndex < 0 || currentStateIndex >= motionStates.Count)
                return new int[0];

            MotionState current = motionStates[currentStateIndex];
            if (current == null)
                return new int[0];

            return current.nextStateIndexs;
        }
        return new int[0];
    }
}
