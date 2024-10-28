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
            AddStep("not started", () => progress.Status.Value = new SubmissionStageProgress.StageStatus(SubmissionStageProgress.StageStatusType.NotStarted));
            AddStep("in progress (indeterminate)", () => progress.Status.Value = new SubmissionStageProgress.StageStatus(SubmissionStageProgress.StageStatusType.InProgress));
            AddStep("in progress (30%)", () => progress.Status.Value = new SubmissionStageProgress.StageStatus(SubmissionStageProgress.StageStatusType.InProgress, 0.3f));
            AddStep("in progress (70%)", () => progress.Status.Value = new SubmissionStageProgress.StageStatus(SubmissionStageProgress.StageStatusType.InProgress, 0.7f));
            AddStep("completed", () => progress.Status.Value = new SubmissionStageProgress.StageStatus(SubmissionStageProgress.StageStatusType.Completed));
            AddStep("failed", () => progress.Status.Value = new SubmissionStageProgress.StageStatus(SubmissionStageProgress.StageStatusType.Failed));
        }
    }
}
