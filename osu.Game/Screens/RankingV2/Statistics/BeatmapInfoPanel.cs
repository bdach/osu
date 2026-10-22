// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Scoring;
using osu.Game.Screens.Select;
using osuTK;

namespace osu.Game.Screens.RankingV2.Statistics
{
    public partial class BeatmapInfoPanel : CompositeDrawable
    {
        private UpdateableBeatmapBackgroundSprite beatmapCover = null!;
        private BeatmapSetOnlineStatusPill statusPill = null!;
        private TruncatingSpriteText titleText = null!;
        private TruncatingSpriteText authorText = null!;
        private StarRatingDisplay starRatingDisplay = null!;
        private OsuTextFlowContainer difficultyNameFlow = null!;
        private FillFlowContainer statisticsFlow = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private IBindable<IScoreInfo> score { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Masking = true;
            CornerRadius = 20;

            InternalChildren =
            [
                beatmapCover = new UpdateableBeatmapBackgroundSprite
                {
                    RelativeSizeAxes = Axes.Both,
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding(5),
                    ColumnDimensions =
                    [
                        new Dimension(),
                        new Dimension(GridSizeMode.AutoSize),
                    ],
                    RowDimensions = [new Dimension(GridSizeMode.AutoSize)],
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Padding = new MarginPadding
                                {
                                    Left = 15,
                                    Vertical = 5,
                                },
                                Children =
                                [
                                    statusPill = new BeatmapSetOnlineStatusPill(),
                                    titleText = new TruncatingSpriteText
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Font = OsuFont.Style.Title,
                                    },
                                    authorText = new TruncatingSpriteText
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Font = OsuFont.Style.Heading2,
                                    },
                                ],
                            },
                            new Container
                            {
                                AutoSizeAxes = Axes.X,
                                RelativeSizeAxes = Axes.Y,
                                Masking = true,
                                CornerRadius = 15,
                                Children =
                                [
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background6,
                                        Alpha = 0.8f,
                                    },
                                    new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.TopLeft,
                                        Origin = Anchor.TopLeft,
                                        Padding = new MarginPadding
                                        {
                                            Horizontal = 15,
                                            Vertical = 5,
                                        },
                                        Spacing = new Vector2(5),
                                        Children =
                                        [
                                            starRatingDisplay = new StarRatingDisplay(new StarDifficulty())
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                            },
                                            difficultyNameFlow = new OsuTextFlowContainer
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                            }
                                        ],
                                    },
                                    statisticsFlow = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.BottomLeft,
                                        Origin = Anchor.BottomLeft,
                                        Padding = new MarginPadding
                                        {
                                            Horizontal = 15,
                                            Vertical = 5,
                                        },
                                        Spacing = new Vector2(5),
                                    }
                                ],
                            },
                        },
                    },
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
            Debug.Assert(score.Value.Beatmap != null);

            beatmapCover.Beatmap.Value = score.Value.Beatmap;
            statusPill.Status = score.Value.Beatmap.Status;

            titleText.Text = new RomanisableString(score.Value.Beatmap.Metadata.TitleUnicode, score.Value.Beatmap.Metadata.Title);
            authorText.Text = new RomanisableString(score.Value.Beatmap.Metadata.ArtistUnicode, score.Value.Beatmap.Metadata.Artist);

            // TODO: make actually calculate
            starRatingDisplay.Current.Value = new StarDifficulty(score.Value.Beatmap.StarRating, 0);
            difficultyNameFlow.Clear();
            difficultyNameFlow.AddText(score.Value.Beatmap.DifficultyName, t => t.Font = OsuFont.Style.Heading2);
            difficultyNameFlow.AddText(" mapped by "); // TODO: localisation
            difficultyNameFlow.AddText(score.Value.Beatmap.Metadata.Author.Username, t => t.Font = OsuFont.Default.With(weight: FontWeight.SemiBold));

            // TODO: this is all terribly synchronous and should not be
            var playableBeatmap = beatmap.Value.GetPlayableBeatmap(score.Value.Ruleset);
            var rulesetInstance = score.Value.Ruleset.CreateInstance();
            var hitObjectCounts = playableBeatmap.GetStatistics()
                                                 .Select(s => new BeatmapTitleWedge.StatisticDifficulty
                                                 {
                                                     Width = 65, // TODO: this hardcoded sizing sucks make it better
                                                     Value = new BeatmapTitleWedge.StatisticDifficulty.Data(s.Name, s.BarDisplayLength ?? 0, s.BarDisplayLength ?? 0, 1, s.Content),
                                                 })
                                                 .ToList();
            var difficultyStats = rulesetInstance.GetBeatmapAttributesForDisplay(score.Value.Beatmap, score.Value.Mods.Select(m => m.ToMod(rulesetInstance)).ToArray())
                                                 .Select(s => new BeatmapTitleWedge.StatisticDifficulty
                                                 {
                                                     Width = 85, // TODO: this hardcoded sizing sucks make it better
                                                     Value = new BeatmapTitleWedge.StatisticDifficulty.Data(s),
                                                 });
            statisticsFlow.Clear();
            statisticsFlow.AddRange(hitObjectCounts);
            statisticsFlow.Add(Empty().With(sp => sp.Width = 30));
            statisticsFlow.AddRange(difficultyStats);
        }

        protected override void Update()
        {
            base.Update();

            difficultyNameFlow.Colour = starRatingDisplay.ForegroundTextColour;
            foreach (var statistic in statisticsFlow.OfType<BeatmapTitleWedge.StatisticDifficulty>())
                statistic.AccentColour = starRatingDisplay.ForegroundTextColour;
        }
    }
}
