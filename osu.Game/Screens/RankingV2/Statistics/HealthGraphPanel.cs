// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Layout;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Screens.RankingV2.Statistics
{
    public partial class HealthGraphPanel : StatisticItemPanel
    {
        private readonly IScoreWithReplayData scoreWithReplayData;

        private Container graphContainer = null!;
        private SmoothPath path = null!;

        private List<Vector2> vertices = [];

        private readonly LayoutValue pathCached = new LayoutValue(Invalidation.DrawSize);

        public HealthGraphPanel(IScoreWithReplayData scoreWithReplayData)
            : base("Health")
        {
            this.scoreWithReplayData = scoreWithReplayData;

            AddLayout(pathCached);
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 200,
                    Padding = new MarginPadding { Vertical = 5, },
                    Children = new Drawable[]
                    {
                        graphContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding
                            {
                                Left = 40,
                            },
                        },
                        new OsuSpriteText
                        {
                            Text = "100%",
                            Colour = colours.Green1,
                            Origin = Anchor.CentreLeft,
                            RelativePositionAxes = Axes.Both,
                        },
                        new OsuSpriteText
                        {
                            Text = "50%",
                            Colour = Interpolation.ValueAt(0.5, colours.Green1, colours.Red1, 0, 1),
                            Origin = Anchor.CentreLeft,
                            RelativePositionAxes = Axes.Both,
                            Y = 0.5f,
                        },
                        new OsuSpriteText
                        {
                            Text = "0%",
                            Colour = colours.Red1,
                            Origin = Anchor.CentreLeft,
                            RelativePositionAxes = Axes.Both,
                            Y = 1,
                        },
                    },
                },
            };

            for (int i = 0; i <= 4; ++i)
            {
                graphContainer.Add(new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = i % 2 == 0 ? 1 : 1,
                    Alpha = i % 2 == 0 ? 0.5f : 0.25f,
                    Colour = Interpolation.ValueAt((float)i / 4, colours.Green1, colours.Red1, 0, 1),
                    RelativePositionAxes = Axes.Y,
                    Y = (float)i / 4,
                });
            }

            var hpGraph = scoreWithReplayData.HealthGraph;
            double minTime = hpGraph.Min(hv => hv.Time);
            double maxTime = hpGraph.Max(hv => hv.Time);
            double timeRange = maxTime - minTime;

            graphContainer.Add(path = new SmoothPath
            {
                Colour = ColourInfo.GradientVertical(colours.Green1, colours.Red1),
                PathRadius = 1.5f,
            });

            for (int i = 0; i < hpGraph.Count; ++i)
            {
                var point = hpGraph[i];
                vertices.Add(new Vector2((float)((point.Time - minTime) / timeRange), 1 - (float)point.Health));
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!pathCached.IsValid)
            {
                path.ClearVertices();

                foreach (var vertex in vertices)
                    path.AddVertex(vertex * graphContainer.ChildSize);

                path.OriginPosition = path.PositionInBoundingBox(new Vector2());

                pathCached.Validate();
            }
        }
    }
}
