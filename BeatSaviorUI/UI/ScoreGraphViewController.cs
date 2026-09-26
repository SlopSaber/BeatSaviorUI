using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaviorUI.Models;
using BeatSaviorUI.Utilities;
using HMUI;
using TMPro;
using UnityEngine;

namespace BeatSaviorUI.UI
{
	internal class ScoreGraphViewController : BSMLResourceViewController
	{
		public override string ResourceName => $"{nameof(BeatSaviorUI)}.UI.Views.ScoreGraphView.bsml";

		[UIObject("graph")] private readonly GameObject graphObject = null!;
		[UIComponent("title")] private readonly CurvedTextMeshPro titleText = null!;
		[UIComponent("creditsText")] private readonly TextMeshProUGUI creditsText = null!;

		private int lastSongBeat = 0;
		private const float Width = 90;
		private const float Height = 45;
		private const float OffsetX = 10f;
		private const float OffsetY = 5f;
		private const int MaxGraphSegments = 240;
		private float scoreOffset;
		private bool won = true;

		private List<float> misses = [];
		private Dictionary<float, float> graph = [];

		private List<Color> Colors { get; } =
		[
			Color.yellow,
			new Color(1, 0.5f, 0, 1), // Orange
			Color.red,
			new Color(1, 0, 0.5f, 1), // Dark pink
			Color.magenta,
			new Color(0.5f, 0, 1, 1), // Purple
			Color.blue,
			new Color(0, 0.5f, 1), // Light blue
			Color.cyan,
			new Color(0, 1, 0.5f), // Blue-green
			Color.green,
			new Color(0.5f, 1, 0)
		];

		private List<string> uCute { get; } =
		[
			"You are awesome. Keep going !",
			"Smile ! You are beautiful <3",
			"You're not Mystogan#5049, but you're still awesome ! (Whoever that guy is)",
			"Damn you're good at this game !",
			"You're stunning *blushes*",
			"You made my day better. Thanks !",
			"I hope you had a great day !",
			"Everybody loves you, don't forget it <3",
			"I couldn't write this sentence without U in it <3",
			"There’s ordinary. And then there’s you.",
			"Aside from stats, you are my favorite.",
			"On a scale of 1 to 10, you're a 115.",
			"You're so hot, brother.",
			"Help me step bro, I'm stuck !",
			"Clearly, you've never played a Muffn map",
			"Is there anything you can’t do?",
			"I bet you’re smarter than Google.",
			"Your smile is proof that the best things in life are free.",
			"I love your sabers' colors !",
			"I wanted to write something more, but I don't need to. You're already all this mod needs <3",
			"I WANT MORE STAAAAAAAAAATS",
			"Someday I feel bad. But then I remember you're actually using my mod <3",
			"Everytime I see a comment on my mod, it makes me smile. Thank you <3",
			"More than 20000 users for a mod made by a monkey brain. I don't deserve you guys <3",
			// To the one reading this : You're awesome. I mean it.
			"DADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADADA",
			"Is Taichi still number one ?",
			"Overkill is such a good song !",
			"June is the best month. Change my mind.",
			"<3",
			"I bet you cannot play the next map on faster.",
			"You can cut notes but you'll never break my heart <3",
			"Duck.",
			"Duck plushies are the best plushies."
		];

		[UIAction("#post-parse")]
		public void PostParse()
		{
			creditsText.text = $"{Plugin.Metadata.Name} v{Plugin.Metadata.HVersion} by {Plugin.Metadata.Author}";
		}
		
