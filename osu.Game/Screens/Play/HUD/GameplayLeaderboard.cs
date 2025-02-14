// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Containers;
using osu.Game.Screens.Select.Leaderboards;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Play.HUD
{
    public partial class GameplayLeaderboard : CompositeDrawable, ISerialisableDrawable
    {
        private readonly Cached sorting = new Cached();

        public Bindable<bool> Expanded = new Bindable<bool>();

        protected readonly FillFlowContainer<GameplayLeaderboardScore> Flow;

        private bool requiresScroll;
        private readonly OsuScrollContainer scroll;

        public GameplayLeaderboardScore? TrackedScore { get; private set; }

        [Resolved]
        private IGameplayLeaderboardProvider? leaderboardProvider { get; set; }

        private readonly IBindableList<ILeaderboardScore> scores = new BindableList<ILeaderboardScore>();

        private const int max_panels = 8;

        /// <summary>
        /// Create a new leaderboard.
        /// </summary>
        public GameplayLeaderboard()
        {
            Width = GameplayLeaderboardScore.EXTENDED_WIDTH + GameplayLeaderboardScore.SHEAR_WIDTH;

            InternalChildren = new Drawable[]
            {
                scroll = new InputDisabledScrollContainer
                {
                    ClampExtension = 0,
                    RelativeSizeAxes = Axes.Both,
                    Child = Flow = new FillFlowContainer<GameplayLeaderboardScore>
                    {
                        RelativeSizeAxes = Axes.X,
                        X = GameplayLeaderboardScore.SHEAR_WIDTH,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(2.5f),
                        LayoutDuration = 450,
                        LayoutEasing = Easing.OutQuint,
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (leaderboardProvider != null)
            {
                scores.BindTo(leaderboardProvider.Scores);
                scores.BindCollectionChanged((_, _) =>
                {
                    Clear();
                    foreach (var score in scores)
                        Add(score);
                }, true);
            }

            Scheduler.AddDelayed(sort, 1000, true);
        }

        /// <summary>
        /// Adds a player to the leaderboard.
        /// </summary>
        public void Add(ILeaderboardScore score)
        {
            var drawable = CreateLeaderboardScoreDrawable(score);

            if (score.Tracked)
            {
                if (TrackedScore != null)
                    throw new InvalidOperationException("Cannot track more than one score.");

                TrackedScore = drawable;
            }

            drawable.Expanded.BindTo(Expanded);

            Flow.Add(drawable);
            drawable.TotalScore.BindValueChanged(_ => sorting.Invalidate(), true);
            drawable.DisplayOrder.BindValueChanged(_ => sorting.Invalidate(), true);

            int displayCount = Math.Min(Flow.Count, max_panels);
            Height = displayCount * (GameplayLeaderboardScore.PANEL_HEIGHT + Flow.Spacing.Y);
            requiresScroll = displayCount != Flow.Count;
        }

        public void Clear()
        {
            Flow.Clear();
            TrackedScore = null;
            scroll.ScrollToStart(false);
        }

        protected virtual GameplayLeaderboardScore CreateLeaderboardScoreDrawable(ILeaderboardScore score) =>
            new GameplayLeaderboardScore(score);

        protected override void Update()
        {
            base.Update();

            if (requiresScroll && TrackedScore != null)
            {
                double scrollTarget = scroll.GetChildPosInContent(TrackedScore) + TrackedScore.DrawHeight / 2 - scroll.DrawHeight / 2;

                scroll.ScrollTo(scrollTarget);
            }

            const float panel_height = GameplayLeaderboardScore.PANEL_HEIGHT;

            float fadeBottom = (float)(scroll.Current + scroll.DrawHeight);
            float fadeTop = (float)(scroll.Current + panel_height);

            if (scroll.IsScrolledToStart()) fadeTop -= panel_height;
            if (!scroll.IsScrolledToEnd()) fadeBottom -= panel_height;

            // logic is mostly shared with Leaderboard, copied here for simplicity.
            foreach (var c in Flow)
            {
                float topY = c.ToSpaceOfOtherDrawable(Vector2.Zero, Flow).Y;
                float bottomY = topY + panel_height;

                bool requireTopFade = requiresScroll && topY <= fadeTop;
                bool requireBottomFade = requiresScroll && bottomY >= fadeBottom;

                if (!requireTopFade && !requireBottomFade)
                    c.Colour = Color4.White;
                else if (topY > fadeBottom + panel_height || bottomY < fadeTop - panel_height)
                    c.Colour = Color4.Transparent;
                else
                {
                    if (requireBottomFade)
                    {
                        c.Colour = ColourInfo.GradientVertical(
                            Color4.White.Opacity(Math.Min(1 - (topY - fadeBottom) / panel_height, 1)),
                            Color4.White.Opacity(Math.Min(1 - (bottomY - fadeBottom) / panel_height, 1)));
                    }
                    else if (requiresScroll)
                    {
                        c.Colour = ColourInfo.GradientVertical(
                            Color4.White.Opacity(Math.Min(1 - (fadeTop - topY) / panel_height, 1)),
                            Color4.White.Opacity(Math.Min(1 - (fadeTop - bottomY) / panel_height, 1)));
                    }
                }
            }
        }

        private void sort()
        {
            if (sorting.IsValid)
                return;

            var orderedByScore = Flow
                                 .OrderByDescending(i => i.TotalScore.Value)
                                 .ThenBy(i => i.DisplayOrder.Value)
                                 .ToList();

            for (int i = 0; i < Flow.Count; i++)
            {
                Flow.SetLayoutPosition(orderedByScore[i], i);
                orderedByScore[i].ScorePosition = i + 1 == Flow.Count && leaderboardProvider?.IsPartial == true ? null : i + 1;
            }

            sorting.Validate();
        }

        private partial class InputDisabledScrollContainer : OsuScrollContainer
        {
            public InputDisabledScrollContainer()
            {
                ScrollbarVisible = false;
            }

            public override bool HandlePositionalInput => false;
            public override bool HandleNonPositionalInput => false;
        }

        public bool UsesFixedAnchor { get; set; }
    }
}
