// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osuTK;

namespace osu.Game.Screens.Edit.Timing
{
    public partial class SliderVelocityAdjustmentControl : CompositeDrawable
    {
        public IBindable<double> Current => current;

        private readonly Bindable<double> current = new BindableNumber<double>(1)
        {
            Precision = 0.01,
            MinValue = 0.1,
            MaxValue = 10
        };

        public BindableList<HitObject> ObjectsToAdjust { get; } = new BindableList<HitObject>();

        private bool isMultipleValues;
        private bool applyingStateFromBeatmap;

        private FormDiscreteAdjustmentControl<double> control = null!;
        private FillFlowContainer presetsFlow = null!;

        private readonly BindableList<double> presets = new BindableList<double>();

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(5),
                Children = new Drawable[]
                {
                    control = new FormDiscreteAdjustmentControl<double>(0.05)
                    {
                        Caption = "Slider velocity",
                        Current = current,
                    },
                    presetsFlow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Full,
                        Spacing = new Vector2(5),
                    }
                }
            };

            beatmap.TransactionEnded += updateState;
            beatmap.BeatmapReprocessed += updateState;
            ObjectsToAdjust.BindCollectionChanged((_, _) => updateState());

            presets.BindTo(beatmap.SliderVelocityPresets);
            presets.BindCollectionChanged((_, _) => updatePresets(), true);

            current.BindValueChanged(val => applyVelocity(val.NewValue));
        }

        private void updateState()
        {
            HashSet<double> velocities = ObjectsToAdjust.OfType<IHasSliderVelocity>().Select(point => point.SliderVelocityMultiplier).Distinct().ToHashSet();
            isMultipleValues = velocities.Count > 1;

            applyingStateFromBeatmap = true;

            control.Current.Value = velocities.FirstOrDefault(defaultValue: 1);

            control.LabelFormat = isMultipleValues
                ? static _ => "(multiple)"
                : v => LocalisableString.Interpolate($@"{v:0.00}x");
            control.TextBox.PlaceholderText = isMultipleValues ? "(multiple)" : string.Empty;

            foreach (var preset in presetsFlow.OfType<SliderVelocityPresetTernaryButton>())
            {
                if (velocities.Contains(preset.Velocity))
                    preset.Current.Value = isMultipleValues ? TernaryState.Indeterminate : TernaryState.True;
                else
                    preset.Current.Value = TernaryState.False;
            }

            applyingStateFromBeatmap = false;
        }

        private void updatePresets()
        {
            presetsFlow.RemoveAll(d => d is SliderVelocityPresetTernaryButton, true);

            foreach (double preset in presets)
            {
                var presetButton = new SliderVelocityPresetTernaryButton(preset)
                {
                    Description = default,
                };
                presetButton.Current.BindValueChanged(val =>
                {
                    if (val.NewValue != TernaryState.True)
                        return;

                    if (applyingStateFromBeatmap)
                        return;

                    applyVelocity(preset);
                });

                presetsFlow.Add(presetButton);
            }

            updateState();
        }

        private void applyVelocity(double velocity)
        {
            if (applyingStateFromBeatmap)
                return;

            beatmap.BeginChange();

            foreach (var h in ObjectsToAdjust)
            {
                if (h is IHasSliderVelocity sv)
                {
                    sv.SliderVelocityMultiplier = velocity;
                    beatmap.Update(h);
                }
            }

            beatmap.EndChange();
        }

        public bool TakeFocus()
        {
            if (isMultipleValues)
                control.TextBox.Text = string.Empty;
            return GetContainingFocusManager()!.ChangeFocus(control.TextBox);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (beatmap.IsNotNull())
            {
                beatmap.TransactionEnded -= updateState;
                beatmap.BeatmapReprocessed -= updateState;
            }

            base.Dispose(isDisposing);
        }

        private partial class SliderVelocityPresetTernaryButton : DrawableTernaryButton
        {
            public double Velocity { get; }

            public SliderVelocityPresetTernaryButton(double velocity)
            {
                Velocity = velocity;
                CreateIcon = () => new Container
                {
                    Child = new OsuSpriteText
                    {
                        Text = LocalisableString.Format($@"{velocity:0.00}x"),
                        Font = OsuFont.Style.Body.With(weight: FontWeight.Bold),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                };
                RelativeSizeAxes = Axes.None;
                Width = 50;
                Height = 25;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Icon.Position = Vector2.Zero;
                Icon.RelativeSizeAxes = Axes.Both;
                Icon.Size = new Vector2(1);
            }
        }
    }
}
