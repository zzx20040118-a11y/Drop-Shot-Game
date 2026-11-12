using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DropShot.UI
{
    // 全屏淡入淡出：场景开始时从奶油色淡入，切场景时淡出后再加载
    [RequireComponent(typeof(CanvasGroup))]
    public class SceneFader : MonoBehaviour
    {
        public float fadeInSeconds = 0.6f;
        public float fadeOutSeconds = 0.45f;

        CanvasGroup group;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }

        void Start() => StartCoroutine(FadeIn());

        public IEnumerator FadeIn()
        {
            yield return UITween.Run(fadeInSeconds, t => group.alpha = 1f - UITween.InOutSine(t));
            group.blocksRaycasts = false;
        }

        public IEnumerator FadeOut()
        {
            group.blocksRaycasts = true;
            yield return UITween.Run(fadeOutSeconds, t => group.alpha = UITween.InOutSine(t));
        }

        // 场景名为空或没加进 Build Settings 时只淡出并报警告，方便单独预览
        public void FadeToScene(string sceneName) => StartCoroutine(FadeToSceneRoutine(sceneName));

        IEnumerator FadeToSceneRoutine(string sceneName)
        {
            yield return FadeOut();
            if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning($"[DropShot UI] 场景「{sceneName}」不在 Build Settings 里，没法跳转");
                yield return FadeIn();
            }
        }
    }
}
