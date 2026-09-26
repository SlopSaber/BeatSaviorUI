using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaviorUI.Models;
using BeatSaviorUI.Utilities;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaviorUI.UI
{
	internal class EndOfLevelViewController : BSMLResourceViewController
    {
        [Inject] private readonly PluginConfig config = null!;
        [Inject] private readonly PlayerDataModel playerDataModel = null!;
        
        [UIObject("titleCard")] private readonly GameObject titleCard = null!;
        [UIObject("songCover")] private readonly GameObject songCover = null!;
        [UIComponent("title")] private readonly TextMeshProUGUI title = null!;
        [UIComponent("artist")] private readonly TextMeshProUGUI artist = null!;
        [UIComponent("mapper")] private readonly TextMeshProUGUI mapper = null!;
        [UIComponent("difficulty")] private readonly TextMeshProUGUI difficulty = null!;

        [UIObject("upperBand")] private readonly GameObject upperBand = null!;
        [UIObject("middleBand")] private readonly GameObject middleBand = null!;
        [UIObject("lowerBand")] private readonly GameObject lowerBand = null!;
        [UIComponent("rankLabel")] private readonly TextMeshProUGUI rankLabel = null!;
        [UIComponent("rank")] private readonly TextMeshProUGUI rank = null!;
        [UIComponent("percentLabel")] private readonly TextMeshProUGUI percentLabel = null!;
        [UIComponent("percent")] private readonly TextMeshProUGUI percent = null!;
        [UIComponent("comboLabel")] private readonly TextMeshProUGUI comboLabel = null!;
        [UIComponent("combo")] private readonly TextMeshProUGUI combo = null!;
        [UIComponent("missLabel")] private readonly TextMeshProUGUI missLabel = null!;
        [UIComponent("miss")] private readonly TextMeshProUGUI miss = null!;
        [UIComponent("pausesLabel")] private readonly TextMeshProUGUI pausesLabel = null!;
        [UIComponent("pauses")] private readonly TextMeshProUGUI pauses = null!;

        [UIObject("leftCircle")] private readonly GameObject leftCircle = null!;
        [UIObject("rightCircle")] private readonly GameObject rightCircle = null!;
        [UIComponent("leftAverage")] private readonly TextMeshProUGUI leftAverage = null!;
        [UIComponent("rightAverage")] private readonly TextMeshProUGUI rightAverage = null!;
        [UIComponent("leftBeforeCut")] private readonly TextMeshProUGUI leftBeforeCut = null!;
        [UIComponent("rightBeforeCut")] private readonly TextMeshProUGUI rightBeforeCut = null!;
        [UIComponent("leftAccuracy")] private readonly TextMeshProUGUI leftAccuracy = null!;
        [UIComponent("rightAccuracy")] private readonly TextMeshProUGUI rightAccuracy = null!;
        [UIComponent("leftAfterCut")] private readonly TextMeshProUGUI leftAfterCut = null!;
        [UIComponent("rightAfterCut")] private readonly TextMeshProUGUI rightAfterCut = null!;
        [UIComponent("leftTD")] private readonly TextMeshProUGUI leftTd = null!;
        [UIComponent("rightTD")] private readonly TextMeshProUGUI rightTd = null!;
        [UIComponent("leftSpeed")] private readonly TextMeshProUGUI leftSpeed = null!;
        [UIComponent("rightSpeed")] private readonly TextMeshProUGUI rightSpeed = null!;
        [UIComponent("leftBeforeSwing")] private readonly TextMeshProUGUI leftBeforeSwing = null!;
        [UIComponent("rightBeforeSwing")] private readonly TextMeshProUGUI rightBeforeSwing = null!;
        [UIComponent("leftAfterSwing")] private readonly TextMeshProUGUI leftAfterSwing = null!;
        [UIComponent("rightAfterSwing")] private readonly TextMeshProUGUI rightAfterSwing = null!;
        
        [UIComponent("creditsText")] private readonly TextMeshProUGUI creditsText = null!;

        public override string ResourceName => $"{nameof(BeatSaviorUI)}.UI.Views.EndOfLevelView.bsml";
        
        // Initialized in post-parse
        private ImageView songCoverImg = null!;
        private ImageView upperBandImg = null!;
        private ImageView lowerBandImg = null!;
        private ImageView leftCircleImg = null!;
        private ImageView rightCircleImg = null!;
        private int coverRequestVersion;
        private Coroutine? leftCircleAnimation;
        private Coroutine? rightCircleAnimation;

        [UIAction("#post-parse")]
        public void PostParse()
        {
            var playerData = playerDataModel.playerData;
            var colorScheme = playerData.colorSchemesSettings.GetSelectedColorScheme();
            var circleSprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "Circle");

            titleCard.GetComponent<ContentSizeFitter>().enabled = false;
            titleCard.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            songCoverImg = songCover.AddComponent<ImageView>();
            songCoverImg.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;

            title.enableAutoSizing = true;
            title.fontSizeMax = 15;
            title.fontSizeMin = 1;

            artist.enableAutoSizing = true;
            artist.fontSizeMax = 15;
            artist.fontSizeMin = 1;

            mapper.enableAutoSizing = true;
            mapper.fontSizeMax = 15;
            mapper.fontSizeMin = 1;

            difficulty.enableAutoSizing = true;
            difficulty.fontSizeMax = 15;
            difficulty.fontSizeMin = 1;

            upperBandImg = upperBand.AddComponent<ImageView>();
            upperBandImg.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.WhitePixel;
            upperBandImg.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;

            lowerBandImg = lowerBand.AddComponent<ImageView>();
            lowerBandImg.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.WhitePixel;
            lowerBandImg.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;

            leftCircleImg = leftCircle.AddComponent<ImageView>();
            leftCircleImg.sprite = circleSprite;
            leftCircleImg.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
            leftCircleImg.color = colorScheme.saberAColor;
            leftCircleImg.preserveAspect = true;
            leftCircleImg.transform.localScale *= 1.55f;
            leftCircleImg.type = Image.Type.Filled;
            leftCircleImg.fillOrigin = (int)Image.Origin360.Top;
            leftCircleImg.fillAmount = 0;

            leftAverage.color = colorScheme.saberAColor;
            leftAverage.enableAutoSizing = true;
            leftAverage.fontSizeMin = 1;
            leftAverage.fontSizeMax = 12;

            leftBeforeCut.color = colorScheme.saberAColor;
            leftBeforeCut.overflowMode = TextOverflowModes.Overflow;

            leftAccuracy.color = colorScheme.saberAColor;
            leftAccuracy.overflowMode = TextOverflowModes.Overflow;

            leftAfterCut.color = colorScheme.saberAColor;
            leftAfterCut.overflowMode = TextOverflowModes.Overflow;

            leftTd.color = colorScheme.saberAColor;
            leftTd.overflowMode = TextOverflowModes.Overflow;

            leftSpeed.color = colorScheme.saberAColor;
            leftSpeed.overflowMode = TextOverflowModes.Overflow;

            leftBeforeSwing.color = colorScheme.saberAColor;
            leftBeforeSwing.overflowMode = TextOverflowModes.Overflow;

            leftAfterSwing.color = colorScheme.saberAColor;
            leftAfterSwing.overflowMode = TextOverflowModes.Overflow;

            var img = middleBand.AddComponent<ImageView>();
            img.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.WhitePixel;
            img.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
            img.color = new Color32(255, 255, 255, 100);

            rightCircleImg = rightCircle.AddComponent<ImageView>();
            rightCircleImg.sprite = circleSprite;
            rightCircleImg.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
            rightCircleImg.color = colorScheme.saberBColor;
            rightCircleImg.preserveAspect = true;
            rightCircleImg.transform.localScale *= 1.55f;
            rightCircleImg.type = Image.Type.Filled;
            rightCircleImg.fillOrigin = (int)Image.Origin360.Top;
            rightCircleImg.fillAmount = 0;

            rightAverage.color = colorScheme.saberBColor;
            rightAverage.enableAutoSizing = true;
            rightAverage.fontSizeMin = 1;
            rightAverage.fontSizeMax = 12;

            rightBeforeCut.color = colorScheme.saberBColor;
            rightBeforeCut.overflowMode = TextOverflowModes.Overflow;

            rightAccuracy.color = colorScheme.saberBColor;
            rightAccuracy.overflowMode = TextOverflowModes.Overflow;

            rightAfterCut.color = colorScheme.saberBColor;
            rightAfterCut.overflowMode = TextOverflowModes.Overflow;

            rightTd.color = colorScheme.saberBColor;
            rightTd.overflowMode = TextOverflowModes.Overflow;

            rightSpeed.color = colorScheme.saberBColor;
            rightSpeed.overflowMode = TextOverflowModes.Overflow;

            rightBeforeSwing.color = colorScheme.saberBColor;
            rightBeforeSwing.overflowMode = TextOverflowModes.Overflow;

            rightAfterSwing.color = colorScheme.saberBColor;
            rightAfterSwing.overflowMode = TextOverflowModes.Overflow;

            creditsText.text = $"{Plugin.Metadata.Name} v{Plugin.Metadata.HVersion} by {Plugin.Metadata.Author}";
        }

        public void SetData(PlayData playData, BeatmapLevel beatmapLevel)
        {
            var coverRequest = ++coverRequestVersion;
            songCoverImg.sprite = null;
            _ = LoadCoverAsync(beatmapLevel, coverRequest);
            
            title.text = playData.BeatmapInfo.SongName;
            artist.text = playData.BeatmapInfo.SongArtist;
            mapper.text = playData.BeatmapInfo.SongMapper;

            (difficulty.text, difficulty.color) = playData.BeatmapInfo.GetDifficultyNameAndColor();

            if(!Plugin.Fish)
            {
                rank.text = playData.Rank.Name;
                percent.text = $"{playData.ScoreRatio * 100:F} %"; //broke with mods
                combo.text = $"{playData.CompletionResultsExtraData.MaxCombo}"; //broke
                miss.text = playData.FullCombo ? "FC" : $"{playData.ComboBreaks}";
                pauses.text = config.HidePauseCount ? "-" : $"{playData.CompletionResultsExtraData.PauseCount}";
            }
            else
            {
                rankLabel.text = "Never";
                rank.text = "Never";
                percentLabel.text = "gonna";
                percent.text = "gonna";
                comboLabel.text = "give";
                combo.text = "let";
                missLabel.text = "you";
                miss.text = "you";
                pausesLabel.text = "up,";
                pauses.text = "down.";
            }

            rank.color = playData.Rank.Color;
            
            var color = playData.FullCombo ? new(0.93f, 0.79f, 0.4f) : Color.white;
            miss.color = color;
            missLabel.color = color;
            lowerBandImg.color = color;
            upperBandImg.color = color;

            var leftRatio = playData.Left.Accuracy.Sum() / 115f;
            var rightRatio = playData.Right.Accuracy.Sum() / 115f;
            
            if (leftCircleAnimation != null)
                StopCoroutine(leftCircleAnimation);
            if (rightCircleAnimation != null)
                StopCoroutine(rightCircleAnimation);
            leftCircleAnimation = StartCoroutine(AnimateCircle(leftCircleImg, leftRatio, 1.5f));
            rightCircleAnimation = StartCoroutine(AnimateCircle(rightCircleImg, rightRatio, 1.5f));
            
            leftAverage.text = $"{leftRatio:F2}";
            rightAverage.text = $"{rightRatio:F2}";

            leftBeforeCut.text = $"{playData.Left.Accuracy.Before:F1}";
            rightBeforeCut.text = $"{playData.Right.Accuracy.Before:F1}";

            leftAccuracy.text = $"{playData.Left.Accuracy.Center:F1}";
            rightAccuracy.text = $"{playData.Right.Accuracy.Center:F1}";
            
            leftAfterCut.text = $"{playData.Left.Accuracy.After:F1}";
            rightAfterCut.text = $"{playData.Right.Accuracy.After:F1}";
            
            leftTd.text = $"{playData.Left.TimeDependence:F3}";
            rightTd.text = $"{playData.Right.TimeDependence:F3}";

            leftSpeed.text = $"{playData.Left.Speed * 3.6f:F2} Km/h"; 
            rightSpeed.text = $"{playData.Right.Speed * 3.6f:F2} Km/h"; 
            
            leftBeforeSwing.text = $"{playData.Left.PreSwing * 100f:F2} %";
            rightBeforeSwing.text = $"{playData.Right.PreSwing * 100f:F2} %";
            
            leftAfterSwing.text = $"{playData.Left.PostSwing * 100f:F2} %";
            rightAfterSwing.text = $"{playData.Right.PostSwing * 100f:F2} %";
        }

        private async Task LoadCoverAsync(BeatmapLevel beatmapLevel, int requestVersion)
        {
            try
            {
                var cover = await beatmapLevel.previewMediaData.GetCoverSpriteAsync();
                if (requestVersion == coverRequestVersion && songCoverImg != null)
                    songCoverImg.sprite = cover;
            }
            catch (System.Exception e)
            {
                Plugin.Log.Warn($"Could not load results cover: {e}");
            }
        }

        protected override void OnDestroy()
        {
            coverRequestVersion++;
            base.OnDestroy();
        }

        private static IEnumerator AnimateCircle(ImageView img, float final, float totalTime)
        {
            float timeLeft = totalTime;
            img.fillAmount = 0;

            yield return new WaitForSeconds(1.5f);

            while(timeLeft > Time.deltaTime)
            {
                timeLeft -= Time.deltaTime;
                img.fillAmount = Mathf.SmoothStep(0, final, 1 - timeLeft / totalTime);
                yield return null;
            }

            img.fillAmount = final;
        }
    }
}
