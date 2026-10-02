namespace BeatSaviorUI.Models;

internal readonly struct NoteSnapshot
{
    public readonly ColorType ColorType;
    public readonly float Time;
    public readonly int BeforeScore;
    public readonly int CenterScore;
    public readonly int AfterScore;
    public readonly int TotalScore;
    public readonly float Speed;
    public readonly float PreSwing;
    public readonly float PostSwing;
    public readonly float TimeDependence;

    public bool IsAMiss => TotalScore == 0;

    public NoteSnapshot(Note note)
    {
        ColorType = note.ColorType;
        Time = note.Time;
        BeforeScore = note.Score[0];
        CenterScore = note.Score[1];
        AfterScore = note.Score[2];
        TotalScore = note.TotalScore;
        Speed = note.Speed;
        PreSwing = note.PreSwing;
        PostSwing = note.PostSwing;
        TimeDependence = note.TimeDependence;
    }
}
