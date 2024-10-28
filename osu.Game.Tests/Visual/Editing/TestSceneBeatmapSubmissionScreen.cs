// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Screens.Edit.Submission;

namespace osu.Game.Tests.Visual.Editing
{
    public partial class TestSceneBeatmapSubmissionScreen : ScreenTestScene
    {
        [Test]
        public void TestAppearance()
        {
            AddStep("push screen", () => Stack.Push(new BeatmapSubmissionScreen()));
        }
    }
}
