// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Submission;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    public partial class TestSceneSubmissionStageProgress : OsuTestScene
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        [Test]
        public void TestAppearance()
        {
            SubmissionStageProgress progress = null!;

            AddStep("create content", () => Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.8f),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Child = progress = new SubmissionStageProgress
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    StageDescription = "Frobnicating the foobarator...",
                }
            });
            AddStep("not started", () => progress.Status.Value = SubmissionStageProgress.StageStatusType.NotStarted);
            AddStep("in progress", () => progress.Status.Value = SubmissionStageProgress.StageStatusType.InProgress);
            AddStep("indeterminate progress", () => progress.Progress.Value = null);
            AddStep("30% progress", () => progress.Progress.Value = 0.3f);
            AddStep("70% progress", () => progress.Progress.Value = 0.7f);
            AddStep("completed", () => progress.Status.Value = SubmissionStageProgress.StageStatusType.Completed);
            AddStep("failed", () => progress.Status.Value = SubmissionStageProgress.StageStatusType.Failed);
        }
    }
}
