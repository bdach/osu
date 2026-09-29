// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.RankingV2.Argon
{
    public partial class TotalScoreWedge : CompositeDrawable, ISerialisableDrawable
    {
        public static readonly ColourInfo TEXT_GRADIENT = ColourInfo.GradientVertical(Colour4.White, Colour4.FromHex(@"B2E5FE"));

        private Container scoreContainer = null!;
        private Sprite perfectIndicator = null!;
        private Container personalBestIndicator = null!;
        private Box personalBestFlash = null!;
        private TotalScoreCounter totalScoreText = null!;

        [Resolved]
        private IBindable<IScoreInfo> score { get; set; } = null!;

        private Bindable<ScoringMode> scoringMode { get; set; } = new Bindable<ScoringMode>();

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, TextureStore textures, OsuConfigManager configManager)
        {
            Width = ArgonResultsScreenV2.LEFT_WEDGE_HEIGHT * 1.3f;
            Height = 115;

            InternalChildren =
            [
                scoreContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Shear = OsuGame.SHEAR,
                    CornerRadius = ShearedButton.CORNER_RADIUS,
                    Masking = true,
                    EdgeEffect = BeatmapInfoWedge.CreateShadowEdgeEffect(),
                    Children =
                    [
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background5.Opacity(0.98f),
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Horizontal,
                            Padding = new MarginPadding
                            {
                                Vertical = 15,
                                Right = 30,
                            },
                            Spacing = new Vector2(20),
                            Shear = -OsuGame.SHEAR,
                            // TODO: this layout is a hackjob and probably not going to hold up to scrutiny.
                            // think about how to make it better later
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Children =
                            [
                                new Container
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Size = new Vector2(65),
                                    Child = perfectIndicator = new Sprite
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Texture = textures.Get(@"Icons/Ranking/perfect"),
                                        Size = new Vector2(75),
                                        Colour = new ColourInfo
                                        {
                                            TopLeft = Colour4.FromHex(@"00FFAA"),
                                            TopRight = Colour4.FromHex(@"7CF6FF"),
                                            BottomLeft = Colour4.FromHex(@"7CF6FF"),
                                            BottomRight = Colour4.FromHex(@"FF9AD7"),
                                        }
                                    },
                                },
                                totalScoreText = new TotalScoreCounter
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                },
                            ]
                        }
                    ]
                },
                // TODO: not actually hooked up to anything because this doesn't exist on old screens
                // figure it out later
                personalBestIndicator = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Origin = Anchor.Centre,
                    RelativeAnchorPosition = new Vector2(0.95f, 0),
                    Shear = OsuGame.SHEAR,
                    CornerRadius = ShearedButton.CORNER_RADIUS,
                    Masking = true,
                    EdgeEffect = BeatmapInfoWedge.CreateShadowEdgeEffect(),
                    Children =
                    [
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourInfo.GradientVertical(Colour4.FromHex(@"#FFE7A8"), Colour4.FromHex(@"#FFB800")),
                        },
                        new OsuSpriteText
                        {
                            Colour = colourProvider.Background5,
                            Text = "PERSONAL BEST",
                            UseFullGlyphHeight = false,
                            Spacing = new Vector2(1.5f),
                            Font = OsuFont.Style.Body.With(weight: FontWeight.Bold),
                            Shear = -OsuGame.SHEAR,
                            Margin = new MarginPadding
                            {
                                Horizontal = 12,
                                Vertical = 6,
                            }
                        },
                        personalBestFlash = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.White,
                            Alpha = 0,
                            Blending = BlendingParameters.Additive,
                        }
                    ]
                }
            ];

            configManager.BindWith(OsuSetting.ScoreDisplayMode, scoringMode);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            score.BindValueChanged(_ => updateState());
            scoringMode.BindValueChanged(_ => updateState(), true);
        }

        private void updateState()
        {
            totalScoreText.SetCountWithoutRolling(score.Value.GetDisplayScore(scoringMode.Value));
            perfectIndicator.Alpha = score.Value.MaxCombo == score.Value.GetMaximumAchievableCombo() ? 1 : 0;
        }

        public double StartAnimating(double startTime)
        {
            const double transition_duration = 500;

            scoreContainer.FadeOut()
                          .MoveToOffset(new Vector2(-50, 0));

            perfectIndicator.FadeOut()
                            .RotateTo(30)
                            .ScaleTo(new Vector2(1.2f));

            personalBestIndicator.FadeOut()
                                 .ScaleTo(new Vector2(1.2f));

            double latestTransformEndTime = startTime;

            using (BeginAbsoluteSequence(latestTransformEndTime))
            {
                scoreContainer.FadeIn(transition_duration, Easing.OutQuint)
                              .MoveToOffset(new Vector2(50, 0), transition_duration, Easing.OutQuint);
            }

            totalScoreText.ResetCount();
            totalScoreText.Current.Value = score.Value.GetDisplayScore(scoringMode.Value);
            latestTransformEndTime = totalScoreText.LatestTransformEndTime;

            if (score.Value.MaxCombo == score.Value.GetMaximumAchievableCombo())
            {
                using (BeginAbsoluteSequence(latestTransformEndTime))
                {
                    perfectIndicator.FadeIn(150, Easing.OutQuint)
                                    .RotateTo(0, 500, Easing.InOutElastic)
                                    .ScaleTo(Vector2.One, 500, Easing.InOutElastic);

                    latestTransformEndTime = perfectIndicator.LatestTransformEndTime;

                    perfectIndicator.FlashColour(Colour4.White, 1000, Easing.OutSine);
                }
            }

            using (BeginAbsoluteSequence(latestTransformEndTime))
            {
                personalBestIndicator.FadeIn(150, Easing.OutQuint)
                                     .ScaleTo(Vector2.One, 500, Easing.InOutElastic);
                personalBestFlash.FadeOutFromOne(1000, Easing.OutSine);

                latestTransformEndTime = personalBestIndicator.LatestTransformEndTime;
            }

            return latestTransformEndTime;
        }

        public void FinishAnimating()
        {
            scoreContainer.FinishTransforms();
            perfectIndicator.FinishTransforms();
            personalBestIndicator.FinishTransforms();
            personalBestFlash.FinishTransforms();
            totalScoreText.StopRolling();
        }

        public bool UsesFixedAnchor { get; set; }

        public partial class TotalScoreCounter : RollingCounter<long>
        {
            public const double ROLLING_DURATION = 3000;
            public const Easing ROLLING_EASING = Easing.OutPow10;

            protected override double RollingDuration => ROLLING_DURATION;

            protected override Easing RollingEasing => ROLLING_EASING;

            protected override IHasText CreateText()
            {
                return new OsuSpriteText
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    // TODO: classic scoring likely breaks this sizing. figure out later what to do with ultra large score numbers
                    Font = OsuFont.TorusAlternate.With(size: 120, weight: FontWeight.Light, fixedWidth: true),
                    Spacing = new Vector2(-5),
                    Colour = TEXT_GRADIENT,
                    UseFullGlyphHeight = false,
                    Margin = new MarginPadding { Top = 5, }, // `UseFullGlyphHeight` *almost* does the job to trim the glyph paddings, but it still can look offset because of decimal commas and such
                };
            }

            protected override LocalisableString FormatCount(long count) => count.ToString(@"N0");
        }
    }
}
