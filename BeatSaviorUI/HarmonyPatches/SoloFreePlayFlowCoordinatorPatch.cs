using BeatSaviorUI.UI;
using HMUI;
using System;
using System.Collections;
using System.Threading.Tasks;
using BeatSaviorUI.Models;
using BeatSaviorUI.Stats;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using UnityEngine;

namespace BeatSaviorUI.HarmonyPatches
{
	[UsedImplicitly]
	internal class SoloFreePlayFlowCoordinatorPatch : IAffinity, IDisposable
	{
		private SoloFreePlayFlowCoordinator? presentationOwner;
		private Coroutine? pendingPresentation;
		private int presentationVersion;
		private bool disposed;
		private PluginConfig PluginConfig { get; }
		private EndOfLevelViewController EndOfLevelViewController { get; }
		private ScoreGraphViewController ScoreGraphViewController { get; }

		public SoloFreePlayFlowCoordinatorPatch(EndOfLevelViewController endOfLevelViewController, ScoreGraphViewController scoreGraphViewController, PluginConfig pluginConfig)
		{
			EndOfLevelViewController = endOfLevelViewController;
			ScoreGraphViewController = scoreGraphViewController;
			PluginConfig = pluginConfig;
		}

		[AffinityPatch(typeof(SoloFreePlayFlowCoordinator), nameof(SoloFreePlayFlowCoordinator.ProcessLevelCompletionResultsAfterLevelDidFinish))]
		private void Postfix(ref SoloFreePlayFlowCoordinator __instance, LevelCompletionResults levelCompletionResults, BeatmapLevel beatmapLevel)
		{
			RetirePresentation();
			if (disposed) return;
			if (!PluginConfig.Enabled) return;
			if (levelCompletionResults.levelEndAction != LevelCompletionResults.LevelEndAction.None)
			{
				return;
			}

			Task<PlayDataPreparation.Result>? task = PlayDataPreparation.Pending;
			if (task != null)
			{
				presentationOwner = __instance;
				pendingPresentation = __instance.StartCoroutine(PresentWhenReady(__instance, beatmapLevel, levelCompletionResults,
					task, PlayDataPreparation.Generation, presentationVersion));
				return;
			}
			if (PluginConfig.LastKnownPlayData is null)
			{
				Plugin.Log.Error("There is no last known play data available. Skipping showing results data.");
				return;
			}

			ShowResults(__instance, beatmapLevel, PluginConfig.LastKnownPlayData);
		}

		private IEnumerator PresentWhenReady(SoloFreePlayFlowCoordinator owner, BeatmapLevel level, LevelCompletionResults completion,
			Task<PlayDataPreparation.Result> task, long generation, int version)
		{
			while (!task.IsCompleted)
			{
				if (!CanPresent(owner, completion, task, generation, version))
					yield break;
				yield return null;
			}
			if (!CanPresent(owner, completion, task, generation, version))
				yield break;
			PlayData? data = PlayDataPreparation.Publish(task, generation);
			pendingPresentation = null;
			if (data != null)
				ShowResults(owner, level, data);
		}

		private bool CanPresent(SoloFreePlayFlowCoordinator owner, LevelCompletionResults completion, Task<PlayDataPreparation.Result> task,
			long generation, int version) =>
			!disposed && presentationVersion == version && PluginConfig.Enabled
			&& PlayDataPreparation.IsCurrent(task, generation)
			&& owner != null && owner.isActiveAndEnabled
			&& owner._resultsViewController != null && owner._resultsViewController.isActiveAndEnabled
			&& ReferenceEquals(owner._resultsViewController._levelCompletionResults, completion)
			&& EndOfLevelViewController != null && ScoreGraphViewController != null;

		private void ShowResults(SoloFreePlayFlowCoordinator owner, BeatmapLevel level, PlayData data)
		{
			Plugin.Log.Debug($"Showing {nameof(ResultsViewController)}");
			
			owner.SetLeftScreenViewController(EndOfLevelViewController, ViewController.AnimationType.None);
			EndOfLevelViewController.SetData(data, level);

			if (!PluginConfig.DisableGraphPanel)
			{
				owner.SetRightScreenViewController(ScoreGraphViewController, ViewController.AnimationType.None);
				ScoreGraphViewController.Refresh(data);
			}
		}

		private void RetirePresentation()
		{
			++presentationVersion;
			if (presentationOwner != null && pendingPresentation != null)
				presentationOwner.StopCoroutine(pendingPresentation);
			pendingPresentation = null;
			presentationOwner = null;
		}

		public void Dispose()
		{
			disposed = true;
			RetirePresentation();
		}
	}
}
