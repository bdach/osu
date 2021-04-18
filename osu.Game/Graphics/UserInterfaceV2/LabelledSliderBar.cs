// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;

namespace osu.Game.Graphics.UserInterfaceV2
{
    public class LabelledSliderBar<TNumber> : LabelledComponent<SliderBar<TNumber>, TNumber>
        where TNumber : struct, IEquatable<TNumber>, IComparable<TNumber>, IConvertible
    {
        public bool ShowTicks
        {
            get => Component.ShowTicks;
            set => Component.ShowTicks = value;
        }

        public BindableList<(TNumber, LocalisableString)> Labels => Component.Labels;

        public LabelledSliderBar()
            : base(true)
        {
        }

        protected override SliderBar<TNumber> CreateComponent() => new SliderBar<TNumber>
        {
            TransferValueOnCommit = true,
            RelativeSizeAxes = Axes.X,
        };
    }
}
