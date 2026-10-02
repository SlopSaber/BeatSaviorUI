using BeatSaviorUI.Utilities;

namespace BeatSaviorUI.Models;

internal class PlayTelemetry
{
    public float Speed { get; private set; }
    public float PreSwing { get; private set; }
    public float PostSwing { get; private set; }
    public float TimeDependence { get; private set; }
    public Accuracy Accuracy { get; private set; }
	
    public void ProcessNote(NoteSnapshot note)
    {
        Accuracy += (note.BeforeScore, note.CenterScore, note.AfterScore);
        Speed += note.Speed;
        PreSwing += note.PreSwing;
        PostSwing += note.PostSwing;
        TimeDependence += note.TimeDependence;
    }

    public void ProcessNoteHitCount(int noteHitCount)
    {
        Accuracy /= noteHitCount;
        Speed = SafeMath.Divide(Speed, noteHitCount);
        TimeDependence = SafeMath.Divide(TimeDependence, noteHitCount);
        PreSwing = SafeMath.Divide(PreSwing, noteHitCount);
        PostSwing = SafeMath.Divide(PostSwing, noteHitCount);
    }
}
