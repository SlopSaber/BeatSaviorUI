using System.Collections.Generic;

namespace BeatSaviorUI.Models;

internal class CompletionResultsExtraData(NoteSnapshot[] notes, int maxCombo, int bombHitCount, int pauseCount, int wallHitCount)
{
    public IReadOnlyList<NoteSnapshot> Notes { get; } = notes;
    public int MaxCombo { get; } = maxCombo;
    public int BombHitCount { get; } = bombHitCount;
    public int PauseCount { get; } = pauseCount;
    public int WallHitCount { get; } = wallHitCount;
}
