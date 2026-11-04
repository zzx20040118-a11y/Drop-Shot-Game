using UnityEngine;

public class ChartReader : MonoBehaviour
{
    public TextAsset chartFile;
    public MusicManager musicManager;

    private ChartData chartData;
    public NoteData[] ChartNotes
    {
        get { return chartData != null ? chartData.notes : null; }
    }
    private int nextNoteIndex = 0;

    private void Start()
    {
        chartData = JsonUtility.FromJson<ChartData>(chartFile.text);
    }

    private void Update()
    {
        if (chartData == null || !musicManager.IsPlaying)
            return;

        float currentTime = musicManager.MusicTime;

        while (nextNoteIndex < chartData.notes.Length)
        {
            NoteData note = chartData.notes[nextNoteIndex];

            if (currentTime < note.time)
                break;

            TriggerNote(note);
            nextNoteIndex++;
        }
    }

    private void TriggerNote(NoteData note)
    {
        Debug.Log(note.type + " | Lane: " + note.lane + " | Time: " + note.time);
    }
}
