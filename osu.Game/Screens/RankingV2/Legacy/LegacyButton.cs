// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Scoring;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.RankingV2.Legacy
{
    // TODO: action, disabled state, yadda yadda
    public abstract partial class LegacyButton : CompositeDrawable, ISerialisableDrawable
    {
        private NineSliceSprite backgroundSprite = null!;

        public Colour4 AccentColour { get; init; }

        public LocalisableString Text { get; init; }

        public Action? Action { get; set; }

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin)
        {
            InternalChildren =
            [
                backgroundSprite = new NineSliceSprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = skin.GetTexture(@"button"),
                    TextureInset = new MarginPadding { Horizontal = 16, },
                },
                new OsuSpriteText
                {
                    Text = Text,
                    Font = OsuFont.Default.With(size: 14 * Height / 18),
                    UseFullGlyphHeight = false,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            ];
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            updateState(false);
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            // todo: hack workarounds for `ReceivePositionalInputAt()` hacks so the button can receive scrolls from all over the screen
            // its all bad, fix it later
            updateState(Contains(e.ScreenSpaceMousePosition));
            return true;
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (!Contains(e.ScreenSpaceMousePosition))
                return false;

            Action?.Invoke();
            backgroundSprite.FlashColour(Colour4.White, 400);
            return true;
        }

        private void updateState(bool hovered)
        {
            var targetColour = AccentColour;
            const float unhovered_reduction = 20 / 255f;

            if (!hovered)
            {
                targetColour = new Colour4(
                    MathF.Max(0, targetColour.R - unhovered_reduction),
                    MathF.Max(0, targetColour.G - unhovered_reduction),
                    MathF.Max(0, targetColour.B - unhovered_reduction),
                    targetColour.A);
            }

            backgroundSprite.Colour = targetColour;
        }

        public bool UsesFixedAnchor { get; set; }
    }

    public partial class LegacyOnlineRankingButton : LegacyButton
    {
        public LegacyOnlineRankingButton()
        {
            AccentColour = Colour4.BlueViolet;
            Text = "▼ Online Ranking ▼";
            Size = new Vector2(200, 30) * LegacySkin.STABLE_MAGIC_SCALE_FACTOR;
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

        [Resolved]
        private ResultsScreenV2? results { get; set; }

        private bool transitionCommitted;
        private readonly Bindable<Visibility> detailsVisibility = new Bindable<Visibility>();

        [BackgroundDependencyLoader]
        private void load(IBindable<IScoreInfo> _)
        {
            // just a dummy BDL that requires `IBindable<IScoreInfo>`
            // this is done so that this component doesn't show up on other skinnable screens

            Action = commitTransitionToDetails;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (results != null)
            {
                detailsVisibility.BindTo(results.DetailsVisible);
                detailsVisibility.BindValueChanged(visible =>
                {
                    // this workaround ensures the scroll flow works correctly if the details are hidden in some way invisible to this component
                    if (visible.NewValue == Visibility.Hidden)
                    {
                        transitionCommitted = false;
                        scrollDelta = null;
                    }
                });
            }
        }

        private Vector2? scrollDelta;
        private ScheduledDelegate? cancelScroll;

        protected override bool OnScroll(ScrollEvent e)
        {
            scrollDelta = (scrollDelta ?? Vector2.Zero) + e.ScrollDelta;

            cancelScroll?.Cancel();

            if (scrollDelta.Value.Y > 0)
            {
                cancelTransitionToDetails();
                return true;
            }

            if (scrollDelta.Value.Y == 0 || transitionCommitted)
                return false;

            if (scrollDelta.Value.Y < -15)
            {
                commitTransitionToDetails();
                return true;
            }

            startTransitionToDetails(new Vector2(0, scrollDelta.Value.Y * 20));
            cancelScroll = Scheduler.AddDelayed(cancelTransitionToDetails, 500);
            return true;
        }

        private void startTransitionToDetails(Vector2 delta)
        {
            results?.PopInDetails((main, details) =>
            {
                main.RelativePositionAxes = Axes.None;
                main.MoveTo(new Vector2(0, delta.Y));

                details.Alpha = 1;
                details.RelativePositionAxes = Axes.None;
                details.Anchor = Anchor.BottomCentre;
                details.Origin = Anchor.TopCentre;
                details.FadeIn()
                       .MoveTo(new Vector2(0, delta.Y));
            });
        }

        private void commitTransitionToDetails()
        {
            results?.PopInDetails((main, details) =>
            {
                main.RelativePositionAxes = Axes.Y;
                main.MoveTo(new Vector2(0, -1), 1000, Easing.OutQuint)
                    .Then()
                    .FadeOut();

                details.Anchor = Anchor.BottomCentre;
                details.Origin = Anchor.TopCentre;
                details.RelativePositionAxes = Axes.Y;
                details.FadeIn()
                       .Then()
                       .MoveTo(new Vector2(0, -1), 1000, Easing.OutQuint);

                transitionCommitted = true;
                scrollDelta = null;
            });
        }

        private void cancelTransitionToDetails()
        {
            results?.PopOutDetails((main, details) =>
            {
                main.FadeIn()
                    .MoveTo(Vector2.Zero, 1000, Easing.OutQuint);

                details.MoveTo(Vector2.Zero, 1000, Easing.OutQuint)
                       .Then().FadeOut();

                transitionCommitted = false;
                scrollDelta = null;
            });
        }
    }
}
