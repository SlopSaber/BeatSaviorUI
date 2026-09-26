using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaviorUI.Models;
using HarmonyLib;
using JetBrains.Annotations;
using Zenject;

namespace BeatSaviorUI.Stats;

[UsedImplicitly]
internal class LevelDataController : IInitializable, IDisposable
{
    private StandardLevelScenesTransitionSetupData StandardLevelScenesTransitionSetupData { get; }
    private BeatmapObjectManager BeatmapObjectManager { get; }
    private ScoreController ScoreController { get; }
    private PlayerHeadAndObstacleInteraction PlayerHeadAndObstacleInteraction { get; }
    private PauseController PauseController { get; }
    private GameplayCoreSceneSetupData GameplayCoreSceneSetupData { get; }
    private AudioTimeSyncController AudioTimeSyncController { get; }
    private PluginConfig Config { get; }

    public LevelDataController(StandardLevelScenesTransitionSetupData standardLevelScenesTransitionSetupData, BeatmapObjectManager beatmapObjectManager, PlayerHeadAndObstacleInteraction playerHeadAndObstacleInteraction, ScoreController scoreController, PauseController pauseController, GameplayCoreSceneSetupData gameplayCoreSceneSetupData, AudioTimeSyncController audioTimeSyncController, PluginConfig config)
    {
        StandardLevelScenesTransitionSetupData = standardLevelScenesTransitionSetupData;
        BeatmapObjectManager = beatmapObjectManager;
        PlayerHeadAndObstacleInteraction = playerHeadAndObstacleInteraction;
        ScoreController = scoreController;
        PauseController = pauseController;
        GameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
        AudioTimeSyncController = audioTimeSyncController;
        Config = config;
    }
    
    private readonly bool scoreSaberPlaybackEnabled = AccessTools.Method("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted:Prefix") != null;	
    
    private int combo;
    private int multiplier = 1;
    private int multiplierProgress;
    
    private readonly List<Note> notes = [];
    private int maxCombo;
    private int bombHitCount;
    private int pauseCount;
    private int wallHitCount;

    public void Initialize()
    {
        SwingTranspilerHandler.Reset();
        SwingTranspilerHandler.Enabled = Config.Enabled;
        if (!Config.Enabled) return;
        StandardLevelScenesTransitionSetupData.didFinishEvent += OnLevelFinished;
        ScoreController.scoringForNoteStartedEvent += OnScoringForNoteStarted;
        BeatmapObjectManager.noteWasCutEvent += OnNoteWasCut;
        BeatmapObjectManager.noteWasMissedEvent += OnNoteMissed;
        PlayerHeadAndObstacleInteraction.headDidEnterObstaclesEvent += OnHeadEnteredObstacle;
        PauseController.didPauseEvent += OnSongPaused;
    }

    public void Dispose()
    {
	    SwingTranspilerHandler.Enabled = false;
	    SwingTranspilerHandler.Reset();
	    StandardLevelScenesTransitionSetupData.didFinishEvent -= OnLevelFinished;
	    ScoreController.scoringForNoteStartedEvent -= OnScoringForNoteStarted;
	    BeatmapObjectManager.noteWasCutEvent -= OnNoteWasCut;
	    BeatmapObjectManager.noteWasMissedEvent -= OnNoteMissed;
	    PlayerHeadAndObstacleInteraction.headDidEnterObstaclesEvent -= OnHeadEnteredObstacle;
	    PauseController.didPauseEvent -= OnSongPaused;
    }

    private void OnLevelFinished(StandardLevelScenesTransitionSetupData standardLevelScenesTransitionSetupData, LevelCompletionResults levelCompletionResults)
    {
	    if (!Config.Enabled) return;
	    if (combo > maxCombo)
	    {
		    maxCombo = combo;
	    }

	    var playData = new CompletionResultsExtraData(notes, maxCombo, bombHitCount, pauseCount, wallHitCount);
	    var beatmapInfo = GetBeatmapInfo();
	    
	    PluginConfig.LastKnownPlayData = new(levelCompletionResults, playData, beatmapInfo);
    }

