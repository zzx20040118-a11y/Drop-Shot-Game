using UnityEngine;
using UnityEngine.SceneManagement;
using DropShot.Judgement;

public class FinalPortalController : MonoBehaviour
{
    [Header("判定系统")]
    public JudgementSystem judgement;

    [Header("传送门")]
    public GameObject portal;

    [Header("下一关场景名称")]
    public string nextSceneName = "End";

    private int finalStrikeIndex = -1;
    private bool portalTriggered = false;

    private void Awake()
    {
        if (portal != null)
            portal.SetActive(false);
    }

    private void Start()
    {
        if (judgement == null || judgement.Chart == null)
        {
            Debug.LogError("[Portal] JudgementSystem 或谱面未设置！");
            return;
        }

        // 自动寻找最后一个 Strike
        for (int i = 0; i < judgement.Chart.notes.Count; i++)
        {
            ChartNote note = judgement.Chart.notes[i];

            if (NoteKindParser.Parse(note.type) == NoteKind.Strike)
            {
                finalStrikeIndex = i;
            }
        }

        if (finalStrikeIndex < 0)
        {
            Debug.LogError("[Portal] 谱面中没有 Strike！");
            return;
        }

        Debug.Log($"[Portal] 最后一次 Strike 下标：{finalStrikeIndex}");
    }

    private void OnEnable()
    {
        if (judgement != null)
            judgement.StrikeChargeStarted += OnStrikeStarted;
    }

    private void OnDisable()
    {
        if (judgement != null)
            judgement.StrikeChargeStarted -= OnStrikeStarted;
    }

    private void OnStrikeStarted(int index)
    {
        if (index != finalStrikeIndex)
            return;

        if (portalTriggered)
            return;

        portalTriggered = true;

        if (portal != null)
        {
            portal.SetActive(true);
            Debug.Log("[Portal] 最后一次 Strike 开始！传送门出现！");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (portal == null || !portal.activeInHierarchy)
            return;

        if (!other.CompareTag("Player"))
            return;

        Debug.Log("[Portal] 球进入传送门，加载下一关！");
        SceneManager.LoadScene(nextSceneName);
    }
}
