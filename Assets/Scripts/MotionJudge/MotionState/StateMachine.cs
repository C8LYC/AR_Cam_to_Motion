using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StateMachine : MonoBehaviour
{
    public Action[] actions;
    readonly Dictionary<Action, ActionRuntime> m_ActionRuntime = new Dictionary<Action, ActionRuntime>();

    public GameObject MotionStateDisplayPrefab;

    public GameObject MotionLookupUI;

    public GameObject ActionTitlePrefab;
    public GameObject ActionStateGroupPrefab;

    public Color activeStateColor = new Color(0.2f, 0.9f, 0.2f, 0.3f);
    public Color inactiveStateColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);

    class ActionRuntime
    {
        public int currentStateIndex;
        public float currentStateElapsed;
        public bool currentStateWasValid;
    }

    class StateDisplayEntry
    {
        public MotionState state;
        public Image image;
        public TMP_Text label;
    }

    readonly Dictionary<Action, List<StateDisplayEntry>> m_ActionStateDisplays =
        new Dictionary<Action, List<StateDisplayEntry>>();
    bool m_HasBuiltStateDisplays;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BuildStateDisplays();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Action action in actions)
        {
            if(!action.IsActive)
            {
                SetActionDisplayState(action, -1);
                continue;
            }

            if (action.motionStates == null || action.motionStates.Count == 0)
            {
                SetActionDisplayState(action, -1);
                continue;
            }

            ActionRuntime runtime = GetRuntime(action);
            if (runtime.currentStateIndex < 0 || runtime.currentStateIndex >= action.motionStates.Count)
                runtime.currentStateIndex = 0;
            MotionState currentState = action.motionStates[runtime.currentStateIndex];
            if (currentState == null)
            {
                SetActionDisplayState(action, -1);
                continue;
            }

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
                SetActionDisplayState(action, runtime.currentStateIndex);
                continue;
            }

            if (isValid && currentState.hold_time > 0f && runtime.currentStateElapsed >= currentState.hold_time)
            {
                runtime.currentStateIndex = 0;
                runtime.currentStateElapsed = 0f;
                runtime.currentStateWasValid = false;
                SetActionDisplayState(action, runtime.currentStateIndex);
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

            SetActionDisplayState(action, runtime.currentStateIndex);
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

    void BuildStateDisplays()
    {
        if (m_HasBuiltStateDisplays)
            return;

        if (MotionStateDisplayPrefab == null || MotionLookupUI == null || actions == null)
            return;

        Transform parent = MotionLookupUI.transform;
        for (int i = 0; i < actions.Length; i++)
        {
            Action action = actions[i];
            if (action == null || action.motionStates == null)
                continue;

            if (ActionTitlePrefab != null)
                InstantiateActionTitle(parent, action.actionName);

            Transform rowParent = parent;
            if (ActionStateGroupPrefab != null)
            {
                GameObject rowInstance = Instantiate(ActionStateGroupPrefab, parent, false);
                rowParent = rowInstance.transform;
            }

            List<StateDisplayEntry> entries = new List<StateDisplayEntry>();
            for (int j = 0; j < action.motionStates.Count; j++)
            {
                MotionState state = action.motionStates[j];
                if (state == null)
                    continue;

                GameObject instance = Instantiate(MotionStateDisplayPrefab, rowParent, false);
                Image image = instance.GetComponent<Image>();
                if (image == null)
                    image = instance.GetComponentInChildren<Image>(true);

                TMP_Text label = instance.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = string.IsNullOrEmpty(state.stateName) ? state.name : state.stateName;

                if (image != null)
                    image.color = inactiveStateColor;

                entries.Add(new StateDisplayEntry
                {
                    state = state,
                    image = image,
                    label = label
                });

            }

            if (entries.Count > 0)
                m_ActionStateDisplays[action] = entries;
        }

        m_HasBuiltStateDisplays = true;
    }

    void SetActionDisplayState(Action action, int activeStateIndex)
    {
        if (!m_ActionStateDisplays.TryGetValue(action, out var entries) || entries == null)
            return;

        MotionState activeState = null;
        if (action != null && action.motionStates != null &&
            activeStateIndex >= 0 && activeStateIndex < action.motionStates.Count)
        {
            activeState = action.motionStates[activeStateIndex];
        }

        for (int i = 0; i < entries.Count; i++)
        {
            StateDisplayEntry entry = entries[i];
            if (entry == null || entry.image == null)
                continue;

            entry.image.color = entry.state == activeState ? activeStateColor : inactiveStateColor;
        }
    }

    void InstantiateActionTitle(Transform parent, string actionName)
    {
        GameObject instance = Instantiate(ActionTitlePrefab, parent, false);
        TMP_Text label = instance.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = actionName;
    }
}
