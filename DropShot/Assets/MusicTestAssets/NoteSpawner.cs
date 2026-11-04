using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("谱面")]
    public ChartReader chartReader;

    [Header("音符预制体")]
    public GameObject normalPrefab;
    public GameObject longPrefab;

    [Header("轨道位置")]
    public Transform lane0;
    public Transform lane1;

    private int nextNoteIndex = 0;

    private void Update()
    {
        if (chartReader == null || chartReader.ChartNotes == null)
            return;

        if (!chartReader.musicManager.IsPlaying)
            return;

        float currentTime = chartReader.musicManager.MusicTime;

        while (nextNoteIndex < chartReader.ChartNotes.Length)
        {
            NoteData note = chartReader.ChartNotes[nextNoteIndex];

            if (currentTime < note.time)
                break;

            SpawnNote(note);
            nextNoteIndex++;
        }
    }

    private void SpawnNote(NoteData note)
    {
        if (note.type == "Strike")
            return;

        Transform lane = note.lane == 0 ? lane0 : lane1;

        GameObject prefab = note.type == "Long"
            ? longPrefab
            : normalPrefab;

        Instantiate(prefab, lane.position, Quaternion.identity);
    }
}
