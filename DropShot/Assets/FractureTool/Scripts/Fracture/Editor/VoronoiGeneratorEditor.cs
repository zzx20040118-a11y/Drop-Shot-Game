using UnityEngine;
using UnityEditor;

namespace FractureTool.Editors
{
    [CustomEditor(typeof(VoronoiGenerator))]
    public class VoronoiGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            VoronoiGenerator generator = (VoronoiGenerator)target;

            // When a FractureController is present it owns these settings
            if (generator.GetComponent<FractureController>() != null)
            {
                EditorGUILayout.HelpBox("Settings are managed by the Fracture Controller on this object.", MessageType.Info);
                return;
            }

            DrawDefaultInspector();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Voronoi Fracture Tool", EditorStyles.boldLabel);

            // Preview Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Preview Voronoi", GUILayout.Height(30)))
            {
                generator.PreviewVoronoi(generator.gameObject);
                SceneView.RepaintAll();
            }

            // Fracture Button (needs Play Mode)
            GUI.backgroundColor = Color.red;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Fracture Definitively", GUILayout.Height(35)))
                {
                    generator.previewMode = false;
                    generator.Fracture(generator.gameObject);
                }
            }
            GUI.backgroundColor = Color.white;

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to fracture. Preview works in both modes.", MessageType.None);
        }
    }
}
