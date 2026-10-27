// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics;

namespace osu.Game.Screens.RankingV2.Statistics
{
    public partial class PerformanceBreakdownPanel : StatisticItemPanel
    {
        [Resolved]
        private IBindable<IScoreInfo> score { get; set; } = null!;

        public PerformanceBreakdownPanel()
            : base("Performance Breakdown")
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            score.BindValueChanged(_ => updateState(), true);
        }

        private void updateState()
        {
            // TODO: this is going to SUCK to get past.
            // probably going to need to convert ppcalc to use IScoreInfo, but at what (perf) cost?
            if (score.Value is ScoreInfo scoreInfo)
            {
                Alpha = 1;
                Child = new PerformanceBreakdownChart(scoreInfo)
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                };
            }
            else
            {
                Alpha = 0;
            }
        }
    }
}
