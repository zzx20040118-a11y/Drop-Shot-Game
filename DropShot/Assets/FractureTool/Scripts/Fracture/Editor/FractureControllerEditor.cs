using UnityEngine;
using UnityEditor;

namespace FractureTool.Editors
{
    [CustomEditor(typeof(FractureController))]
    public class FractureControllerEditor : Editor
    {
        private bool showModeOptions = true;
        private bool showMaterialOptions = true;
        private bool showPhysicsOptions = true;
        private bool showActionButtons = true;

        private bool showCustomPhysics = true;

        public override void OnInspectorGUI()
        {
            FractureController controller = (FractureController)target;

            // Record every change made below so it can be undone and is saved with the scene
            Undo.RecordObject(controller, "Modify Fracture Controller");
            EditorGUI.BeginChangeCheck();

            // Header
            EditorGUILayout.Space();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 16;
            headerStyle.normal.textColor = Color.white;

            GUI.backgroundColor = new Color(0.2f, 0.4f, 0.8f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("FRACTURE TOOL", headerStyle);
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            DrawSetupWarnings(controller);

            // Main Configuration
            showModeOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showModeOptions, "Main Configuration");
            if (showModeOptions)
            {
                EditorGUILayout.BeginVertical("helpbox");

                EditorGUI.BeginChangeCheck();
                FractureController.FractureMode newMode = (FractureController.FractureMode)EditorGUILayout.EnumPopup("Fracture Mode:", controller.mode);

                if (EditorGUI.EndChangeCheck())
                {
                    controller.mode = newMode;
                    // Swap the mode component after the inspector has finished drawing
                    EditorApplication.delayCall += () => { if (controller != null) controller.SetFractureMode(controller.mode); };
                }

                controller.previewMode = EditorGUILayout.Toggle("Preview Mode:", controller.previewMode);

                // Mode info
                DrawModeInfo(controller.mode);

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Materials
            showMaterialOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showMaterialOptions, "Materials");
            if (showMaterialOptions)
            {
                EditorGUILayout.BeginVertical("helpbox");

                controller.interiorMaterial = (Material)EditorGUILayout.ObjectField("Interior Material:", controller.interiorMaterial, typeof(Material), false);
                controller.fragmentMaterial = (Material)EditorGUILayout.ObjectField("Fragment Material:", controller.fragmentMaterial, typeof(Material), false);

                if (controller.fragmentMaterial == null)
                {
                    EditorGUILayout.HelpBox("If no fragment material is assigned, the original object's material will be used.", MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // General Physics
            showPhysicsOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showPhysicsOptions, "General Physics");
            if (showPhysicsOptions)
            {
                EditorGUILayout.BeginVertical("helpbox");

                controller.applyPhysics = EditorGUILayout.Toggle("Apply Physics:", controller.applyPhysics);
                controller.destroyOriginal = EditorGUILayout.Toggle("Destroy Original:", controller.destroyOriginal);
                controller.fragmentSeparation = EditorGUILayout.Slider("Fragment Separation:", controller.fragmentSeparation, 0f, 0.1f);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Physics Control", EditorStyles.boldLabel);
                controller.useExplosionForce = EditorGUILayout.Toggle("Use Explosion Force:", controller.useExplosionForce);

                if (controller.useExplosionForce)
                {
                    controller.globalExplosionForce = EditorGUILayout.Slider("Global Explosion Force:", controller.globalExplosionForce, 0f, 10f);
                }
                else
                {
                    EditorGUILayout.HelpBox("Fragments will only be affected by gravity (collapse effect)", MessageType.Info);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Fragment Lifetime", EditorStyles.boldLabel);
                controller.autoDestroy = EditorGUILayout.Toggle("Auto Destroy Fragments:", controller.autoDestroy);

                if (controller.autoDestroy)
                {
                    controller.destroyAfterSeconds = EditorGUILayout.Slider("Destroy After (seconds):", controller.destroyAfterSeconds, 1f, 30f);
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Mode specific options
            EditorGUILayout.Space();
            DrawModeSpecificOptions(controller);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Radial, Random Planes and Grid modes, material presets, UV correction and trigger/collision fractures are available in Fracture Tool Pro.", MessageType.None);

            // Custom Physics Section (only show when Custom is selected)
            if (controller.materialType == FractureController.MaterialType.Custom)
            {
                EditorGUILayout.Space();
                showCustomPhysics = EditorGUILayout.BeginFoldoutHeaderGroup(showCustomPhysics, "Custom Physics Settings");
                if (showCustomPhysics)
                {
                    EditorGUILayout.BeginVertical("helpbox");

                    // Quick preset buttons
                    EditorGUILayout.LabelField("Quick Presets:", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Lightweight", GUILayout.Width(80)))
                        controller.SetCustomPhysicsPreset("lightweight");
                    if (GUILayout.Button("Heavy", GUILayout.Width(60)))
                        controller.SetCustomPhysicsPreset("heavy");
                    if (GUILayout.Button("Bouncy", GUILayout.Width(60)))
                        controller.SetCustomPhysicsPreset("bouncy");
                    if (GUILayout.Button("Reset", GUILayout.Width(60)))
                        controller.SetCustomPhysicsPreset("default");

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    // Custom physics properties
                    DrawCustomPhysicsProperties(controller);

                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            // Action Buttons
            showActionButtons = EditorGUILayout.BeginFoldoutHeaderGroup(showActionButtons, "Actions");
            if (showActionButtons)
            {
                EditorGUILayout.BeginVertical("helpbox");

                // Preview Button
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("PREVIEW FRACTURE", GUILayout.Height(35)))
                {
                    controller.Preview();
                    SceneView.RepaintAll();
                }

                EditorGUILayout.Space(5);

                // Fracture Button (fragments are physics objects, so this needs Play Mode)
                GUI.backgroundColor = Color.red;
                using (new EditorGUI.DisabledScope(!Application.isPlaying || controller.HasFractured))
                {
                    if (GUILayout.Button("FRACTURE OBJECT", GUILayout.Height(40)))
                        controller.Fracture();
                }
                GUI.backgroundColor = Color.white;

                if (!Application.isPlaying)
                    EditorGUILayout.HelpBox("Enter Play Mode to fracture. Preview works in both modes.", MessageType.None);

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Current Status
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Mode: {controller.mode}");
            EditorGUILayout.LabelField($"Preview: {(controller.previewMode ? "Enabled" : "Disabled")}");
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
                PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
        }

        void DrawSetupWarnings(FractureController controller)
        {
            MeshFilter mf = controller.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || controller.GetComponent<MeshRenderer>() == null)
            {
                EditorGUILayout.HelpBox("This object needs a MeshFilter with a mesh and a MeshRenderer to be fractured.", MessageType.Error);
            }
            else if (!mf.sharedMesh.isReadable)
            {
                EditorGUILayout.HelpBox("Mesh is not readable. Enable Read/Write in the model import settings, otherwise fracturing fails in builds.", MessageType.Warning);
                string path = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (AssetImporter.GetAtPath(path) is ModelImporter importer && GUILayout.Button("Enable Read/Write"))
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
            }

            if (controller.GetComponent<Collider>() == null)
                EditorGUILayout.HelpBox("A Collider is required (used for the fracture bounds, clicks and triggers).", MessageType.Warning);
        }

        void DrawModeInfo(FractureController.FractureMode mode)
        {
            string info = mode switch
            {
                FractureController.FractureMode.Voronoi => "Organic fracture based on Voronoi cells",
                FractureController.FractureMode.Simple => "Simple horizontal cut",
                _ => "Unknown fracture mode"
            };

            EditorGUILayout.HelpBox(info, MessageType.Info);
        }

        void DrawModeSpecificOptions(FractureController controller)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Mode Specific Options - {controller.mode}", EditorStyles.boldLabel);

            switch (controller.mode)
            {
                case FractureController.FractureMode.Voronoi:
                    DrawVoronoiOptions(controller);
                    break;
                case FractureController.FractureMode.Simple:
                    EditorGUILayout.HelpBox("Simple fracture: One horizontal cut.\nNo additional configuration required.", MessageType.Info);
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        static void RecordComponent(Component component)
        {
            if (GUI.changed)
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        void DrawVoronoiOptions(FractureController controller)
        {
            VoronoiGenerator vg = controller.GetVoronoiComponent();
            if (vg == null) return;
            Undo.RecordObject(vg, "Modify Voronoi Settings");

            vg.pointCount = EditorGUILayout.IntSlider("Number of Points:", vg.pointCount, 3, 200);

            EditorGUILayout.HelpBox($"Estimated fragments: ~{vg.pointCount}", vg.pointCount > 60 ? MessageType.Warning : MessageType.Info);
            RecordComponent(vg);
        }

        void OnSceneGUI()
        {
            FractureController controller = (FractureController)target;

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 250, 140));
            GUILayout.BeginVertical("box");
            GUILayout.Label($"Fracture Mode: {controller.mode}");
            GUILayout.Label($"Material: {controller.materialType}");
            GUILayout.Label($"Preview: {(controller.previewMode ? "ON" : "OFF")}");

            if (controller.materialType == FractureController.MaterialType.Custom)
            {
                GUILayout.Label($"Custom Mass: {controller.customPhysics.mass:F2}");
                GUILayout.Label($"Custom Force: {controller.customPhysics.explosionForceMultiplier:F1}x");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        void DrawCustomPhysicsProperties(FractureController controller)
        {
            var physics = controller.customPhysics;

            // Basic Physics
            EditorGUILayout.LabelField("Basic Physics Properties", EditorStyles.boldLabel);
            physics.mass = EditorGUILayout.Slider("Mass:", physics.mass, 0.1f, 3f);
            physics.drag = EditorGUILayout.Slider("Drag:", physics.drag, 0f, 2f);
            physics.angularDrag = EditorGUILayout.Slider("Angular Drag:", physics.angularDrag, 0f, 2f);
            physics.gravityScale = EditorGUILayout.Slider("Gravity Scale:", physics.gravityScale, 0.1f, 2f);

            EditorGUILayout.Space();

            // Material Properties
            EditorGUILayout.LabelField("Material Properties", EditorStyles.boldLabel);
            physics.bounciness = EditorGUILayout.Slider("Bounciness:", physics.bounciness, 0f, 1f);
            physics.friction = EditorGUILayout.Slider("Friction:", physics.friction, 0f, 2f);

            EditorGUILayout.Space();

            // Fracture Behavior
            EditorGUILayout.LabelField("Fracture Behavior", EditorStyles.boldLabel);
            physics.explosionForceMultiplier = EditorGUILayout.Slider("Explosion Force Multiplier:", physics.explosionForceMultiplier, 0.1f, 5f);
            physics.fragmentMultiplier = EditorGUILayout.Slider("Fragment Multiplier:", physics.fragmentMultiplier, 0.1f, 3f);

            // Show effective values
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Calculated Values", EditorStyles.miniBoldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Effective Explosion Force:", controller.globalExplosionForce * physics.explosionForceMultiplier);
            EditorGUI.EndDisabledGroup();
        }
    }
}
