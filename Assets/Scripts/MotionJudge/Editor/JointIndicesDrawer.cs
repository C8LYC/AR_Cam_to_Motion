using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(JointIndices))]
public class JointIndicesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Enum)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);
        Rect fieldRect = EditorGUI.PrefixLabel(position, label);
        string current = property.enumDisplayNames[property.enumValueIndex];
        if (GUI.Button(fieldRect, current, EditorStyles.popup))
        {
            var popup = new SearchableEnumPopup(property);
            PopupWindow.Show(fieldRect, popup);
        }
        EditorGUI.EndProperty();
    }

    class SearchableEnumPopup : PopupWindowContent
    {
        const float k_RowHeight = 18f;
        const float k_SearchHeight = 20f;
        const float k_Padding = 6f;

        readonly SerializedObject m_SerializedObject;
        readonly string m_PropertyPath;
        readonly string[] m_DisplayNames;
        readonly List<int> m_Filtered = new List<int>();

        string m_Search = string.Empty;
        Vector2 m_Scroll;

        public SearchableEnumPopup(SerializedProperty property)
        {
            m_SerializedObject = property.serializedObject;
            m_PropertyPath = property.propertyPath;
            m_DisplayNames = property.enumDisplayNames;
            RebuildFilter();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(280f, 320f);
        }

        public override void OnOpen()
        {
            EditorApplication.delayCall += () =>
            {
                if (editorWindow != null)
                {
                    GUI.FocusControl("SearchField");
                    editorWindow.Repaint();
                }
            };
        }

        public override void OnGUI(Rect rect)
        {
            Rect searchRect = new Rect(k_Padding, k_Padding, rect.width - 2f * k_Padding, k_SearchHeight);
            GUI.SetNextControlName("SearchField");
            string newSearch = EditorGUI.TextField(searchRect, m_Search);
            if (!string.Equals(newSearch, m_Search, StringComparison.Ordinal))
            {
                m_Search = newSearch;
                RebuildFilter();
            }

            Rect listRect = new Rect(
                k_Padding,
                k_Padding + k_SearchHeight + k_Padding,
                rect.width - 2f * k_Padding,
                rect.height - k_SearchHeight - 3f * k_Padding);

            float contentHeight = m_Filtered.Count * k_RowHeight;
            m_Scroll = GUI.BeginScrollView(
                listRect,
                m_Scroll,
                new Rect(0f, 0f, listRect.width - 16f, contentHeight));

            for (int i = 0; i < m_Filtered.Count; i++)
            {
                int index = m_Filtered[i];
                Rect row = new Rect(0f, i * k_RowHeight, listRect.width - 16f, k_RowHeight);
                if (GUI.Button(row, m_DisplayNames[index], EditorStyles.label))
                {
                    SetEnumValue(index);
                    editorWindow.Close();
                }
            }

            GUI.EndScrollView();
        }

        void RebuildFilter()
        {
            m_Filtered.Clear();
            for (int i = 0; i < m_DisplayNames.Length; i++)
            {
                string name = m_DisplayNames[i];
                if (string.IsNullOrEmpty(m_Search) ||
                    name.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    m_Filtered.Add(i);
                }
            }
        }

        void SetEnumValue(int index)
        {
            m_SerializedObject.Update();
            SerializedProperty prop = m_SerializedObject.FindProperty(m_PropertyPath);
            if (prop != null)
            {
                prop.enumValueIndex = index;
                m_SerializedObject.ApplyModifiedProperties();
            }
        }
    }
}
