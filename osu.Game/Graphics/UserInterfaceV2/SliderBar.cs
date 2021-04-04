// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserInterfaceV2
{
    public class SliderBar<T> : Framework.Graphics.UserInterface.SliderBar<T>
        where T : struct, IEquatable<T>, IComparable<T>, IConvertible
    {
        public const int HEIGHT = 20;
        public const int FADE_DURATION = 200;

        private bool showTicks;

        public bool ShowTicks
        {
            get => showTicks;
            set
            {
                if (showTicks == value)
                    return;

                showTicks = value;

                if (IsLoaded)
                    updateState();
            }
        }

        private Box leftBox;
        private Box rightBox;
        private Nub<T> nub;
        private TickContainer<T> ticks;

        private Color4 primaryColour;
        private Color4 disabledColour;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            AutoSizeAxes = Axes.Y;
            RangePadding = Nub<T>.WIDTH / 2.0f;

            primaryColour = colours.BlueDark;
            disabledColour = colours.Gray3;

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = HEIGHT,
                        Children = new Drawable[]
                        {
                            new CircularContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 7,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Masking = true,
                                Children = new[]
                                {
                                    leftBox = new Box
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                    },
                                    rightBox = new Box
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Colour = Colour4.FromHex("16191e")
                                    }
                                },
                            },
                            new Container
                            {
                                Padding = new MarginPadding
                                {
                                    Horizontal = RangePadding
                                },
                                RelativeSizeAxes = Axes.Both,
                                Child = nub = new Nub<T>
                                {
                                    Origin = Anchor.TopCentre,
                                    RelativePositionAxes = Axes.X,
                                    Current = Current
                                }
                            },
                        }
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Horizontal = RangePadding },
                        Child = ticks = new TickContainer<T>
                        {
                            RelativeSizeAxes = Axes.X,
                            Current = Current
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Current.BindDisabledChanged(_ => updateState(), true);
            FinishTransforms(true);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            leftBox.Scale = new Vector2(Math.Clamp(
                RangePadding + nub.DrawPosition.X, 0, DrawWidth), 1);
            rightBox.Scale = new Vector2(Math.Clamp(
                DrawWidth - nub.DrawPosition.X - RangePadding, 0, DrawWidth), 1);
        }

        protected override void UpdateValue(float value)
        {
            nub.MoveToX(value, 250, Easing.OutQuint);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            nub.Dragging.Value = true;
            return base.OnDragStart(e);
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            nub.Dragging.Value = false;
            base.OnDragEnd(e);
        }

        private void updateState()
        {
            leftBox.FadeColour(Current.Disabled ? disabledColour : primaryColour, FADE_DURATION, Easing.OutQuint);
            ticks.FadeTo(showTicks ? 1 : 0, FADE_DURATION, Easing.OutQuint);
        }
    }
}
