// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Game.Online.API.Requests.Responses;
using osuTK;

namespace osu.Game.Beatmaps.Drawables.Cards
{
    public partial class BeatmapCardExtraInfoRow : CompositeDrawable
    {
        public Bindable<APIBeatmapSet> BeatmapSet { get; } = new Bindable<APIBeatmapSet>();

        [Resolved(CanBeNull = true)]
        private BeatmapCardContent? content { get; set; }

        private FillFlowContainer flow = null!;
        private BeatmapSetOnlineStatusPill statusPill = null!;
        private DifficultySpectrumDisplay? spectrumDisplay;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChild = flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(3, 0),
                Children = new Drawable[]
                {
                    statusPill = new BeatmapSetOnlineStatusPill
                    {
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        TextSize = 13f
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            BeatmapSet.BindValueChanged(_ =>
            {
                statusPill.Status = BeatmapSet.Value.Status;
                spectrumDisplay?.Expire();
                flow.Add(spectrumDisplay = new DifficultySpectrumDisplay(BeatmapSet.Value)
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    DotSize = new Vector2(5, 10)
                });
            }, true);
        }

        protected override bool OnHover(HoverEvent e)
        {
            content?.ExpandAfterDelay();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            if (content?.Expanded.Value == false)
                content.CancelExpand();

            base.OnHoverLost(e);
        }
    }
}
