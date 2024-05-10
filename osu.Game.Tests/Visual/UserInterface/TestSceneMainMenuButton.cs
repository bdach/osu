// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Screens.Menu;
using osuTK.Input;
using Color4 = osuTK.Graphics.Color4;

namespace osu.Game.Tests.Visual.UserInterface
{
    [TestFixture]
    public partial class TestSceneMainMenuButton : OsuTestScene
    {
        [Test]
        public void TestStandardButton()
        {
            AddStep("add button", () => Child = new MainMenuIconButton(
                ButtonSystemStrings.Solo, @"button-default-select", OsuIcon.Player, new Color4(102, 68, 204, 255), () => { }, 0, Key.P)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                ButtonSystemState = ButtonSystemState.TopLevel,
            });
        }

        [Test]
        public void TestBeatmapOfTheDayButton()
        {
            AddStep("add button", () => Child = new BeatmapOfTheDayButton(
                @"button-default-select", new Color4(102, 68, 204, 255), () => { }, 0, Key.D)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                ButtonSystemState = ButtonSystemState.TopLevel,
                Beatmap = CreateAPIBeatmap(),
            });
        }
    }
}
