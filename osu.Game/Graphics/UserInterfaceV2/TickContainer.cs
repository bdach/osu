// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Graphics.UserInterfaceV2
{
    internal class TickContainer<T> : CompositeDrawable, IHasCurrentValue<T>
        where T : struct, IEquatable<T>, IComparable<T>, IConvertible
    {
        private readonly BindableNumberWithCurrent<T> current = new BindableNumberWithCurrent<T>();

        public Bindable<T> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private Container ticks;
        private Container labels;

        public BindableList<(T, LocalisableString)> Labels { get; } = new BindableList<(T, LocalisableString)>();

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Y;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Colour = Colour4.FromHex("3f6073"),
                Children = new Drawable[]
                {
                    ticks = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 2
                    },
                    labels = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            current.MinValueChanged += _ => generateTicks();
            current.MaxValueChanged += _ => generateTicks();
            current.PrecisionChanged += _ => generateTicks();
            generateTicks();

            Labels.BindCollectionChanged((_, __) => generateLabels(), true);
        }

        private void generateTicks()
        {
            ticks.Clear();

            double minValue = Convert.ToDouble(current.MinValue);
            double maxValue = Convert.ToDouble(current.MaxValue);
            double step = Convert.ToDouble(current.Precision);

            double estimatedTicks = (maxValue - minValue) / step;

            if (estimatedTicks > 100)
                return;

            int i = 0;
            double lastTick;

            do
            {
                lastTick = Math.Min(i * step / (maxValue - minValue), 1);

                ticks.Add(new Tick
                {
                    RelativePositionAxes = Axes.X,
                    X = (float)lastTick
                });

                i += 1;
            } while (lastTick < 1);

            generateLabels();
        }

        private class Tick : Box
        {
            public Tick()
            {
                RelativeSizeAxes = Axes.Y;
                Width = 1;
                EdgeSmoothness = new Vector2(1);
            }
        }

        private void generateLabels()
        {
            labels.Clear();

            double minValue = Convert.ToDouble(current.MinValue);
            double maxValue = Convert.ToDouble(current.MaxValue);

            foreach (var (position, label) in Labels)
            {
                double pos = Convert.ToDouble(position);
                float relativePos = (float)Math.Clamp((pos - minValue) / (maxValue - minValue), 0, 1);

                var anchor = anchorForPosition(relativePos);

                labels.Add(new OsuSpriteText
                {
                    RelativePositionAxes = Axes.X,
                    X = relativePos,
                    Text = label,
                    Font = OsuFont.Default.With(size: 12),
                    Shadow = false,
                    Origin = anchor
                });
            }
        }

        private Anchor anchorForPosition(float position)
        {
            if (Precision.AlmostEquals(position, 0))
                return Anchor.TopLeft;

            if (Precision.AlmostEquals(position, 1))
                return Anchor.TopRight;

            return Anchor.TopCentre;
        }
    }
}
