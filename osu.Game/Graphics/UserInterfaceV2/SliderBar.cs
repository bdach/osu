// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

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

        private class Nub : CircularContainer
        {
            public const int WIDTH = 40;

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Size = new Vector2(WIDTH, HEIGHT);
                Masking = true;

                Children = new[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colours.BlueDark
                    }
                };
            }
        }
    }
}
