using System;
using System.Collections.Generic;
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
		private float scoreOffset;

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

			try
			{
				foreach (Transform t in graphObject.transform)
					Destroy(t.gameObject);

				var data = playData.GraphData;
				if (!data.HasData)
				{
					if (data.Error != null)
						Plugin.Log.Error(data.Error);
					return;
				}

				scoreOffset = data.ScoreOffset;
				lastSongBeat = Mathf.CeilToInt(playData.BeatmapInfo.SongDuration);
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

				foreach (var label in data.HorizontalLabels)
				{
					CreateHorizontalLabelLine(label.Value, new Color(label.Red, label.Green, label.Blue, .75f));
				}

				foreach (var segment in data.Segments)
				{
					CreateGraphLine((segment.StartTime, segment.StartValue), (segment.EndTime, segment.EndValue), Color.white);
				}

				foreach (var label in data.VerticalLabels)
				{
					var color = label.IsFailed ? Color.red : new Color(1, 1, 1, .75f);
					CreateVerticalLabelLine(label.Time, label.Value, color, label.Text);
				}

				if (data.Error != null)
					Plugin.Log.Error(data.Error);
			}
			catch (Exception e)
			{
				Plugin.Log.Error(e);
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

	}
}
