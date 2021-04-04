// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserInterfaceV2
{
    internal class Nub<T> : CircularContainer, IHasCurrentValue<T>
        where T : struct, IEquatable<T>, IComparable<T>, IConvertible
    {
        public const int WIDTH = 40;

        public Bindable<bool> Dragging { get; } = new BindableBool();

        private readonly BindableNumberWithCurrent<T> current = new BindableNumberWithCurrent<T>();

        public Bindable<T> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private Color4 primaryColour;
        private Color4 activeColour;
        private Color4 disabledColour;

        private Box fill;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Size = new Vector2(WIDTH, SliderBar<T>.HEIGHT);
            Masking = true;

            primaryColour = colours.BlueDark;
            activeColour = colours.BlueDark.Lighten(0.3f);
            disabledColour = colours.Gray3;

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

            Current.BindDisabledChanged(_ => updateState());
            Dragging.BindValueChanged(_ => updateState(), true);
            FinishTransforms(true);
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
            if (Current.Disabled)
            {
                fill.FadeColour(disabledColour, SliderBar<T>.FADE_DURATION, Easing.OutQuint);
                return;
            }

            bool active = IsHovered || Dragging.Value;
            fill.FadeColour(active ? activeColour : primaryColour, SliderBar<T>.FADE_DURATION, Easing.OutQuint);
        }
    }
}
