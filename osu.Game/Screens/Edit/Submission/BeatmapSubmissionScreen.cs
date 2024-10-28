// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Edit.Submission
{
    public partial class BeatmapSubmissionScreen : OsuScreen
    {
        private BeatmapSubmissionOverlay overlay = null!;

        public override bool AllowBackButton => false;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        private Container submissionProgress = null!;
        private SubmissionStageProgress exportStep = null!;
        private SubmissionStageProgress createSetStep = null!;
        private SubmissionStageProgress uploadStep = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRangeInternal(new Drawable[]
            {
                overlay = new BeatmapSubmissionOverlay(),
                submissionProgress = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Alpha = 0,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = 0.8f,
                    Masking = true,
                    CornerRadius = 10,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background5,
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Padding = new MarginPadding(20),
                            Spacing = new Vector2(5),
                            Children = new Drawable[]
                            {
                                exportStep = new SubmissionStageProgress { StageDescription = BeatmapSubmissionStrings.ExportingBeatmapSet, },
                                createSetStep = new SubmissionStageProgress { StageDescription = BeatmapSubmissionStrings.CreatingBeatmapSet, },
                                uploadStep = new SubmissionStageProgress { StageDescription = BeatmapSubmissionStrings.UploadingBeatmapSetContents, },
                            }
                        }
                    }
                }
            });

            overlay.State.BindValueChanged(_ =>
            {
                if (overlay.State.Value == Visibility.Hidden)
                {
                    if (!overlay.Completed)
                        this.Exit();
                    else
                        beginSubmission();
                }
            });
        }

        private void beginSubmission()
        {
            submissionProgress.FadeIn(200, Easing.OutQuint);
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            overlay.Show();
        }
    }
}