    private BeatmapInfo GetBeatmapInfo()
    {
	    string songID = GameplayCoreSceneSetupData.beatmapKey.levelId.Replace("custom_level_","").Split('_')[0];
	    string songDifficulty = GameplayCoreSceneSetupData.beatmapKey.difficulty.ToString().ToLower();
	    string gameMode = GameplayCoreSceneSetupData.beatmapKey.characteristic.SerializedName();

	    int songDifficultyRank = GameplayCoreSceneSetupData.beatmapKey.characteristic.SortingOrder();
	    string songName = GameplayCoreSceneSetupData.beatmapLevel.songName;
	    string songArtist = GameplayCoreSceneSetupData.beatmapLevel.songAuthorName;
	    string songMapper = GameplayCoreSceneSetupData.beatmapLevel.allMappers.FirstOrDefault() ?? "Unknown";

	    float songDuration = AudioTimeSyncController.songLength;

	    SongDataType songDataType;
	    float songSpeed = 1;
	    // float songStartTime = 0;
	    if (GameplayCoreSceneSetupData.practiceSettings != null) {
		    songDataType = SongDataType.Practice;
		    songSpeed = GameplayCoreSceneSetupData.practiceSettings.songSpeedMul;
		    // songStartTime = GameplayCoreSceneSetupData.practiceSettings.startSongTime;
	    }
	    else
	    {
		    songDataType = scoreSaberPlaybackEnabled ? SongDataType.Replay : SongDataType.None;
	    }

	    float songJumpDistance = GameplayCoreSceneSetupData.beatmapBasicData.noteJumpStartBeatOffset;

	    return new(songDataType, songID, songDifficulty, songName, songArtist, songMapper, gameMode, songDifficultyRank,
		    songSpeed, songDuration, songJumpDistance);
    }
    
    private void OnScoringForNoteStarted(ScoringElement scoringElement)
    {
	    if (scoringElement is not GoodCutScoringElement goodCut)
	    {
		    return;
	    }

	    var noteCutInfo = goodCut.cutScoreBuffer.noteCutInfo;

	    if (!noteCutInfo.allIsOK || goodCut.noteData.colorType == ColorType.None)
	    {
		    return;
	    }

	    combo++;
	    ComputeMultiplier(true);
	    notes.Add(new(goodCut, CutType.Cut, noteCutInfo, multiplier));
    }
    
    private void OnNoteWasCut(NoteController noteController, in NoteCutInfo noteCutInfo)
    {
	    if (IsGoodCut(in noteCutInfo))
	    {
		    return;
	    }
	    ComputeMultiplier(false);
    	
	    if (noteCutInfo.noteData.colorType != ColorType.None)
    	{
    		notes.Add(new(noteController, CutType.BadCut, noteCutInfo, multiplier));
    	} 
    	else if (noteCutInfo.noteData.colorType == ColorType.None)
    	{
    		bombHitCount++;
    	}
    }

    private void OnNoteMissed(NoteController controller)
    {
	    if (controller.noteData.colorType == ColorType.None)
	    {
		    return;
	    }

	    notes.Add(new Note(controller, CutType.Miss, multiplier));
	    ComputeMultiplier(false);
    }

    private void ComputeMultiplier(bool goodHit)
    {
    	if(!goodHit)
    	{
    		if (combo > maxCombo)
		    {
			    maxCombo = combo;
		    }

		    combo = 0;
    		if(multiplier > 1)
		    {
			    multiplier /= 2;
		    }

		    multiplierProgress = 0;
    	} 
	    else if (multiplier < 8)
    	{
    		multiplierProgress++;
    		if(multiplierProgress == multiplier * 2)
    		{
    			multiplierProgress = 0;
    			multiplier *= 2;
    		}
    	}
    }

    private void OnHeadEnteredObstacle()
    {
    	wallHitCount++;
    	ComputeMultiplier(false);
    }

    private void OnSongPaused() => 
	    pauseCount++;

    private static bool IsGoodCut(in NoteCutInfo noteCutInfo) =>
	    noteCutInfo.allIsOK && noteCutInfo.noteData.colorType != ColorType.None;
}
