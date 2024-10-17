// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;

namespace osu.Game.Screens.Edit.Submission
{
    public partial class BeatmapSubmissionScreen : OsuScreen
    {
        private BeatmapSubmissionOverlay overlay = null!;

        public override bool AllowBackButton => false;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(overlay = new BeatmapSubmissionOverlay());

            overlay.State.BindValueChanged(_ =>
            {
                // TODO: probably won't fly in the future, but for now it'll work visually
                if (overlay.State.Value == Visibility.Hidden)
                    this.Exit();
            });
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            overlay.Show();
        }
    }
}
