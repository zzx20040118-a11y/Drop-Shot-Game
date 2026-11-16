using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using HeyHeyThere.SceneTransitionsFree;

public class TitleScreenController : MonoBehaviour
{
    [Header("Main Menu Buttons / 主菜单按钮")]
    public GameObject menuButtons;

    [Header("Menu Exit Animation / 菜单退出动画")]
    public Animator menuExitAnimator;
    public string menuExitAnimationName = "MenuExit";

    [Header("Loading Animation / Loading 动画")]
    public Animator loadingAnimator;
    public string loadingAnimationName = "Loading";

    [Header("Loading Complete Animation / Loading 完成动画")]
    public string loadingCompleteAnimationName = "LoadingComplete";

    [Header("Scene Transition / 场景转场")]
    public string transitionStyle = "iris";
    public float transitionDuration = 0.6f;

    [Header("Four Gameplay Scenes / 四个游戏场景")]
    public string modeAEasyScene = "ModeA_Easy";
    public string modeAHardScene = "ModeA_Hard";
    public string modeBEasyScene = "ModeB_Easy";
    public string modeBHardScene = "ModeB_Hard";

    private bool isLoading = false;

    // ================= START =================

    public void OnStartButtonClicked()
    {
        if (isLoading)
            return;

        TitleMenuAudio menuAudio = GetComponent<TitleMenuAudio>();
        if (menuAudio != null) menuAudio.PlayClick(true);

        StartCoroutine(LoadingRoutine());
    }

    public void OnQuitButtonClicked()
    {
        if (isLoading) return;
        isLoading = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ================= 加载流程 =================

    private IEnumerator LoadingRoutine()
    {
        isLoading = true;

        // 读取主菜单选择
        int selectedMode = PlayerPrefs.GetInt("SelectedMode", 0);
        int selectedDifficulty = PlayerPrefs.GetInt("SelectedDifficulty", 0);

        string gameplaySceneName;

        // 确定目标场景
        if (selectedMode == 0 && selectedDifficulty == 0)
        {
            gameplaySceneName = modeAEasyScene;
        }
        else if (selectedMode == 0 && selectedDifficulty == 1)
        {
            gameplaySceneName = modeAHardScene;
        }
        else if (selectedMode == 1 && selectedDifficulty == 0)
        {
            gameplaySceneName = modeBEasyScene;
        }
        else
        {
            gameplaySceneName = modeBHardScene;
        }

        Debug.Log("[TitleScreen] 即将进入场景：" + gameplaySceneName);

        // 后台加载游戏场景
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(gameplaySceneName);

        if (operation == null)
        {
            Debug.LogError("[TitleScreen] 场景加载失败：" + gameplaySceneName);
            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        // ================= 菜单退出动画 =================

        if (menuExitAnimator != null)
        {
            menuExitAnimator.Play(
                menuExitAnimationName,
                0,
                0f
            );

            yield return null;

            AnimatorStateInfo menuState =
                menuExitAnimator.GetCurrentAnimatorStateInfo(0);

            yield return new WaitForSecondsRealtime(
                menuState.length
            );
        }

        // 隐藏菜单
        if (menuButtons != null)
            menuButtons.SetActive(false);

        // ================= Loading 动画 =================

        if (loadingAnimator != null)
        {
            loadingAnimator.gameObject.SetActive(true);

            loadingAnimator.Play(
                loadingAnimationName,
                0,
                0f
            );

            yield return null;

            // 至少完整播放一遍 Loading
            while (true)
            {
                AnimatorStateInfo loadingState =
                    loadingAnimator.GetCurrentAnimatorStateInfo(0);

                if (loadingState.normalizedTime >= 1f &&
                    !loadingAnimator.IsInTransition(0))
                {
                    break;
                }

                yield return null;
            }
        }

        // ================= 等待场景加载到 90% =================

        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // ================= Loading Complete =================

        if (loadingAnimator != null)
        {
            loadingAnimator.Play(
                loadingCompleteAnimationName,
                0,
                0f
            );

            yield return null;

            AnimatorStateInfo completeState =
                loadingAnimator.GetCurrentAnimatorStateInfo(0);

            yield return new WaitForSecondsRealtime(
                completeState.length
            );
        }

        // ================= Iris 圆形闭合 =================

        var options = new SceneTransition.Options
        {
            Duration = transitionDuration,
            Color = Color.black
        };

        Debug.Log("[TitleScreen] 开始 Iris 闭合");

        yield return SceneTransition.Cover(
            transitionStyle,
            options
        );

        Debug.Log("[TitleScreen] Iris 闭合完成");

        // ================= 进入游戏 =================

        Debug.Log("[TitleScreen] 正在激活游戏场景");

        operation.allowSceneActivation = true;

        // 注意：
        // 场景切换后，当前主菜单协程会终止。
        // Iris 展开由游戏场景的 SceneRevealOnStart 负责。
    }
}