		public void Refresh(PlayData playData)
		{
			lastSongBeat = 0;
			scoreOffset = 0;
			won = true;
			misses = [];

			try
			{
				foreach (Transform t in graphObject.transform)
					Destroy(t.gameObject);

				graph = playData.Graph;
				float lastGraphEntry = -1;

				// That should never happen but some magical things decided to fuck my monkey brain by doing impossible things once in a while and that's freaking annoying
				if (graph.Count == 0)
					return;

				GetOffsets(playData.CompletionResultsExtraData.Notes);
				lastSongBeat = Mathf.CeilToInt(playData.BeatmapInfo.SongDuration);
				won = playData.Won;
				if(!Plugin.Fish)
				{
					titleText.text = playData.BeatmapInfo.SongName;
				}
				else
				{
					titleText.text = uCute[Utils.Random.Next(uCute.Count)];
					titleText.color = Colors[Utils.Random.Next(Colors.Count)];
				}
				titleText.enableAutoSizing = true;
				titleText.fontSizeMin = 1;
				titleText.fontSizeMax = 10;

				CreateHorizontalLabels();

				var sampleStep = Mathf.Max(1, Mathf.CeilToInt((graph.Count - 1f) / MaxGraphSegments));
				var pointIndex = 0;
				var lastDrawnValue = 0f;
				var lastKey = 0f;
				var lastValue = 0f;
				foreach (var point in graph)
				{
					lastKey = point.Key;
					lastValue = point.Value;
					if (pointIndex == 0)
					{
						lastGraphEntry = point.Key;
						lastDrawnValue = point.Value;
					}
					else if (pointIndex % sampleStep == 0)
					{
						CreateGraphLine((lastGraphEntry, lastDrawnValue), (point.Key, point.Value), Color.white);
						lastGraphEntry = point.Key;
						lastDrawnValue = point.Value;
					}
					pointIndex++;
				}
				if (lastGraphEntry != lastKey)
					CreateGraphLine((lastGraphEntry, lastDrawnValue), (lastKey, lastValue), Color.white);

				CreateVerticalLabels();
			} 
			catch (Exception e)
			{
				Plugin.Log.Error(e);
			}
		} 

		private void CreateVerticalLabels()
		{
			// Add the easter egg "Connasse de chaise"

			List<float> drops = new List<float>();
			int labelsLeft = 3;

			if(misses.Count != labelsLeft)
				drops = FindScoreDrops();

			if (misses.Count < 4)
			{
				for (int i = 0; i < misses.Count; i++)
				{
					CreateVerticalLabelLine(misses[i], graph[misses[i]], new Color(1, 1, 1, .75f), "Missed");
					labelsLeft--;
				}
			}

			for (int i = 0; i < drops.Count && labelsLeft > 0; i++)
			{
				if (misses.Count < 4 && !misses.Contains(drops[i]) || misses.Count >= 4)
				{
					CreateVerticalLabelLine(drops[i], graph[drops[i]], new Color(1, 1, 1, .75f), "Score drop");
					labelsLeft--;
				}
			}

			if (!won)
				CreateVerticalLabelLine(graph.Last().Key, graph.Last().Value, Color.red, "Failed");
		}

		private List<float> FindScoreDrops()
		{
			int dropWindow = 2;     // in seconds
			int firstBeat = (int)graph.First().Key;
			int lastBeat = (int)graph.Last().Key;
			int closestToDropWindow = firstBeat, dropSpacing = 4;
			float diff, dropDelta = -0.01f;
			List<(float, float)> drops = new List<(float, float)>();
			List<float> res = new List<float>();

			if (scoreOffset == 0.93f)
				dropDelta = 0.0025f;	// 0.25%
			else if (scoreOffset == 0.8f)
				dropDelta = 0.005f;		// 0.5%

			for (int i = firstBeat + dropWindow + 5; i < lastBeat; i++)
			{
				if (graph.ContainsKey(i - dropWindow))
					closestToDropWindow = i - dropWindow;

				if (graph.ContainsKey(i))
				{
					diff = graph[i] - graph[closestToDropWindow];
					if (diff < dropDelta)
					{
						drops.Add((i, diff));
						i += dropSpacing;
					}
				}
			}

			drops = drops.OrderBy(e => e.Item2).Take(3).ToList();

			while (res.Count < 3 && drops.Count > 0)
			{
				res.Add(drops.First().Item1);
				drops.RemoveAt(0);
			}

			return res;
		}

		private void CreateHorizontalLabels()
		{
			float increment = 0.1f;

			if (scoreOffset == 0.93f)
				increment = 0.01f;
			else if (scoreOffset == 0.8f)
				increment = 0.05f;

			for (float i = 0 ; i <= 1 + increment - 0.001f; i += increment)
			{
				if (i >= scoreOffset - 0.001f)
				{
					CreateHorizontalLabelLine(i, new Color(1, 0.2f + i * 0.8f, 0.2f + i * 0.8f, .75f));
				}
			}
		}

