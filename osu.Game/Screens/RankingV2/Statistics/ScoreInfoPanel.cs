// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play.HUD;
using osu.Game.Screens.RankingV2.Argon;
using osu.Game.Skinning;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Screens.RankingV2.Statistics
{
    public partial class ScoreInfoPanel : CompositeDrawable
    {
        private Sprite rankSprite = null!;
        private OsuSpriteText totalScoreText = null!;
        private ModDisplay modDisplay = null!;
        private OsuSpriteText modMultiplierSpriteText = null!;
        private FillFlowContainer hitStatisticsFlow = null!;
        private FillFlowContainer leftDetails = null!;
        private FillFlowContainer rightDetails = null!;

        [Resolved]
        private IBindable<IScoreInfo> score { get; set; } = null!;

        [Resolved]
        private SkinManager skins { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, OsuColour colours)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Masking = true;
            CornerRadius = 20;

            InternalChildren =
            [
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children =
                    [
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            ColumnDimensions =
                            [
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension(),
                            ],
                            RowDimensions = [new Dimension(GridSizeMode.AutoSize)],
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(15),
                                        Padding = new MarginPadding
                                        {
                                            Vertical = 5,
                                            Left = 15,
                                        },
                                        Children =
                                        [
                                            new Container
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Size = new Vector2(50, 60),
                                                Child = rankSprite = new Sprite
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    FillMode = FillMode.Fit,
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                },
                                            },
                                            totalScoreText = new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Font = OsuFont.TorusAlternate.With(size: 54, weight: FontWeight.Light),
                                                Colour = ArgonTotalScorePanel.TEXT_GRADIENT,
                                                UseFullGlyphHeight = false,
                                            },
                                            new FillFlowContainer
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Direction = FillDirection.Vertical,
                                                Children =
                                                [
                                                    new OsuSpriteText
                                                    {
                                                        Text = "MODS",
                                                        Font = OsuFont.Default.With(weight: FontWeight.Bold),
                                                        Colour = colourProvider.Content2,
                                                    },
                                                    modDisplay = new ModDisplay
                                                    {
                                                        ExpansionMode = ExpansionMode.AlwaysContracted,
                                                        Scale = new Vector2(0.6f),
                                                    }
                                                ]
                                            },
                                            new FillFlowContainer
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Direction = FillDirection.Vertical,
                                                Children =
                                                [
                                                    new OsuSpriteText
                                                    {
                                                        Text = "MULTIPLIER",
                                                        Font = OsuFont.Default.With(weight: FontWeight.Bold),
                                                        Colour = colourProvider.Content2,
                                                    },
                                                    modMultiplierSpriteText = new OsuSpriteText
                                                    {
                                                        Font = OsuFont.Default.With(size: 28),
                                                        Colour = ColourInfo.GradientVertical(colours.Red0, colours.Red1),
                                                    },
                                                ]
                                            },
                                        ],
                                    },
                                    hitStatisticsFlow = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Spacing = new Vector2(15, 0),
                                        Padding = new MarginPadding
                                        {
                                            Vertical = 5,
                                            Right = 15,
                                        },
                                    },
                                },
                            },
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            CornerRadius = 20,
                            Masking = true,
                            Children =
                            [
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = colourProvider.Background5,
                                },
                                new GridContainer
                                {
                                    Padding = new MarginPadding(15),
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    ColumnDimensions =
                                    [
                                        new Dimension(GridSizeMode.Absolute, 350),
                                        new Dimension(GridSizeMode.Absolute, 10),
                                        new Dimension(),
                                    ],
                                    RowDimensions = [new Dimension(GridSizeMode.AutoSize)],
                                    Content = new[]
                                    {
                                        new Drawable[]
                                        {
                                            leftDetails = new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Spacing = new Vector2(10),
                                                Direction = FillDirection.Vertical,
                                                Children =
                                                [
                                                    new GlobalStatisticsUpdatePanel(),
                                                ]
                                            },
                                            rightDetails = new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Spacing = new Vector2(10),
                                                Direction = FillDirection.Full,
                                            },
                                        },
                                    }
                                }
                            ]
                        }
                    ],
                },
            ];
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            score.BindValueChanged(_ => updateState(), true);
        }

        private void updateState()
        {
            rankSprite.Texture = skins.DefaultClassicSkin.GetTexture(DrawableRank.GetLegacyRankTextureName(score.Value.Rank));
            totalScoreText.Text = score.Value.TotalScore.ToString(@"N0"); // TODO make classic scoring work

            var rulesetInstance = score.Value.Ruleset.CreateInstance();
            var scoreMultiplierCalculator = rulesetInstance.CreateScoreMultiplierCalculator(new ScoreMultiplierContext(score.Value.Beatmap!.Difficulty));
            var mods = score.Value.Mods.Select(m => m.ToMod(rulesetInstance)).ToArray();
            modDisplay.Current.Value = mods;
            modMultiplierSpriteText.Text = ModUtils.FormatScoreMultiplier(scoreMultiplierCalculator.CalculateFor(mods));

            hitStatisticsFlow.RemoveAll(d => d is StatisticValue, true);
            var statistics = score.Value.GetStatisticsForDisplay();

            foreach (var statistic in statistics.Reverse())
            {
                hitStatisticsFlow.Add(new StatisticValue
                {
                    AccentColour = statistic.Result.IsBasic() ? colours.ForHitResult(statistic.Result) : null,
                    Caption = statistic.DisplayName,
                    Value = statistic.Count.ToLocalisableString(@"N0"),
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                });
            }
        }
    }
}
