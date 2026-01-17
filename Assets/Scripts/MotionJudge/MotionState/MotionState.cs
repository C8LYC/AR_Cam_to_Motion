using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MotionJudge/MotionState")]
public class MotionState : ScriptableObject
{
    public string stateName;
    [SerializeReference]
    public List<MotionCondition> conditions = new List<MotionCondition>();

    public float exit_time;
    public float hold_time;
    public int[] nextStateIndexs;

    public bool JudgeConditions()
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            MotionCondition condition = conditions[i];
            if (condition == null || !condition.Evaluate())
                return false;
        }

        return true;
    }
}
