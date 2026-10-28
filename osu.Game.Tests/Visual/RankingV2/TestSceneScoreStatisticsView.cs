// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.Containers;
using osu.Game.Online;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko;
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

        private readonly Bindable<ScoringMode> scoringMode = new Bindable<ScoringMode>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager configManager)
        {
            configManager.BindWith(OsuSetting.ScoreDisplayMode, scoringMode);
            Dependencies.Cache(Realm);
        }

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
            AddStep("set score", () => setScore(new OsuRuleset().RulesetInfo));
            AddStep("create content", () => Child = new OsuScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding
                {
                    Horizontal = 30,
                    Vertical = 10,
                },
                Child = new ScoreStatisticsView(),
                ScrollbarOverlapsContent = false,
            });

            AddStep("many mods", () => setScore(new OsuRuleset().RulesetInfo, s => s.Mods = new Mod[]
            {
                new OsuModNoFail(),
                new OsuModHardRock(),
                new OsuModHidden(),
                new OsuModFlashlight(),
                new OsuModStrictTracking(),
                new OsuModSpunOut(),
                new OsuModRandom(),
                new OsuModSingleTap(),
                new OsuModBubbles(),
                new OsuModDeflate(),
                new ModWindUp(),
                new OsuModMuted(),
                new OsuModNoScope(),
                new OsuModSynesthesia(),
                new OsuModTransform(),
            }));

            AddStep("osu!", () => setScore(new OsuRuleset().RulesetInfo));
            AddStep("taiko", () => setScore(new TaikoRuleset().RulesetInfo));
            AddStep("catch", () => setScore(new CatchRuleset().RulesetInfo));
            AddStep("mania", () => setScore(new ManiaRuleset().RulesetInfo));
            AddToggleStep("toggle classic scoring mode", b =>
            {
                scoringMode.Value = b ? ScoringMode.Classic : ScoringMode.Standardised;
                setScore(new OsuRuleset().RulesetInfo, score =>
                {
                    score.MaximumStatistics[HitResult.Great] = 100000;
                });
            });
        }

        private void setScore(RulesetInfo rulesetInfo, Action<ScoreInfo>? setUp = null)
        {
            var s = TestResources.CreateTestScoreInfo(Beatmap.Value.BeatmapInfo);
            s.Ruleset = rulesetInfo;
            s.HitEvents = TestSceneStatisticsPanel.CreatePositionDistributedHitEvents();

            var graph = new List<HealthValue>();

            for (int i = 0; i < 300; ++i)
                graph.Add(new HealthValue(1500 * i, RNG.NextDouble()));

            s.HealthGraph = graph;

            setUp?.Invoke(s);

            score.Value = s;
            statisticsUpdate.Value = new ScoreBasedUserStatisticsUpdate(s, new UserStatistics
            {
                Level = new UserStatistics.LevelInfo { Current = 5, Progress = 20, },
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
                Level = new UserStatistics.LevelInfo { Current = 5, Progress = 30, },
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
        }
    }
}
