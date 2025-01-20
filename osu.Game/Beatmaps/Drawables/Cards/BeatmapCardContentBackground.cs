// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;

namespace osu.Game.Beatmaps.Drawables.Cards
{
    public partial class BeatmapCardContentBackground : CompositeDrawable
    {
        public Bindable<APIBeatmapSet> BeatmapSet { get; } = new Bindable<APIBeatmapSet>();
        public BindableBool Dimmed { get; } = new BindableBool();

        private readonly Box background;
        private DelayedLoadUnloadWrapper? cover;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public BeatmapCardContentBackground()
        {
            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
            };
        }

        private static Drawable createCover(IBeatmapSetOnlineInfo onlineInfo) => new OnlineBeatmapSetCover(onlineInfo)
        {
            RelativeSizeAxes = Axes.Both,
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            FillMode = FillMode.Fill
        };

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            background.Colour = colourProvider.Background2;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            BeatmapSet.BindValueChanged(_ =>
            {
                cover?.Expire();
                AddInternal(cover = new DelayedLoadUnloadWrapper(() => createCover(BeatmapSet.Value), 500, 500)
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Transparent
                });
            }, true);
            Dimmed.BindValueChanged(_ => updateState(), true);
            FinishTransforms(true);
        }

        private void updateState() => Schedule(() =>
        {
            background.FadeColour(Dimmed.Value ? colourProvider.Background4 : colourProvider.Background2, BeatmapCard.TRANSITION_DURATION, Easing.OutQuint);

            var gradient = ColourInfo.GradientHorizontal(Colour4.White.Opacity(0), Colour4.White.Opacity(0.2f));
            cover.FadeColour(gradient, BeatmapCard.TRANSITION_DURATION, Easing.OutQuint);
        });
    }
}
