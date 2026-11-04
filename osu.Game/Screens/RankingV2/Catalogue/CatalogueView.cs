// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Overlays;
using osu.Game.Screens.RankingV2.Statistics;
using osuTK;

namespace osu.Game.Screens.RankingV2.Catalogue
{
    public partial class CatalogueView : CompositeDrawable
    {
        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren =
            [
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions =
                    [
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(),
                    ],
                    ColumnDimensions = [new Dimension()],
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new BeatmapInfoPanel
                            {
                                Margin = new MarginPadding { Bottom = 10, },
                            },
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Masking = true,
                                CornerRadius = 20,
                                Children =
                                [
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background4,
                                    },
                                    new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding(20),
                                        RowDimensions = [new Dimension()],
                                        ColumnDimensions =
                                        [
                                            new Dimension(),
                                            new Dimension(GridSizeMode.Absolute, 10),
                                            new Dimension()
                                        ],
                                        Content = new[]
                                        {
                                            new[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(10),
                                                    Children =
                                                    [
                                                        new RateBeatmapPanel(),
                                                    ],
                                                },
                                                Empty(),
                                                Empty(),
                                            },
                                        },
                                    },
                                ],
                            },
                        },
                    },
                },
            ];
        }
    }
}
