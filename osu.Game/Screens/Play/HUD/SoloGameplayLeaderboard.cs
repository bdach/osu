/*

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Configuration;

namespace osu.Game.Screens.Play.HUD
{
    public partial class SoloGameplayLeaderboard : GameplayLeaderboard
    {
        private const int duration = 100;

        private readonly Bindable<bool> configVisibility = new Bindable<bool>();

        /// <summary>
        /// Whether the leaderboard should be visible regardless of the configuration value.
        /// This is true by default, but can be changed.
        /// </summary>
        public readonly Bindable<bool> AlwaysVisible = new Bindable<bool>(true);

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.GameplayLeaderboard, configVisibility);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Alpha will be updated via `updateVisibility` below.
            Alpha = 0;

            AlwaysVisible.BindValueChanged(_ => updateVisibility());
            configVisibility.BindValueChanged(_ => updateVisibility(), true);
        }

        private void updateVisibility() =>
            this.FadeTo(AlwaysVisible.Value || configVisibility.Value ? 1 : 0, duration);
    }
}

*/
