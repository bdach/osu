// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Screens.OnlinePlay.DailyChallenge;

namespace osu.Game.Tests.Visual.DailyChallenge
{
    public partial class TestSceneDailyChallengeCarousel : OsuTestScene
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Plum);

        [Test]
        public void TestBasicAppearance()
        {
            DailyChallengeCarousel carousel = null!;

            AddStep("create content", () => Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                carousel = new DailyChallengeCarousel()
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });
            AddSliderStep("adjust width", 0.1f, 1, 1, width =>
            {
                if (carousel.IsNotNull())
                    carousel.Width = width;
            });
            AddSliderStep("adjust height", 0.1f, 1, 1, height =>
            {
                if (carousel.IsNotNull())
                    carousel.Height = height;
            });
            AddRepeatStep("add content", () => carousel.Add(new FakeContent()), 3);
        }

        private partial class FakeContent : CompositeDrawable
        {
            private OsuSpriteText text;

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Colour4(RNG.NextSingle(), RNG.NextSingle(), RNG.NextSingle(), 1),
                    },
                    text = new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "Fake Content " + (char)('A' + RNG.Next(26)),
                    },
                };

                text.FadeOut(500, Easing.OutQuint)
                    .Then().FadeIn(500, Easing.OutQuint)
                    .Loop();
            }
        }
    }
}
