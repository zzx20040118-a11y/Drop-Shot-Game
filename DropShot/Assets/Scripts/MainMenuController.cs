using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Input Mode Images / 输入模式图片")]
    public GameObject modeASelected;
    public GameObject modeBSelected;

    [Header("Difficulty Images / 难度图片")]
    public GameObject easySelected;
    public GameObject hardSelected;

    [Header("Tutorial Panels / 教程 Panel")]
    public GameObject modeATutorialPanel;
    public GameObject modeBTutorialPanel;

    // 0 = Mode A, 1 = Mode B
    private int selectedMode = 0;

    // 0 = Easy, 1 = Hard
    private int selectedDifficulty = 0;

    private void Start()
    {
        // 默认选择 Mode A + Easy
        SelectModeA();
        SelectEasy();

        if (modeATutorialPanel != null)
            modeATutorialPanel.SetActive(false);

        if (modeBTutorialPanel != null)
            modeBTutorialPanel.SetActive(false);
    }

    // ================= 输入模式 =================

    public void SelectModeA()
    {
        selectedMode = 0;

        PlayerPrefs.SetInt("SelectedMode", selectedMode);
        PlayerPrefs.Save();

        if (modeASelected != null)
            modeASelected.SetActive(true);

        if (modeBSelected != null)
            modeBSelected.SetActive(false);

        if (modeATutorialPanel != null)
            modeATutorialPanel.SetActive(true);

        if (modeBTutorialPanel != null)
            modeBTutorialPanel.SetActive(false);
    }

    public void SelectModeB()
    {
        selectedMode = 1;

        PlayerPrefs.SetInt("SelectedMode", selectedMode);
        PlayerPrefs.Save();

        if (modeASelected != null)
            modeASelected.SetActive(false);

        if (modeBSelected != null)
            modeBSelected.SetActive(true);

        if (modeATutorialPanel != null)
            modeATutorialPanel.SetActive(false);

        if (modeBTutorialPanel != null)
            modeBTutorialPanel.SetActive(true);
    }

    // ================= 难度 =================

    public void SelectEasy()
    {
        selectedDifficulty = 0;

        PlayerPrefs.SetInt("SelectedDifficulty", selectedDifficulty);
        PlayerPrefs.Save();

        if (easySelected != null)
            easySelected.SetActive(true);

        if (hardSelected != null)
            hardSelected.SetActive(false);
    }

    public void SelectHard()
    {
        selectedDifficulty = 1;

        PlayerPrefs.SetInt("SelectedDifficulty", selectedDifficulty);
        PlayerPrefs.Save();

        if (easySelected != null)
            easySelected.SetActive(false);

        if (hardSelected != null)
            hardSelected.SetActive(true);
    }
}
