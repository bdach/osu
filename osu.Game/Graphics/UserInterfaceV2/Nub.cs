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
    internal class Nub<T> : CircularContainer
        where T : struct, IEquatable<T>, IComparable<T>, IConvertible
    {
        public const int WIDTH = 40;

        public Bindable<bool> Dragging { get; } = new BindableBool();

        private Color4 primaryColour;
        private Color4 activeColour;

        private Box fill;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Size = new Vector2(WIDTH, SliderBar<T>.HEIGHT);
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