		private void CreateVerticalLabelLine(float x, float y, Color color, string text)
		{
			void CreateLabelText(string _text)
			{
				GameObject go = new GameObject("LabelText", typeof(FormattableText));
				RectTransform rt = go.GetComponent<RectTransform>();
				CurvedTextMeshPro tmp = go.GetComponent<FormattableText>();

				go.transform.SetParent(graphObject.transform, false);
				tmp.text = _text;
				tmp.color = color;
				tmp.alignment = TextAlignmentOptions.Center;
				tmp.fontSize = 3;
				tmp.fontSharedMaterial = titleText.fontSharedMaterial;

				rt.anchorMin = Vector2.zero;
				rt.anchorMax = Vector2.zero;
				rt.sizeDelta = new Vector2(12, 10);
				rt.anchoredPosition = new Vector2((x / lastSongBeat) * Width + OffsetX, -2f);
			}

			Vector2 pos1v = new Vector2((x / lastSongBeat) * Width + OffsetX, 3f),
					pos2v = new Vector2((x / lastSongBeat) * Width + OffsetX, ((y - scoreOffset) / (1 - scoreOffset)) * Height + OffsetY - 2f);

			CreateLine("LabelLine", pos1v, pos2v, color, 0.1f);
			CreateLabelText(text + " at " + $"{Math.Floor(x / 60):N0}:{Math.Floor(x % 60):00}");
		}

		private void CreateHorizontalLabelLine(float value, Color color)
		{
			void CreateLabelText(string text)
			{
				GameObject go = new GameObject("LabelText", typeof(FormattableText));
				RectTransform rt = go.GetComponent<RectTransform>();
				CurvedTextMeshPro tmp = go.GetComponent<FormattableText>();

				go.transform.SetParent(graphObject.transform, false);
				tmp.text = text;
				tmp.color = color;
				tmp.fontSize = 3;
				tmp.fontSharedMaterial = titleText.fontSharedMaterial;

				rt.anchorMin = Vector2.zero;
				rt.anchorMax = Vector2.zero;
				rt.pivot = new Vector2(0, 1);
				rt.sizeDelta = new Vector2(10, 5);
				rt.anchoredPosition = new Vector2(3f, ((value - scoreOffset) / (1 - scoreOffset)) * Height + OffsetY);
			}

			Vector2 pos1v = new Vector2(4f, ((value - scoreOffset) / (1 - scoreOffset)) * Height + OffsetY),
					pos2v = new Vector2(Width + 13f, ((value - scoreOffset) / (1 - scoreOffset)) * Height + OffsetY);

			CreateLine("LabelLine", pos1v, pos2v, color, 0.25f);
			CreateLabelText((value * 100).ToString("0") + " %");
		}

		private void CreateGraphLine((float, float) pos1, (float, float) pos2, Color color)
		{
			Vector2 pos1v = new Vector2((pos1.Item1 / lastSongBeat) * Width + OffsetX, ((pos1.Item2 - scoreOffset) / (1 - scoreOffset)) * Height + OffsetY),
					pos2v = new Vector2((pos2.Item1 / lastSongBeat) * Width + OffsetX, ((pos2.Item2 - scoreOffset) / (1 - scoreOffset)) * Height + OffsetY);

			CreateLine("GraphLine", pos1v, pos2v, color, 0.75f);
		}

		private void CreateLine(string name, Vector2 pos1v, Vector2 pos2v, Color color, float lineWidth)
		{
			GameObject go = new GameObject(name);

			Vector2 dir = (pos2v - pos1v).normalized;
			float distance = Vector2.Distance(pos1v, pos2v);

			ImageView image = go.AddComponent<ImageView>();
			image.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.WhitePixel;
			image.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
			image.color = color;

			go.transform.SetParent(graphObject.transform, false);
			RectTransform rt = go.GetComponent<RectTransform>();

			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.zero;
			rt.sizeDelta = new Vector2(distance, lineWidth);
			rt.anchoredPosition = pos1v + dir * distance * .5f;
			rt.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
		}

		private void GetOffsets(List<Note> notes)
		{
			foreach (Note n in notes)
			{
				if (n.IsAMiss && misses.Count < 4)
					misses.Add((float)Math.Truncate(n.Time));
			}

			scoreOffset = graph.Min(e => e.Value);

			if (scoreOffset >= 0.93f)
				scoreOffset = 0.93f;
			else if (scoreOffset >= 0.85f)
				scoreOffset = 0.8f;
			else
				scoreOffset -= 0.1f;
		}

	}
}
