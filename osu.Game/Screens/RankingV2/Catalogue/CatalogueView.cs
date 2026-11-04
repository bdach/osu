// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Scoring;
using osu.Game.Screens.RankingV2.Statistics;

namespace osu.Game.Screens.RankingV2.Catalogue
{
    public partial class CatalogueView : CompositeDrawable
    {
        [Resolved]
        private IBindable<IScoreInfo> score { get; set; } = null!;

        [Cached]
        private Bindable<APIBeatmap?> apiBeatmap { get; set; } = new Bindable<APIBeatmap?>();

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, IAPIProvider api)
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
                                                new GridContainer
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    RowDimensions =
                                                    [
                                                        new Dimension(GridSizeMode.AutoSize),
                                                        new Dimension(GridSizeMode.Absolute, 10),
                                                        new Dimension()
                                                    ],
                                                    Content = new[]
                                                    {
                                                        new Drawable[]
                                                        {
                                                            new RateBeatmapPanel(),
                                                        },
                                                        [],
                                                        new Drawable[]
                                                        {
                                                            new TagBeatmapPanel(),
                                                        }
                                                    }
                                                },
                                                Empty(),
                                                new AddToCollectionsPanel(),
                                            },
                                        },
                                    },
                                ],
                            },
                        },
                    },
                },
            ];

            var getBeatmapSetRequest = new GetBeatmapSetRequest(score.Value.Beatmap!.BeatmapSet!.OnlineID);
            getBeatmapSetRequest.Success += set =>
            {
                apiBeatmap.Value = set.Beatmaps.SingleOrDefault(b => b.MatchesOnlineID(score.Value.Beatmap));
            };
            api.Queue(getBeatmapSetRequest);
        }
    }
}
