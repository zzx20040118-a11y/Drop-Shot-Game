using UnityEngine;

public class StageSwitcher : MonoBehaviour
{
    public GameObject[] stages;
    private int a = 0;

    void Start()
    {
        ShowStage();
    }

    public void NextStage()
    {
        if (stages == null || a >= stages.Length - 1) return;

        a++;
        ShowStage();
    }

    void ShowStage()
    {
        if (stages == null) return;

        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i] != null)
                stages[i].SetActive(i == a);
        }
    }
}
