// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps.Drawables.Cards.Buttons;
using osu.Game.Overlays;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Online.API.Requests.Responses;
using osuTK;

namespace osu.Game.Beatmaps.Drawables.Cards
{
    public partial class BeatmapCardThumbnail : Container
    {
        public Bindable<APIBeatmapSet> BeatmapSet { get; } = new Bindable<APIBeatmapSet>();
        public BindableBool Dimmed { get; } = new BindableBool();

        public new MarginPadding Padding
        {
            get => foreground.Padding;
            set => foreground.Padding = value;
        }

        private readonly UpdateableOnlineBeatmapSetCover cover;
        private readonly Box background;
        private readonly Container foreground;
        private readonly PlayButton playButton;
        private readonly CircularProgress progress;
        private Container content;

        protected override Container<Drawable> Content => content;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public BeatmapCardThumbnail()
        {
            InternalChildren = new Drawable[]
            {
                cover = new UpdateableOnlineBeatmapSetCover(BeatmapSetCoverType.List)
                {
                    RelativeSizeAxes = Axes.Both,
                },
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                foreground = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        playButton = new PlayButton
                        {
                            BeatmapSet = { BindTarget = BeatmapSet },
                            RelativeSizeAxes = Axes.Both
                        },
                        progress = new CircularProgress
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            InnerRadius = 0.2f
                        },
                        content = new Container
                        {
                            RelativeSizeAxes = Axes.Both
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            progress.Colour = colourProvider.Highlight1;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Dimmed.BindValueChanged(_ => updateState());

            BeatmapSet.BindValueChanged(_ => cover.OnlineInfo = BeatmapSet.Value, true);
            playButton.Playing.BindValueChanged(_ => updateState(), true);
            FinishTransforms(true);
        }

        protected override void Update()
        {
            base.Update();

            progress.Progress = playButton.Progress.Value;
            progress.Size = new Vector2(50 * playButton.DrawWidth / (BeatmapCardNormal.HEIGHT - BeatmapCard.CORNER_RADIUS));
        }

        private void updateState()
        {
            bool shouldDim = Dimmed.Value || playButton.Playing.Value;

            playButton.FadeTo(shouldDim ? 1 : 0, BeatmapCard.TRANSITION_DURATION, Easing.OutQuint);
            background.FadeColour(colourProvider.Background6.Opacity(shouldDim ? 0.6f : 0f), BeatmapCard.TRANSITION_DURATION, Easing.OutQuint);
        }
    }
}
