// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics.Containers;
using osu.Game.Online;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.RankingV2.Statistics;
using osu.Game.Tests.Resources;
using osu.Game.Tests.Visual.Ranking;
using osu.Game.Users;
using Realms;

namespace osu.Game.Tests.Visual.RankingV2
{
    public partial class TestSceneScoreStatisticsView : OsuTestScene
    {
        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Cached(typeof(IBindable<IScoreInfo>))]
        private Bindable<IScoreInfo> score { get; set; } = new Bindable<IScoreInfo>();

        [Cached(typeof(IBindable<ScoreBasedUserStatisticsUpdate>))]
        private Bindable<ScoreBasedUserStatisticsUpdate> statisticsUpdate = new Bindable<ScoreBasedUserStatisticsUpdate>();

        [Cached]
        private OverlayColourProvider colours { get; set; } = new OverlayColourProvider(OverlayColourScheme.Blue);

        protected override void LoadComplete()
        {
            base.LoadComplete();

            realm.Run(r =>
            {
                var beatmapInfo = r.All<BeatmapInfo>()
                                   .Filter($"{nameof(BeatmapInfo.Ruleset)}.{nameof(RulesetInfo.OnlineID)} = $0 AND {nameof(BeatmapInfo.StatusInt)} = 1", 0)
                                   .FirstOrDefault();

                if (beatmapInfo != null)
                    Beatmap.Value = beatmaps.GetWorkingBeatmap(beatmapInfo);
            });
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("set score", () =>
            {
                var s = TestResources.CreateTestScoreInfo(Beatmap.Value.BeatmapInfo);
                s.HitEvents = TestSceneStatisticsPanel.CreatePositionDistributedHitEvents();

                var graph = new List<HealthValue>();

                for (int i = 0; i < 300; ++i)
                    graph.Add(new HealthValue(1500 * i, RNG.NextDouble()));

                s.HealthGraph = graph;

                score.Value = s;
                statisticsUpdate.Value = new ScoreBasedUserStatisticsUpdate(s,
                    new UserStatistics
                    {
                        Level = new UserStatistics.LevelInfo
                        {
                            Current = 5,
                            Progress = 20,
                        },
                        GlobalRank = 38000,
                        CountryRank = 12006,
                        PP = 2134,
                        RankedScore = 21123849,
                        Accuracy = 0.985,
                        PlayCount = 13375,
                        PlayTime = 354490,
                        TotalScore = 128749597,
                        TotalHits = 0,
                        MaxCombo = 1233,
                    }, new UserStatistics
                    {
                        Level = new UserStatistics.LevelInfo
                        {
                            Current = 5,
                            Progress = 30,
                        },
                        GlobalRank = 36000,
                        CountryRank = 12000,
                        PP = (decimal)2134.5,
                        RankedScore = 23897015,
                        Accuracy = 0.984,
                        PlayCount = 13376,
                        PlayTime = 35789,
                        TotalScore = 132218497,
                        TotalHits = 0,
                        MaxCombo = 1233,
                    });
            });
            AddStep("create content", () => Child = new OsuScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding
                {
                    Horizontal = 30,
                    Vertical = 10,
                },
                Child = new ScoreStatisticsView(),
            });
        }
    }
}
