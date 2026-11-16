using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TitleButtonAudio : MonoBehaviour
{
    public TitleMenuAudio menuAudio;
    public bool startButton;
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    void PlayClick()
    {
        if (menuAudio != null) menuAudio.PlayClick(startButton);
    }

    void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
    }
}
