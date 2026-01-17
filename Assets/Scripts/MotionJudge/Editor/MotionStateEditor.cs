using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MotionState))]
public class MotionStateEditor : Editor
{
    SerializedProperty m_Conditions;

    void OnEnable()
    {
        m_Conditions = serializedObject.FindProperty("conditions");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "conditions");
        EditorGUILayout.PropertyField(m_Conditions, true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add Condition", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(m_Conditions == null))
        {
            if (GUILayout.Button("Add Angle Condition"))
                AddCondition(new AngleCondition());
            if (GUILayout.Button("Add Position Axis Condition"))
                AddCondition(new PositionAxisCondition());
            if (GUILayout.Button("Add Velocity Axis Condition"))
                AddCondition(new VelocityAxisCondition());
            if (GUILayout.Button("Add Time Different Condition"))
                AddCondition(new TimeDifferentCondition());
            if (GUILayout.Button("Add Distance Condition"))
                AddCondition(new PositionCondition());
            if (GUILayout.Button("Add Speed Condition"))
                AddCondition(new VelocityCondition());
        }

        serializedObject.ApplyModifiedProperties();
    }

    void AddCondition(object condition)
    {
        int index = m_Conditions.arraySize;
        m_Conditions.InsertArrayElementAtIndex(index);
        SerializedProperty element = m_Conditions.GetArrayElementAtIndex(index);
        element.managedReferenceValue = condition;
    }
}
