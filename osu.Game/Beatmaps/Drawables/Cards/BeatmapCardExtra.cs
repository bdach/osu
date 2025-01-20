// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.Drawables.Cards.Statistics;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Overlays.BeatmapSet;
using osuTK;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Beatmaps.Drawables.Cards
{
    public partial class BeatmapCardExtra : BeatmapCard
    {
        protected override Drawable IdleContent => idleBottomContent;
        protected override Drawable DownloadInProgressContent => downloadProgressBar;

        private const float height = 112;

        [Cached]
        private readonly BeatmapCardContent content;

        private BeatmapCardThumbnail thumbnail = null!;
        private CollapsibleButtonContainer buttonContainer = null!;

        private GridContainer statisticsContainer = null!;

        private FillFlowContainer idleBottomContent = null!;
        private BeatmapCardDownloadProgressBar downloadProgressBar = null!;
        private TruncatingSpriteText titleText = null!;
        private TruncatingSpriteText artistText = null!;
        private VideoIconPill videoIcon = null!;
        private StoryboardIconPill storyboardIcon = null!;
        private SpotlightBeatmapBadge spotlightBadge = null!;
        private ExplicitContentBeatmapBadge explicitBadge = null!;
        private FeaturedArtistBeatmapBadge featuredArtistBadge = null!;
        private TruncatingSpriteText sourceText = null!;
        private LinkFlowContainer authorText = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public BeatmapCardExtra()
            : this(true)
        {
        }

        public BeatmapCardExtra(bool allowExpansion)
            : base(allowExpansion)
        {
            content = new BeatmapCardContent(height);
        }

        [BackgroundDependencyLoader(true)]
        private void load(BeatmapSetOverlay? beatmapSetOverlay)
        {
            Width = WIDTH;
            Height = height;

            FillFlowContainer leftIconArea = null!;
            FillFlowContainer titleBadgeArea = null!;
            GridContainer artistContainer = null!;

            Content.Child = content.With(c =>
            {
                c.MainContent = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        thumbnail = new BeatmapCardThumbnail
                        {
                            Name = @"Left (icon) area",
                            BeatmapSet = { BindTarget = BeatmapSet },
                            Size = new Vector2(height),
                            Padding = new MarginPadding { Right = CORNER_RADIUS },
                            Child = leftIconArea = new FillFlowContainer
                            {
                                Margin = new MarginPadding(4),
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(1)
                            }
                        },
                        buttonContainer = new CollapsibleButtonContainer
                        {
                            X = height - CORNER_RADIUS,
                            Width = WIDTH - height + CORNER_RADIUS,
                            BeatmapSet = { BindTarget = BeatmapSet },
                            FavouriteState = { BindTarget = FavouriteState },
                            ButtonsCollapsedWidth = CORNER_RADIUS,
                            ButtonsExpandedWidth = 24,
                            Children = new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Children = new Drawable[]
                                    {
                                        new GridContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            ColumnDimensions = new[]
                                            {
                                                new Dimension(),
                                                new Dimension(GridSizeMode.AutoSize)
                                            },
                                            RowDimensions = new[]
                                            {
                                                new Dimension(GridSizeMode.AutoSize)
                                            },
                                            Content = new[]
                                            {
                                                new Drawable[]
                                                {
                                                    titleText = new TruncatingSpriteText
                                                    {
                                                        Font = OsuFont.Default.With(size: 18f, weight: FontWeight.SemiBold),
                                                        RelativeSizeAxes = Axes.X,
                                                    },
                                                    titleBadgeArea = new FillFlowContainer
                                                    {
                                                        Anchor = Anchor.BottomRight,
                                                        Origin = Anchor.BottomRight,
                                                        AutoSizeAxes = Axes.Both,
                                                        Direction = FillDirection.Horizontal,
                                                    }
                                                }
                                            }
                                        },
                                        artistContainer = new GridContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            ColumnDimensions = new[]
                                            {
                                                new Dimension(),
                                                new Dimension(GridSizeMode.AutoSize)
                                            },
                                            RowDimensions = new[]
                                            {
                                                new Dimension(GridSizeMode.AutoSize)
                                            },
                                            Content = new[]
                                            {
                                                new[]
                                                {
                                                    artistText = new TruncatingSpriteText
                                                    {
                                                        Font = OsuFont.Default.With(size: 14f, weight: FontWeight.SemiBold),
                                                        RelativeSizeAxes = Axes.X,
                                                    },
                                                    Empty()
                                                },
                                            }
                                        },
                                        sourceText = new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Shadow = false,
                                            Font = OsuFont.GetFont(size: 11f, weight: FontWeight.SemiBold),
                                            Colour = colourProvider.Content2
                                        },
                                    }
                                },
                                new Container
                                {
                                    Name = @"Bottom content",
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    Children = new Drawable[]
                                    {
                                        idleBottomContent = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 2),
                                            AlwaysPresent = true,
                                            Children = new Drawable[]
                                            {
                                                authorText = new LinkFlowContainer(s =>
                                                {
                                                    s.Shadow = false;
                                                    s.Font = OsuFont.GetFont(size: 11f, weight: FontWeight.SemiBold);
                                                }).With(d =>
                                                {
                                                    d.AutoSizeAxes = Axes.Both;
                                                    d.Margin = new MarginPadding { Top = 1 };
                                                }),
                                                statisticsContainer = new GridContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    RowDimensions = new[]
                                                    {
                                                        new Dimension(GridSizeMode.AutoSize),
                                                        new Dimension(GridSizeMode.AutoSize)
                                                    },
                                                    ColumnDimensions = new[]
                                                    {
                                                        new Dimension(GridSizeMode.AutoSize),
                                                        new Dimension(GridSizeMode.AutoSize),
                                                        new Dimension()
                                                    },
                                                    Content = new[]
                                                    {
                                                        new Drawable[3],
                                                        new Drawable[3]
                                                    }
                                                },
                                                new BeatmapCardExtraInfoRow
                                                {
                                                    BeatmapSet = { BindTarget = BeatmapSet },
                                                }
                                            }
                                        },
                                        downloadProgressBar = new BeatmapCardDownloadProgressBar
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Height = 5,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            State = { BindTarget = DownloadState },
                                            Progress = { BindTarget = DownloadProgress }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
                c.ExpandedContent = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Horizontal = 8, Vertical = 10 },
                    Child = new BeatmapCardDifficultyList
                    {
                        BeatmapSet = { BindTarget = BeatmapSet },
                    }
                };
                c.Expanded.BindTarget = Expanded;
            });

            leftIconArea.Add(videoIcon = new VideoIconPill { IconSize = new Vector2(16) });
            leftIconArea.Add(storyboardIcon = new StoryboardIconPill { IconSize = new Vector2(16) });

            titleBadgeArea.Add(spotlightBadge = new SpotlightBeatmapBadge
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Margin = new MarginPadding { Left = 4 },
                Alpha = 0,
            });
            titleBadgeArea.Add(explicitBadge = new ExplicitContentBeatmapBadge
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Margin = new MarginPadding { Left = 4 },
                Alpha = 0,
            });
            artistContainer.Content[0][1] = featuredArtistBadge = new FeaturedArtistBeatmapBadge
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Margin = new MarginPadding { Left = 4 },
                Alpha = 0,
            };

            Content.Action = () => beatmapSetOverlay?.FetchAndShowBeatmapSet(BeatmapSet.Value.OnlineID);
        }

        private void createStatistics()
        {
            BeatmapCardStatistic withMargin(BeatmapCardStatistic original)
            {
                original.Margin = new MarginPadding { Right = 8 };
                return original;
            }

            statisticsContainer.Content[0][0] = withMargin(new FavouritesStatistic(BeatmapSet.Value)
            {
                Current = FavouriteState,
            });

            statisticsContainer.Content[1][0] = withMargin(new PlayCountStatistic(BeatmapSet.Value));

            var hypesStatistic = HypesStatistic.CreateFor(BeatmapSet.Value);
            statisticsContainer.Content[0][1] = hypesStatistic != null ? withMargin(hypesStatistic) : Empty();

            var nominationsStatistic = NominationsStatistic.CreateFor(BeatmapSet.Value);
            statisticsContainer.Content[1][1] = nominationsStatistic != null ? withMargin(nominationsStatistic) : Empty();

            var dateStatistic = BeatmapCardDateStatistic.CreateFor(BeatmapSet.Value);
            statisticsContainer.Content[0][2] = dateStatistic != null ? withMargin(dateStatistic) : Empty();
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            bool showDetails = IsHovered || Expanded.Value;

            buttonContainer.ShowDetails.Value = showDetails;
            thumbnail.Dimmed.Value = showDetails;
        }

        protected override void UpdateBeatmapSet()
        {
            base.UpdateBeatmapSet();

            titleText.Text = new RomanisableString(BeatmapSet.Value.TitleUnicode, BeatmapSet.Value.Title);
            var romanisableArtist = new RomanisableString(BeatmapSet.Value.ArtistUnicode, BeatmapSet.Value.Artist);
            artistText.Text = BeatmapsetsStrings.ShowDetailsByArtist(romanisableArtist);
            sourceText.Text = BeatmapSet.Value.Source;
            authorText.Clear();
            authorText.AddText("mapped by ", t => t.Colour = colourProvider.Content2);
            authorText.AddUserLink(BeatmapSet.Value.Author);
            videoIcon.Alpha = BeatmapSet.Value.HasVideo ? 1 : 0;
            storyboardIcon.Alpha = BeatmapSet.Value.HasStoryboard ? 1 : 0;
            spotlightBadge.Alpha = BeatmapSet.Value.FeaturedInSpotlight ? 1 : 0;
            explicitBadge.Alpha = BeatmapSet.Value.HasExplicitContent ? 1 : 0;
            featuredArtistBadge.Alpha = BeatmapSet.Value.TrackId != null ? 1 : 0;
            createStatistics();
        }
    }
}
