// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Settings.Sections.Online
{
    public partial class WebSettings : SettingsSubsection
    {
        protected override LocalisableString Header => OnlineSettingsStrings.WebHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = OnlineSettingsStrings.ExternalLinkWarning,
                    Current = config.GetBindable<bool>(OsuSetting.ExternalLinkWarning)
                }),
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = OnlineSettingsStrings.PreferNoVideo,
                    Current = config.GetBindable<bool>(OsuSetting.PreferNoVideo)
                })
                {
                    Keywords = new[] { "no-video" },
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = OnlineSettingsStrings.AutomaticallyDownloadMissingBeatmaps,
                    Current = config.GetBindable<bool>(OsuSetting.AutomaticallyDownloadMissingBeatmaps),
                })
                {
                    Keywords = new[] { "spectator", "replay" },
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = OnlineSettingsStrings.ShowExplicitContent,
                    Current = config.GetBindable<bool>(OsuSetting.ShowOnlineExplicitContent),
                })
                {
                    Keywords = new[] { "nsfw", "18+", "offensive" },
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = AccountsStrings.OptionsBeatmapsetShowAnimeCover.ToSentence(),
                    Current = config.GetBindable<bool>(OsuSetting.ShowAnimeCovers),
                })
            };
        }
    }
}
