using UnityEngine;
using DropShot.Judgement;
using TMPro;
using UnityEngine.UI;

public class StrikeRingUI : MonoBehaviour
{
    [Header("外圈")]
    public GameObject strikeRing;
    public RectTransform ringTransform;

    [Header("内圈 PERFECT 目标")]
    public GameObject targetRing;
    public RectTransform targetRingTransform;

    [Header("提示图片与文字")]
    public TMP_Text holdSpaceText;
    public GameObject holdSpaceImage;
    public Sprite holdSpaceSprite;
    public Vector2 holdSpaceImageSize = new Vector2(180f, 64f);
    public Vector2 holdSpaceImageOffset = new Vector2(0f, -50f);

    [Header("判定系统")]
    public JudgementSystem judgement;
    [HideInInspector] public float visualShrinkDuration = 1f;

    Vector3 initialScale;
    Vector3 targetScale;
    int currentNoteIndex = -1;
    bool isCharging;
    double chargeStartSongTime;
    double targetSongTime;

    void Awake()
    {
        if (ringTransform != null) initialScale = ringTransform.localScale;
        if (targetRingTransform != null) targetScale = targetRingTransform.localScale;
        PrepareHoldImage();
        HideRing(-1);
    }

    void OnEnable()
    {
        if (judgement == null) return;
        judgement.StrikeAutoStarted += ShowRing;
        judgement.StrikeChargeStarted += StartCharging;
        judgement.StrikeChargeEnded += HideRing;
    }

    void OnDisable()
    {
        if (judgement != null)
        {
            judgement.StrikeAutoStarted -= ShowRing;
            judgement.StrikeChargeStarted -= StartCharging;
            judgement.StrikeChargeEnded -= HideRing;
        }
        HideRing(-1);
    }

    void Update()
    {
        if (!isCharging || judgement == null || ringTransform == null) return;
        double duration = targetSongTime - chargeStartSongTime;
        float progress = duration > 0
            ? Mathf.Clamp01((float)((judgement.SongTime - chargeStartSongTime) / duration)) : 1f;
        ringTransform.localScale = Vector3.Lerp(initialScale, targetScale, progress);
    }

    void ShowRing(int noteIndex)
    {
        currentNoteIndex = noteIndex;
        isCharging = false;
        if (ringTransform != null) ringTransform.localScale = initialScale;
        if (strikeRing != null) strikeRing.SetActive(true);
        if (targetRing != null) targetRing.SetActive(true);
        SetHoldHint(true);
    }

    void StartCharging(int noteIndex)
    {
        if (judgement == null || judgement.Chart == null || ringTransform == null) return;
        if (noteIndex < 0 || noteIndex >= judgement.Chart.notes.Count) return;
        if (currentNoteIndex != noteIndex) ShowRing(noteIndex);
        chargeStartSongTime = judgement.SongTime;
        targetSongTime = judgement.Chart.notes[noteIndex].targetTime;
        isCharging = true;
        SetHoldHint(true);
    }

    void PrepareHoldImage()
    {
        if (holdSpaceSprite == null || holdSpaceText == null) return;
        RectTransform source = holdSpaceText.rectTransform;
        if (holdSpaceImage == null)
        {
            holdSpaceImage = new GameObject("HoldSpaceImage", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            holdSpaceImage.transform.SetParent(source.parent, false);
        }
        Image image = holdSpaceImage.GetComponent<Image>();
        if (image == null) return;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = source.anchorMin;
        rect.anchorMax = source.anchorMax;
        rect.pivot = source.pivot;
        rect.sizeDelta = new Vector2(Mathf.Max(1f, holdSpaceImageSize.x),
            Mathf.Max(1f, holdSpaceImageSize.y));
        rect.anchoredPosition = source.anchoredPosition + holdSpaceImageOffset;
        rect.localRotation = source.localRotation;
        rect.localScale = Vector3.one;
        image.sprite = holdSpaceSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        rect.SetAsLastSibling();
    }

    void SetHoldHint(bool visible)
    {
        if (holdSpaceImage != null) holdSpaceImage.SetActive(visible);
        if (holdSpaceText == null) return;
        holdSpaceText.text = isCharging ? "RELEASE" : "HOLD";
        if (holdSpaceImage == null) holdSpaceText.text += " SPACE";
        holdSpaceText.gameObject.SetActive(visible);
    }

    void HideRing(int noteIndex)
    {
        if (noteIndex >= 0 && noteIndex != currentNoteIndex) return;
        if (strikeRing != null) strikeRing.SetActive(false);
        if (targetRing != null) targetRing.SetActive(false);
        isCharging = false;
        currentNoteIndex = -1;
        if (ringTransform != null) ringTransform.localScale = initialScale;
        SetHoldHint(false);
    }
}
