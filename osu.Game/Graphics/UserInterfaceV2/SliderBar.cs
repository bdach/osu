// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
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

        private Box leftBox;
        private Box rightBox;
        private Container nubContainer;
        private Nub nub;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Height = HEIGHT;
            RangePadding = Nub.WIDTH / 2.0f;

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
                            Colour = colours.BlueDark
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
                nubContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = nub = new Nub
                    {
                        Origin = Anchor.TopCentre,
                        RelativePositionAxes = Axes.X
                    }
                }
            };
        }

        protected override void Update()
        {
            base.Update();
            nubContainer.Padding = new MarginPadding { Horizontal = RangePadding };
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

        private class Nub : CircularContainer
        {
            public const int WIDTH = 40;

            public Bindable<bool> Dragging { get; } = new BindableBool();

            private Color4 primaryColour;
            private Color4 activeColour;

            private Box fill;

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Size = new Vector2(WIDTH, HEIGHT);
                Masking = true;

                primaryColour = colours.BlueDark;
                activeColour = colours.BlueDark.Lighten(0.3f);

                Children = new[]
                {
                    fill = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Dragging.BindValueChanged(_ => updateState(), true);
            }

            protected override bool OnHover(HoverEvent e)
            {
                updateState();
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                updateState();
            }

            private void updateState()
            {
                bool active = IsHovered || Dragging.Value;
                fill.FadeColour(active ? activeColour : primaryColour, 200, Easing.OutQuint);
            }
        }
    }
}
