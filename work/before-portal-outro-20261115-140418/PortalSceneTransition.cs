using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DropShot.Judgement;

public class PortalSceneTransition : MonoBehaviour
{
    [Header("传送门")]
    public GameObject portal;
    public BoxCollider portalCollider;

    [Header("传送门移动")]
    public float portalMoveSpeed = 10f;

    [Header("滚动球场景切换延迟")]
    public float sceneLoadDelay = 5f;

    [Header("球")]
    public Collider flyingBallCollider;
    public Collider rollingBallCollider;

    [Header("判定系统")]
    public JudgementSystem judgement;

    [Header("场景")]
    public string nextSceneName;

    private bool portalActivated = false;
    private bool sceneLoading = false;

    private int finalStrikeIndex = -1;

    void Start()
    {
        if (portal != null)
        {
            portal.SetActive(false);
        }

        if (judgement == null)
        {
            Debug.LogError("[Portal] 没有设置 Judgement！");
            return;
        }

        FindLastStrike();

        judgement.StrikeAutoStarted += OnStrikeAutoStarted;

        Debug.Log("[Portal] 已开始监听 StrikeAutoStarted");
    }

    void FindLastStrike()
    {
        if (judgement.Chart == null ||
            judgement.Chart.notes == null)
        {
            Debug.LogError("[Portal] Chart 尚未加载！");
            return;
        }

        finalStrikeIndex = -1;

        for (int i = 0; i < judgement.Chart.notes.Count; i++)
        {
            var note = judgement.Chart.notes[i];

            if (NoteKindParser.Parse(note.type) == NoteKind.Strike)
            {
                finalStrikeIndex = i;
            }
        }

        Debug.Log("[Portal] 最后一次 Strike Index = " + finalStrikeIndex);
    }

    void OnStrikeAutoStarted(int noteIndex)
    {
        Debug.Log("[Portal] StrikeAutoStarted Index = " + noteIndex);

        if (noteIndex == finalStrikeIndex)
        {
            ActivatePortal();
        }
    }

    public void ActivatePortal()
    {
        if (portalActivated)
            return;

        portalActivated = true;

        if (portal != null)
        {
            portal.SetActive(true);
        }

        Debug.Log("[Portal] 传送门已激活！");
    }

    void Update()
    {
        if (!portalActivated || sceneLoading)
            return;

        // 传送门沿世界 X 轴移动
        transform.position += Vector3.right * portalMoveSpeed * Time.deltaTime;

        // 飞行球：接触后立即进入下一关
        if (CheckBallContact(flyingBallCollider))
        {
            sceneLoading = true;

            Debug.Log("[Portal] 飞行球接触传送门，立即进入下一关！");

            LoadNextScene();
            return;
        }

        // 滚动球：接触后延迟进入下一关
        if (CheckBallContact(rollingBallCollider))
        {
            sceneLoading = true;

            Debug.Log("[Portal] 滚动球接触传送门，等待 " + sceneLoadDelay + " 秒！");

            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }

    bool CheckBallContact(Collider ball)
    {
        if (ball == null ||
            !ball.enabled ||
            !ball.gameObject.activeInHierarchy ||
            portalCollider == null ||
            !portalCollider.enabled)
        {
            return false;
        }

        Vector3 direction;
        float distance;

        return Physics.ComputePenetration(
            portalCollider,
            portalCollider.transform.position,
            portalCollider.transform.rotation,
            ball,
            ball.transform.position,
            ball.transform.rotation,
            out direction,
            out distance
        );
    }

    // 使用真实时间等待，不受 Time.timeScale 影响
    IEnumerator LoadNextSceneAfterDelay()
    {
        Debug.Log("[Portal] 开始真实时间倒计时：" + sceneLoadDelay + " 秒");

        yield return new WaitForSecondsRealtime(sceneLoadDelay);

        Debug.Log("[Portal] 延迟结束，准备切换场景！");

        LoadNextScene();
    }

    void LoadNextScene()
    {
        Debug.Log("[Portal] 进入下一关：" + nextSceneName);

        SceneManager.LoadScene(nextSceneName);
    }

    void OnDestroy()
    {
        if (judgement != null)
        {
            judgement.StrikeAutoStarted -= OnStrikeAutoStarted;
        }
    }
}
