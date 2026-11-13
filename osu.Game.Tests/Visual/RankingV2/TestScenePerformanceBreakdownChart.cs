// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Overlays;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Screens.RankingV2.Statistics;

namespace osu.Game.Tests.Visual.RankingV2
{
    public partial class TestScenePerformanceBreakdownChart : OsuTestScene
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        [Test]
        public void TestAppearance()
        {
            AddStep("create component", () =>
            {
                PerformanceDisplayAttribute[] attrs =
                [
                    new PerformanceDisplayAttribute("Aim", "Aim", RNG.NextDouble()),
                    new PerformanceDisplayAttribute("Speed", "Speed", RNG.NextDouble()),
                    new PerformanceDisplayAttribute("Accuracy", "Accuracy", RNG.NextDouble()),
                    new PerformanceDisplayAttribute("Flashlight", "Flashlight Bonus", RNG.NextDouble()),
                    new PerformanceDisplayAttribute("Reading", "Reading", RNG.NextDouble()),
                ];
                PerformanceDisplayAttribute[] perfectAttrs =
                [
                    new PerformanceDisplayAttribute("Aim", "Aim", 1),
                    new PerformanceDisplayAttribute("Speed", "Speed", 1),
                    new PerformanceDisplayAttribute("Accuracy", "Accuracy", 1),
                    new PerformanceDisplayAttribute("Flashlight", "Flashlight Bonus", 1),
                    new PerformanceDisplayAttribute("Reading", "Reading", 1),
                ];

                Child = new PerformanceBreakdownChartV2(attrs, perfectAttrs)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };
            });
        }
    }
}
