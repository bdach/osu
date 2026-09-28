// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Threading;
using osu.Game.Graphics;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Scoring;
using osu.Game.Screens.Footer;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.RankingV2.Argon
{
    public partial class GradeDisplay : CompositeDrawable, ISerialisableDrawable
    {
        public bool CanBePlaced(SkinnableContainer skinnableContainer) => !skinnableContainer.Components.OfType<GradeDisplay>().Any();
        public bool CanBeMoved => false;
        public bool CanBeRotated => false;
        public bool CanBeScaled => false;

        private Container gradedCirclesContainer = null!;
        private Sprite rankSprite = null!;

        private Drawable glowLayer = null!;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

        [Resolved]
        private IBindable<IScoreInfo> score { get; set; } = null!;

        [Resolved]
        private SkinManager skinManager { get; set; } = null!;

        [Resolved]
        private ResultsScreenV2? results { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            AutoSizeAxes = Axes.Both;
            Anchor = Anchor.CentreRight;
            Origin = Anchor.CentreRight;
            X = 100;
            Y = -ScreenFooter.HEIGHT / 2f;
            Masking = true;

            InternalChildren =
            [
                new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 2,
                    Masking = true,
                    EdgeEffect = BeatmapInfoWedge.CreateShadowEdgeEffect(),
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourInfo.GradientHorizontal(
                            colourProvider.Background4.Opacity(0.99f),
                            colourProvider.Background4.Opacity(0.9f)
                        ),
                    },
                },
                new Container
                {
                    Size = new Vector2(600),
                    Margin = new MarginPadding(20),
                    Children =
                    [
                        gradedCirclesContainer = new CircularContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            EdgeEffect = BeatmapInfoWedge.CreateShadowEdgeEffect()
                        },
                        rankSprite = new Sprite
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.7f),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            FillMode = FillMode.Fit,
                        }
                    ]
                },
                glowLayer = new Box
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.5f,
                    Height = 2,
                    Blending = BlendingParameters.Additive,
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
            var scoreProcessor = score.Value.Ruleset.CreateInstance().CreateScoreProcessor();

            gradedCirclesContainer.Child = new GradedCirclesV2(scoreProcessor)
            {
                RelativeSizeAxes = Axes.Both,
                Progress = score.Value.Accuracy,
            };

            glowLayer.Colour = ColourInfo.GradientHorizontal(
                OsuColour.ForRank(score.Value.Rank).Opacity(0.0f),
                OsuColour.ForRank(score.Value.Rank).Opacity(0.3f)
            );

            // glowContainer.EdgeEffect = new EdgeEffectParameters
            // {
            //     // in the design this was an inner glow, but framework can't do that, and this kind of has... more sauce anyway?
            //     Type = EdgeEffectType.Glow,
            //     // adjust opacity depending on rank type maybe? or just add more flair in a different way.
            //     Colour = OsuColour.ForRank(score.Value.Rank).Opacity(0.05f),
            //     Radius = 50,
            // };

            rankSprite.Texture = skinManager.DefaultClassicSkin.GetTexture(DrawableRank.GetLegacyRankTextureName(score.Value.Rank));
        }

        private Vector2? dragDelta;

        private Vector2? scrollDelta;
        private ScheduledDelegate? cancelScroll;

        protected override bool OnDragStart(DragStartEvent e)
        {
            FinishTransforms(false, nameof(Margin));
            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            base.OnDrag(e);
            dragDelta = e.MousePosition - e.MouseDownPosition;
            startTransitionToDetails(dragDelta.Value);
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            base.OnDragEnd(e);

            if (dragDelta?.X < -200)
                commitTransitionToDetails();
            else
                cancelTransitionToDetails();
        }

        protected override bool OnScroll(ScrollEvent e)
        {
            scrollDelta = (scrollDelta ?? Vector2.Zero) + e.ScrollDelta;

            cancelScroll?.Cancel();

            if (scrollDelta.Value.X < 0)
            {
                cancelTransitionToDetails();
                return true;
            }

            if (scrollDelta.Value.X == 0)
                return false;

            if (scrollDelta.Value.X > 15)
            {
                commitTransitionToDetails();
                return true;
            }

            startTransitionToDetails(new Vector2(-scrollDelta.Value.X * 20, 0));
            cancelScroll = Scheduler.AddDelayed(() =>
            {
                cancelTransitionToDetails();
                scrollDelta = null;
            }, 500);
            return true;
        }

        private void startTransitionToDetails(Vector2 delta)
        {
            FinishTransforms(false, nameof(Margin));
            Margin = new MarginPadding { Right = -Math.Min(delta.X, 0) };
            results?.PopInDetails(details =>
            {
                details.Alpha = 1;
                details.RelativePositionAxes = Axes.None;
                details.Anchor = Anchor.CentreRight;
                details.Origin = Anchor.CentreLeft;
                details.Position = new Vector2(delta.X, 0);
            });
        }

        private void commitTransitionToDetails()
        {
            this.TransformTo(nameof(Margin), new MarginPadding(), 1000, Easing.OutQuint);
            results?.PopInDetails(details =>
            {
                details.RelativePositionAxes = Axes.X;
                details.MoveTo(new Vector2(-1, 0), 1000, Easing.OutQuint);
            });
        }

        private void cancelTransitionToDetails()
        {
            this.TransformTo(nameof(Margin), new MarginPadding(), 1000, Easing.OutQuint);
            results?.PopOutDetails(details =>
            {
                details.MoveTo(Vector2.Zero, 1000, Easing.OutQuint)
                       .Then().FadeOut();
            });
        }

        public bool UsesFixedAnchor { get; set; }
    }
}
