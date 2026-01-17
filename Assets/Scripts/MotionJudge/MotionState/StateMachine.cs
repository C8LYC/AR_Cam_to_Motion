using System.Collections.Generic;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    public Action[] actions;
    readonly Dictionary<Action, ActionRuntime> m_ActionRuntime = new Dictionary<Action, ActionRuntime>();

    class ActionRuntime
    {
        public int currentStateIndex;
        public float currentStateElapsed;
        public bool currentStateWasValid;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Action action in actions)
        {
            if(!action.IsActive)
                continue;

            if (action.motionStates == null || action.motionStates.Count == 0)
                continue;

            ActionRuntime runtime = GetRuntime(action);
            if (runtime.currentStateIndex < 0 || runtime.currentStateIndex >= action.motionStates.Count)
                runtime.currentStateIndex = 0;
            MotionState currentState = action.motionStates[runtime.currentStateIndex];
            if (currentState == null)
                continue;

            bool isValid = currentState.JudgeConditions();
            if (isValid == runtime.currentStateWasValid)
                runtime.currentStateElapsed += Time.deltaTime;
            else
                runtime.currentStateElapsed = 0f;

            runtime.currentStateWasValid = isValid;

            if (!isValid && currentState.exit_time > 0f && runtime.currentStateElapsed >= currentState.exit_time)
            {
                runtime.currentStateIndex = 0;
                runtime.currentStateElapsed = 0f;
                runtime.currentStateWasValid = false;
                continue;
            }

            if (isValid && currentState.hold_time > 0f && runtime.currentStateElapsed >= currentState.hold_time)
            {
                runtime.currentStateIndex = 0;
                runtime.currentStateElapsed = 0f;
                runtime.currentStateWasValid = false;
                continue;
            }

            int[] nextStateIndexes = action.GetNextStateIndexes(runtime.currentStateIndex);
            for (int i = 0; i < nextStateIndexes.Length; i++)
            {
                int nextIndex = nextStateIndexes[i];
                if (nextIndex < 0 || nextIndex >= action.motionStates.Count)
                    continue;

                MotionState nextState = action.motionStates[nextIndex];
                if (nextState != null && nextState.JudgeConditions())
                {
                    runtime.currentStateIndex = nextIndex;
                    runtime.currentStateElapsed = 0f;
                    runtime.currentStateWasValid = false;
                    break;
                }
            }
        }
    }

    ActionRuntime GetRuntime(Action action)
    {
        if (!m_ActionRuntime.TryGetValue(action, out var runtime) || runtime == null)
        {
            runtime = new ActionRuntime();
            m_ActionRuntime[action] = runtime;
        }
        return runtime;
    }
}
