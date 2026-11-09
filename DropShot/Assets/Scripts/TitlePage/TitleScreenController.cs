using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleScreenController : MonoBehaviour
{
    [Header("主菜单按钮")]
    public GameObject menuButtons;

    [Header("菜单退出动画")]
    public Animator menuExitAnimator;
    public string menuExitAnimationName = "MenuExit";

    [Header("Loading 动画")]
    public Animator loadingAnimator;
    public string loadingAnimationName = "Loading";

    [Header("Loading 完成动画")]
    public string loadingCompleteAnimationName = "LoadingComplete";

    [Header("场景")]
    public string gameplaySceneName = "Gameplay";

    private bool isLoading = false;

    public void OnStartButtonClicked()
    {
        if (isLoading)
            return;

        StartCoroutine(LoadingRoutine());
    }

    IEnumerator LoadingRoutine()
    {
        isLoading = true;

        // 开始后台加载 Gameplay
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(gameplaySceneName);

        operation.allowSceneActivation = false;

        // 播放 START + QUIT 一起的退出动画
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

        // 动画结束后隐藏菜单
        if (menuButtons != null)
            menuButtons.SetActive(false);

        // 播放 Loading
        if (loadingAnimator != null)
        {
            loadingAnimator.gameObject.SetActive(true);

            loadingAnimator.Play(
                loadingAnimationName,
                0,
                0f
            );
        }

        // 等待 Gameplay 加载完成
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // 播放 Loading Complete
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

        // 进入 Gameplay
        operation.allowSceneActivation = true;
    }
}
