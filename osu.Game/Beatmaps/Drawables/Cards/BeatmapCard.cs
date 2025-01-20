// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Localisation;

namespace osu.Game.Beatmaps.Drawables.Cards
{
    public abstract partial class BeatmapCard : PoolableDrawable, IHasContextMenu
    {
        public const float TRANSITION_DURATION = 340;
        public const float CORNER_RADIUS = 8;

        protected const float WIDTH = 345;

        public IBindable<bool> Expanded { get; }

        public Bindable<APIBeatmapSet> BeatmapSet { get; } = new Bindable<APIBeatmapSet>();

        protected BeatmapDownloadTracker? DownloadTracker { get; private set; }

        [Cached]
        protected IBindable<DownloadState> DownloadState { get; private set; } = new Bindable<DownloadState>();

        [Cached(Name = nameof(DownloadProgress))]
        protected IBindable<double> DownloadProgress { get; private set; } = new Bindable<double>();

        protected readonly Bindable<BeatmapSetFavouriteState> FavouriteState = new Bindable<BeatmapSetFavouriteState>();

        protected abstract Drawable IdleContent { get; }
        protected abstract Drawable DownloadInProgressContent { get; }

        protected OsuClickableContainer Content { get; }

        [Resolved]
        private BeatmapSetOverlay? beatmapSetOverlay { get; set; }

        protected BeatmapCard(bool allowExpansion = true)
        {
            Content = new OsuClickableContainer(HoverSampleSet.Button)
            {
                RelativeSizeAxes = Axes.Both,
            };
            Expanded = new BindableBool { Disabled = !allowExpansion };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(Content);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            BeatmapSet.BindValueChanged(_ => UpdateBeatmapSet(), true);

            Expanded.BindValueChanged(_ => UpdateState(), true);
            FinishTransforms(true);
        }

        protected virtual void UpdateBeatmapSet()
        {
            FavouriteState.Value = new BeatmapSetFavouriteState(BeatmapSet.Value.HasFavourited, BeatmapSet.Value.FavouriteCount);
            Content.Action = () => beatmapSetOverlay?.FetchAndShowBeatmapSet(BeatmapSet.Value.OnlineID);

            DownloadState.UnbindBindings();
            DownloadProgress.UnbindBindings();

            DownloadTracker?.RemoveAndDisposeImmediately();
            DownloadTracker = new BeatmapDownloadTracker(BeatmapSet.Value);
            AddInternal(DownloadTracker);

            DownloadState.BindTo(DownloadTracker.State);
            DownloadProgress.BindTo(DownloadTracker.Progress);
        }

        protected override bool OnHover(HoverEvent e)
        {
            UpdateState();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            UpdateState();
            base.OnHoverLost(e);
        }

        protected virtual void UpdateState()
        {
            bool showProgress = DownloadState.Value == Online.DownloadState.Downloading || DownloadState.Value == Online.DownloadState.Importing;

            IdleContent.FadeTo(showProgress ? 0 : 1, TRANSITION_DURATION, Easing.OutQuint);
            DownloadInProgressContent.FadeTo(showProgress ? 1 : 0, TRANSITION_DURATION, Easing.OutQuint);
        }

        public MenuItem[] ContextMenuItems => new MenuItem[]
        {
            new OsuMenuItem(ContextMenuStrings.ViewBeatmap, MenuItemType.Highlighted, Content.Action),
        };
    }
}
