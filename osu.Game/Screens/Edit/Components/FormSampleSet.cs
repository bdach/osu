// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Audio;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Edit.Components
{
    public partial class FormSampleSet : CompositeDrawable, IHasCurrentValue<EditorBeatmapSkin.SampleSet?>
    {
        public Bindable<EditorBeatmapSkin.SampleSet?> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private readonly BindableWithCurrent<EditorBeatmapSkin.SampleSet?> current = new BindableWithCurrent<EditorBeatmapSkin.SampleSet?>();

        private Box background = null!;
        private FormFieldCaption caption = null!;
        private GridContainer grid = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Masking = true;
            CornerRadius = 5;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background5,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding(9),
                    Spacing = new Vector2(7),
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        caption = new FormFieldCaption(),
                        grid = new GridContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            RowDimensions = Enumerable.Repeat(new Dimension(GridSizeMode.AutoSize), 4).ToArray(),
                            ColumnDimensions = Enumerable.Repeat(new Dimension(GridSizeMode.Absolute, 100), 4).Prepend(new Dimension(GridSizeMode.AutoSize)).ToArray(),
                            Content = createTableContent().ToArray(),
                        }
                    },
                },
            };
        }

        private IEnumerable<Drawable[]> createTableContent()
        {
            string[] columns = [HitSampleInfo.HIT_NORMAL, ..HitSampleInfo.ALL_ADDITIONS];
            string[] rows = HitSampleInfo.ALL_BANKS;

            yield return columns.Select(makeTableHeading).Prepend(Empty()).ToArray();

            foreach (string row in rows)
                yield return columns.Select(_ => makeButton()).Cast<Drawable>().Prepend(makeTableHeading(row)).ToArray();
        }

        private OsuSpriteText makeTableHeading(string text) => new OsuSpriteText
        {
            Text = text,
            Font = OsuFont.Style.Caption1,
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
        };

        private FormButton.Button makeButton() => new FormButton.Button
        {
            Text = "+",
            Width = 96,
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Margin = new MarginPadding(2),
        };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            updateState();
            Current.BindValueChanged(_ =>
            {
                caption.Caption = Current.Value?.Name ?? default(LocalisableString);
                Alpha = Current.Value != null ? 1 : 0;
            }, true);
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateState();
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);
            updateState();
        }

        private void updateState()
        {
            background.Colour = colourProvider.Background5;
            caption.Colour = colourProvider.Content2;

            BorderThickness = IsHovered ? 2 : 0;

            if (IsHovered)
                BorderColour = colourProvider.Light4;
        }
    }
}
