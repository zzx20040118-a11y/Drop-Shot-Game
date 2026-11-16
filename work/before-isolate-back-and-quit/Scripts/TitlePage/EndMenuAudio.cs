using UnityEngine;
using UnityEngine.UI;
using DropShot.UI;

// Shares the title menu sounds, including a success sound that survives scene changes.
public class EndMenuAudio : MonoBehaviour
{
    public TitleMenuAudio menuAudio;

    void Start()
    {
        if (menuAudio == null) return;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                // JellyButton owns its own click event when present.
                if (button.GetComponent<JellyButton>() != null) continue;
                if (button.GetComponent<TitleButtonAudio>() != null) continue;
                TitleButtonAudio audio = button.gameObject.AddComponent<TitleButtonAudio>();
                audio.menuAudio = menuAudio;
                audio.startButton = button.name == "BackButton";
            }
            foreach (JellyButton button in root.GetComponentsInChildren<JellyButton>(true))
            {
                button.clickClip = null;
                button.hoverClip = null;
                button.onHover.AddListener(menuAudio.PlayHover);
                bool back = button.name == "BackButton";
                button.onClick.AddListener(() => menuAudio.PlayClick(back));
            }
        }
    }
}
