using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaviorUI.Utilities;

namespace BeatSaviorUI.Models;

internal class PlayData
{
	public float ModifiersMultiplier { get; }
    public float ScoreRatio { get; }

    public Rank Rank { get; }
    public bool Won { get; }
    public bool FullCombo { get; }
    public int ComboBreaks { get; }

    public Dictionary<float, float> Graph { get; } = [];

    public PlayTelemetry Left { get; } = new();
    public PlayTelemetry Right { get; } = new();
    
    public CompletionResultsExtraData CompletionResultsExtraData { get; }
    public BeatmapInfo BeatmapInfo { get; }
    public ScoreGraphData GraphData { get; }
    
    public PlayData(PreparationRequest request)
    {
	    CompletionResultsExtraData = request.ExtraData;
	    BeatmapInfo = request.BeatmapInfo;
        
        Won = request.Won;
        FullCombo = request.FullCombo;
		
        Rank = request.Rank;
        ModifiersMultiplier = request.ModifiersMultiplier;
        ScoreRatio = SafeMath.Divide(CompletionResultsExtraData.Notes.Select(n => n.TotalScore).Sum(), 
										Utils.MaxRawScoreForNumberOfNotes(CompletionResultsExtraData.Notes.Count));
        
        var rawGraph = new Dictionary<float, float>();
        var lastGraphNodes = new Queue<float>();
        int actualScore = 0;
        int maxScore = 0;
        int multiplier = 1;
        int multiplierProgress = 0;
        int lastBeat = 0;

        int leftNoteHitCount = 0;
        int rightNoteHitCount = 0;
        
        foreach(var note in CompletionResultsExtraData.Notes)
		{
			actualScore += note.TotalScore;

			if (multiplier < 8)
			{
				multiplierProgress++;
				if (multiplierProgress == multiplier * 2)
				{
					multiplier *= 2;
					multiplierProgress = 0;
				}
			}

			maxScore += 115 * multiplier;

			float noteBeat = MathF.Truncate(note.Time);
			if (noteBeat > lastBeat)
			{
				rawGraph.Add(noteBeat, (float) actualScore / maxScore);
				lastBeat = (int) noteBeat;
			}

			if (note.IsAMiss)
			{
				ComboBreaks++;
				continue;
			}
			
			if (note.ColorType == ColorType.ColorA)
			{
				leftNoteHitCount++;
				Left.ProcessNote(note);
			} 
			else
			{
				rightNoteHitCount++;
				Right.ProcessNote(note);
			}
		}
        
		foreach(var point in rawGraph) {

			lastGraphNodes.Enqueue(point.Value);

			if (lastGraphNodes.Count > 5)
				lastGraphNodes.Dequeue();

			Graph.Add(point.Key, lastGraphNodes.Average());
		}

		Left.ProcessNoteHitCount(leftNoteHitCount);
		Right.ProcessNoteHitCount(rightNoteHitCount);
        GraphData = ScoreGraphData.Prepare(Graph, CompletionResultsExtraData.Notes, Won);
    }

    internal sealed class PreparationRequest
    {
        internal readonly CompletionResultsExtraData ExtraData;
        internal readonly BeatmapInfo BeatmapInfo;
        internal readonly bool Won;
        internal readonly bool FullCombo;
        internal readonly Rank Rank;
        internal readonly float ModifiersMultiplier;

        internal PreparationRequest(LevelCompletionResults results, CompletionResultsExtraData extraData, BeatmapInfo beatmapInfo)
        {
            ExtraData = extraData;
            BeatmapInfo = beatmapInfo;
            Won = results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared;
            FullCombo = results.fullCombo;
            Rank = new Rank(RankModel.GetRankName(results.rank));
            ModifiersMultiplier = GetTotalMultiplier(results.gameplayModifiers, results.energy);
        }
    }
    
    // Modifiers aren't used in UI, but they could be
    // public List<string> Modifiers { get; } = [];
    private static float GetTotalMultiplier(GameplayModifiers gameplayModifiers, float energy)
    {
        float multiplier = 1f;

        if (gameplayModifiers.disappearingArrows)
        {
	        multiplier += 0.07f;
	        // Modifiers.Add("DA");
        }

	    /*string songSpeedModifier = gameplayModifiers.songSpeed switch
        {
	        GameplayModifiers.SongSpeed.SuperFast => "SF",
	        GameplayModifiers.SongSpeed.Faster => "FS",
	        GameplayModifiers.SongSpeed.Slower => "SS",
	        _ => null
        };
	    if (songSpeedModifier != null) Modifiers.Add(songSpeedModifier);*/
        multiplier += gameplayModifiers.songSpeedMul - 1f;

        if (gameplayModifiers.ghostNotes)
        {
	        multiplier += 0.11f;
	        // Modifiers.Add("GN");
        }

        if (gameplayModifiers.noArrows)
        {
	        multiplier -= 0.30f;
	        // Modifiers.Add("NA");
        }

        if (gameplayModifiers.noBombs)
        {
	        multiplier -= 0.10f;
	        // Modifiers.Add("NB");
        }

        if (gameplayModifiers.noFailOn0Energy && energy == 0)
        {
	        multiplier -= 0.50f;
	        // Modifiers.Add("NF");
        }

        if (gameplayModifiers.enabledObstacleType == GameplayModifiers.EnabledObstacleType.NoObstacles)
        {
	        multiplier -= 0.05f;
	        // Modifiers.Add("NO");
        }

        if (gameplayModifiers.zenMode)
        {
	        // Modifiers.Add("ZM");
	        return 0f;
        }

        return multiplier >= 0f ? multiplier : 0f;
    }
}
