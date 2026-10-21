// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.RankingV2.Legacy;
using osu.Game.Tests.Visual.Ranking;

namespace osu.Game.Tests.Visual.RankingV2
{
    public partial class TestSceneLegacyRankingGraph : SkinnableTestScene
    {
        protected override Ruleset CreateRulesetForSkinProvider() => new OsuRuleset();

        [Cached(typeof(IBindable<IScoreInfo>))]
        private readonly Bindable<IScoreInfo> score = new Bindable<IScoreInfo>();

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("set score", () => score.Value = createScore());
            AddStep("create content", () => SetContents(_ => new LegacyRankingGraph()));

            AddToggleStep("toggle outline", t =>
            {
                foreach (var outline in this.ChildrenOfType<OutlineBox>())
                    outline.Alpha = t ? 1 : 0;
            });
            AddStep("begin animation", () =>
            {
                foreach (var graph in this.ChildrenOfType<LegacyRankingGraph>())
                    graph.StartAnimating(Time.Current);
            });
            AddStep("cancel animation", () =>
            {
                foreach (var graph in this.ChildrenOfType<LegacyRankingGraph>())
                    graph.FinishAnimating();
            });
        }

        private static ScoreInfo createScore()
        {
            var graph = new List<HealthValue>();

            for (int i = 0; i < 30; ++i)
                graph.Add(new HealthValue(1500 * i, RNG.NextDouble()));

            var score = new ScoreInfo
            {
                HealthGraph = graph,
                HitEvents = TestSceneStatisticsPanel.CreatePositionDistributedHitEvents(),
            };
            return score;
        }
    }
}
