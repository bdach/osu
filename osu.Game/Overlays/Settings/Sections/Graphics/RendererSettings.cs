// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Database;

namespace osu.Game.Overlays.Settings.Sections.Graphics
{
    public class RendererSettings : SettingsSubsection
    {
        protected override string Header => "Renderer";

        private SettingsEnumDropdown<FrameSync> frameLimiterDropdown;
        private SettingsEnumDropdown<ExecutionMode> executionModeDropdown;

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        [Resolved]
        private RealmContextFactory realmContextFactory { get; set; }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager osuConfig)
        {
            // NOTE: Compatability mode omitted
            Children = new Drawable[]
            {
                // TODO: this needs to be a custom dropdown at some point
                frameLimiterDropdown = new SettingsEnumDropdown<FrameSync>
                {
                    LabelText = "Frame limiter",
                    Current = config.GetBindable<FrameSync>(FrameworkSetting.FrameSync)
                },
                executionModeDropdown = new SettingsEnumDropdown<ExecutionMode>
                {
                    LabelText = "Threading mode",
                    // this bindable is explicitly decoupled from the framework bindables in order to safely purge existing realm contexts during the mode change.
                    // see value change callback in LoadComplete().
                    Current = new Bindable<ExecutionMode>(config.Get<ExecutionMode>(FrameworkSetting.ExecutionMode))
                },
                new SettingsCheckbox
                {
                    LabelText = "Show FPS",
                    Current = osuConfig.GetBindable<bool>(OsuSetting.ShowFpsDisplay)
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            frameLimiterDropdown.Current.BindValueChanged(limit =>
            {
                const string unlimited_frames_note = "Using unlimited frame limiter can lead to stutters, bad performance and overheating. It will not improve perceived latency. \"2x refresh rate\" is recommended.";

                frameLimiterDropdown.WarningText = limit.NewValue == FrameSync.Unlimited ? unlimited_frames_note : string.Empty;
            }, true);

            executionModeDropdown.Current.BindValueChanged(executionMode =>
            {
                using (realmContextFactory.BlockAllOperations())
                    config.SetValue(FrameworkSetting.ExecutionMode, executionMode.NewValue);
            });
        }
    }
}
