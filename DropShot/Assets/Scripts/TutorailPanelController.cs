using UnityEngine;

public class TutorialPanelController : MonoBehaviour
{
    [Header("Tutorial Pages / 教程页面")]
    public GameObject[] pages;

    private int currentPage = 0;

    private void OnEnable()
    {
        currentPage = 0;
        ShowPage();
    }

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            ShowPage();
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage();
        }
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void ShowPage()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == currentPage);
        }
    }
}
