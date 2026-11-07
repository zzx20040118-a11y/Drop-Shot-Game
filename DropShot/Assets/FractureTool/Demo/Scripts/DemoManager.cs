using UnityEngine;
using UnityEngine.SceneManagement;

namespace FractureTool.Demo
{
    /// <summary>
    /// Small helper for the demo scene: on-screen help, crosshair, scene reset and a ball dropper
    /// to show collision triggered fractures.
    /// </summary>
    public class DemoManager : MonoBehaviour
    {
        [Header("Keys")]
        public KeyCode resetKey = KeyCode.R;
        public KeyCode toggleHelpKey = KeyCode.H;

        bool showHelp = true;
        GUIStyle boxStyle;
        GUIStyle crosshairStyle;

        void Update()
        {
            if (Input.GetKeyDown(resetKey))
                ReloadScene();

            if (Input.GetKeyDown(toggleHelpKey))
                showHelp = !showHelp;
        }

        void ReloadScene()
        {
            Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // Works even if the demo scene is not in the Build Settings
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }

        void OnGUI()
        {
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 14,
                    richText = true,
                    padding = new RectOffset(12, 12, 10, 10)
                };
                crosshairStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 22
                };
                crosshairStyle.normal.textColor = Color.white;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
                GUI.Label(new Rect(Screen.width * 0.5f - 20, Screen.height * 0.5f - 20, 40, 40), "+", crosshairStyle);

            if (!showHelp)
                return;

            string help =
                "<b>FRACTURE TOOL - DEMO</b>\n" +
                "WASD / Mouse   Move and look\n" +
                "Left Click        Break the object in front of you\n" +
                "Shooting range: north door, press E\n" +
                $"{resetKey}                   Reset the scene\n" +
                "Esc               Release the cursor\n" +
                $"{toggleHelpKey}                   Hide this help";

            GUI.Box(new Rect(12, 12, 360, 200), help, boxStyle);
        }
    }
}
