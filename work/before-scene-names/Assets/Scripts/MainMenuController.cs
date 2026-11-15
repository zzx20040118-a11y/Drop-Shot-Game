using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("输入模式图片")]
    public GameObject modeASelected;
    public GameObject modeBSelected;

    [Header("难度图片")]
    public GameObject easySelected;
    public GameObject hardSelected;

    [Header("教程 Panel")]
    public GameObject modeATutorialPanel;
    public GameObject modeBTutorialPanel;

    [Header("四个游戏场景")]
    public string modeAEasyScene = "Game_Easy";
    public string modeAHardScene = "Game_Space_NoHints";
    public string modeBEasyScene = "Game_Hard";
    public string modeBHardScene = "Game_DF_NoHints";

    private bool isModeA = true;
    private bool isEasy = true;
    private bool isLoading;

    private void Start()
    {
        // 默认选择 Mode A + Easy
        SelectModeA();
        SelectEasy();

        modeATutorialPanel.SetActive(false);
        modeBTutorialPanel.SetActive(false);
    }

    // ================= 输入模式 =================

    public void SelectModeA()
    {
        isModeA = true;

        modeASelected.SetActive(true);
        modeBSelected.SetActive(false);

        modeATutorialPanel.SetActive(true);
        modeBTutorialPanel.SetActive(false);
    }

    public void SelectModeB()
    {
        isModeA = false;

        modeASelected.SetActive(false);
        modeBSelected.SetActive(true);

        modeBTutorialPanel.SetActive(true);
        modeATutorialPanel.SetActive(false);
    }

    // ================= 难度 =================

    public void SelectEasy()
    {
        isEasy = true;

        easySelected.SetActive(true);
        hardSelected.SetActive(false);
    }

    public void SelectHard()
    {
        isEasy = false;

        easySelected.SetActive(false);
        hardSelected.SetActive(true);
    }

    // ================= 开始游戏 =================

    public void StartGame()
    {
        if (isLoading) return;
        string sceneName;

        if (isModeA && isEasy)
            sceneName = modeAEasyScene;
        else if (isModeA && !isEasy)
            sceneName = modeAHardScene;
        else if (!isModeA && isEasy)
            sceneName = modeBEasyScene;
        else
            sceneName = modeBHardScene;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("无法加载游戏场景：" + sceneName, this);
            return;
        }
        isLoading = true;
        DropShot.UI.ScoreBoard.GameSceneName = sceneName;
        SceneManager.LoadSceneAsync(sceneName);
    }
}
