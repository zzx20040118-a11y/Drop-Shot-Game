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
    public string modeAEasyScene = "ModeA_Easy";
    public string modeAHardScene = "ModeA_Hard";
    public string modeBEasyScene = "ModeB_Easy";
    public string modeBHardScene = "ModeB_Hard";

    private bool isModeA = true;
    private bool isEasy = true;

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
        string sceneName;

        if (isModeA && isEasy)
            sceneName = modeAEasyScene;
        else if (isModeA && !isEasy)
            sceneName = modeAHardScene;
        else if (!isModeA && isEasy)
            sceneName = modeBEasyScene;
        else
            sceneName = modeBHardScene;

        SceneManager.LoadScene(sceneName);
    }
}
