using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class TitleButtonAudio : MonoBehaviour, IPointerEnterHandler
{
    public TitleMenuAudio menuAudio;
    public bool startButton;
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.IsActive() && button.IsInteractable() && menuAudio != null)
            menuAudio.PlayHover();
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
