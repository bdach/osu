// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using osu.Game.Beatmaps;
using osu.Game.IO.Archives;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Tests.Resources;

namespace osu.Game.Benchmarks
{
    public class BenchmarkReplayEncoding : BenchmarkTest
    {
        private FlatWorkingBeatmap workingBeatmap = null!;
        private Score score = null!;
        private LegacyScoreEncoder encoder = null!;

        public override void SetUp()
        {
            base.SetUp();

            using (var stream = TestResources.GetTestBeatmapStream(true))
            using (var archiveReader = new ZipArchiveReader(stream))
            {
                var beatmapStream = archiveReader.GetStream("Soleily - Renatus (Gamu) [Insane].osu");
                workingBeatmap = new FlatWorkingBeatmap(beatmapStream);
            }

            var playable = workingBeatmap.GetPlayableBeatmap(new OsuRuleset().RulesetInfo);
            var replay = new OsuAutoGenerator(playable, Array.Empty<Mod>()).Generate();

            score = new Score
            {
                ScoreInfo = TestResources.CreateTestScoreInfo(playable.BeatmapInfo),
                Replay = replay
            };
            score.ScoreInfo.Ruleset = new OsuRuleset().RulesetInfo;
            encoder = new LegacyScoreEncoder(score, playable);
        }

        [Benchmark]
        public void BenchmarkEncode()
        {
            encoder.Encode(new MemoryStream()); // `MemoryStream` doesn't work if created in `SetUp()`...
        }
    }
}
