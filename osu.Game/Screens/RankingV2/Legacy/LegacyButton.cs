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

            updateState();
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateState();
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateState();
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            Action?.Invoke();
            backgroundSprite.FlashColour(Colour4.White, 400);
            return true;
        }

        private void updateState()
        {
            var targetColour = AccentColour;
            const float unhovered_reduction = 20 / 255f;

            if (!IsHovered)
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

        [BackgroundDependencyLoader]
        private void load(IBindable<IScoreInfo> _)
        {
            // just a dummy BDL that requires `IBindable<IScoreInfo>`
            // this is done so that this component doesn't show up on other skinnable screens

            Action = () =>
            {
                results?.PopInDetails(details =>
                {
                    details.Anchor = Anchor.TopLeft;
                    details.Origin = Anchor.TopLeft;
                    details.RelativePositionAxes = Axes.Both;
                    details.FadeIn()
                           .MoveTo(new Vector2(0, 1))
                           .Then()
                           .MoveTo(Vector2.Zero, 1000, Easing.OutQuint);
                });
            };
        }

        private Vector2? scrollDelta;
        private ScheduledDelegate? cancelScroll;

        protected override bool OnScroll(ScrollEvent e)
        {
            scrollDelta = (scrollDelta ?? Vector2.Zero) + e.ScrollDelta;

            if (scrollDelta.Value.Y > 0)
            {
                cancelTransitionToDetails();
                return true;
            }

            cancelScroll?.Cancel();

            if (scrollDelta.Value.Y < -15)
            {
                commitTransitionToDetails();
                return true;
            }

            startTransitionToDetails(new Vector2(0, scrollDelta.Value.Y * 20));
            cancelScroll = Scheduler.AddDelayed(() =>
            {
                cancelTransitionToDetails();
                scrollDelta = null;
            }, 500);
            return true;
        }

        private void startTransitionToDetails(Vector2 delta)
        {
            Margin = new MarginPadding { Bottom = Math.Max(-delta.Y, 0) };
            results?.PopInDetails(details =>
            {
                details.Alpha = 1;
                details.RelativePositionAxes = Axes.None;
                details.Anchor = Anchor.BottomCentre;
                details.Origin = Anchor.TopCentre;
                details.Position = new Vector2(0, delta.Y);
            });
        }

        private void commitTransitionToDetails()
        {
            this.TransformTo(nameof(Margin), new MarginPadding(), 1000, Easing.OutQuint);
            results?.PopInDetails(details =>
            {
                details.RelativePositionAxes = Axes.Y;
                details.MoveTo(new Vector2(0, -1), 1000, Easing.OutQuint);
            });
        }

        private void cancelTransitionToDetails()
        {
            results?.PopOutDetails(details =>
            {
                details.MoveTo(Vector2.Zero, 1000, Easing.OutQuint)
                       .Then().FadeOut();
            });
        }
    }
}
