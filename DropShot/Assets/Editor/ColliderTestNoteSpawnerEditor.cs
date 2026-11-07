using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ColliderTestNoteSpawner))]
[CanEditMultipleObjects]
public class ColliderTestNoteSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.name == "normalRotationSpeed")
            {
                EditorGUILayout.LabelField("Rotation Speed (degrees / second)", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(property.FindPropertyRelative("x"), new GUIContent("Minimum Speed"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("y"), new GUIContent("Maximum Speed"));
                EditorGUI.indentLevel--;
                EditorGUILayout.HelpBox("Each candy picks a random speed in this range. Changes apply immediately during Play Mode.", MessageType.Info);
            }
            else
            {
                using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                    EditorGUILayout.PropertyField(property, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
