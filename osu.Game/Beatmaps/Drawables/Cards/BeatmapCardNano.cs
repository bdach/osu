// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Resources.Localisation.Web;
using osuTK;

namespace osu.Game.Beatmaps.Drawables.Cards
{
    public partial class BeatmapCardNano : BeatmapCard
    {
        protected override Drawable IdleContent => idleBottomContent;
        protected override Drawable DownloadInProgressContent => downloadProgressBar;

        public override float Width
        {
            get => base.Width;
            set
            {
                base.Width = value;

                if (LoadState >= LoadState.Ready)
                    buttonContainer.Width = value;
            }
        }

        private const float height = 60;
        private const float width = 300;

        [Cached]
        private readonly BeatmapCardContent content;

        private CollapsibleButtonContainer buttonContainer = null!;

        private FillFlowContainer idleBottomContent = null!;
        private BeatmapCardDownloadProgressBar downloadProgressBar = null!;

        private TruncatingSpriteText titleText = null!;
        private TruncatingSpriteText artistText = null!;
        private LinkFlowContainer mapperText = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public BeatmapCardNano()
            : base(false)
        {
            content = new BeatmapCardContent(height);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Width = width;
            Height = height;

            Content.Child = content.With(c =>
            {
                c.MainContent = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = height,
                    Children = new Drawable[]
                    {
                        buttonContainer = new CollapsibleButtonContainer
                        {
                            Width = Width,
                            BeatmapSet = { BindTarget = BeatmapSet },
                            FavouriteState = { BindTarget = FavouriteState },
                            ButtonsCollapsedWidth = 5,
                            ButtonsExpandedWidth = 30,
                            Children = new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Children = new Drawable[]
                                    {
                                        titleText = new TruncatingSpriteText
                                        {
                                            Font = OsuFont.Default.With(size: 19, weight: FontWeight.SemiBold),
                                            RelativeSizeAxes = Axes.X,
                                        },
                                        artistText = new TruncatingSpriteText
                                        {
                                            Font = OsuFont.Default.With(size: 16, weight: FontWeight.SemiBold),
                                            RelativeSizeAxes = Axes.X,
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
                                            Spacing = new Vector2(0, 3),
                                            AlwaysPresent = true,
                                            Children = new Drawable[]
                                            {
                                                mapperText = new LinkFlowContainer(s =>
                                                {
                                                    s.Shadow = false;
                                                    s.Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold);
                                                }).With(d =>
                                                {
                                                    d.AutoSizeAxes = Axes.Both;
                                                    d.Margin = new MarginPadding { Top = 2 };
                                                }),
                                            }
                                        },
                                        downloadProgressBar = new BeatmapCardDownloadProgressBar
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Height = 6,
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
                    Padding = new MarginPadding { Horizontal = 10, Vertical = 13 },
                    Child = new BeatmapCardDifficultyList
                    {
                        BeatmapSet = { BindTarget = BeatmapSet }
                    }
                };
                c.Expanded.BindTarget = Expanded;
            });
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            bool showDetails = IsHovered;

            buttonContainer.ShowDetails.Value = showDetails;
        }

        protected override void UpdateBeatmapSet()
        {
            base.UpdateBeatmapSet();

            titleText.Text = new RomanisableString(BeatmapSet.Value.TitleUnicode, BeatmapSet.Value.Title);

            var romanisableArtist = new RomanisableString(BeatmapSet.Value.ArtistUnicode, BeatmapSet.Value.Artist);
            artistText.Text = BeatmapsetsStrings.ShowDetailsByArtist(romanisableArtist);

            mapperText.Clear();
            mapperText.AddText("mapped by ", t => t.Colour = colourProvider.Content2);
            mapperText.AddUserLink(BeatmapSet.Value.Author);
        }
    }
}
