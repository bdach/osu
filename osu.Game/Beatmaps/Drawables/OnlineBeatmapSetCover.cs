// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Configuration;
using osu.Game.Online.API;

namespace osu.Game.Beatmaps.Drawables
{
    [LongRunningLoad]
    public partial class OnlineBeatmapSetCover : Sprite
    {
        private readonly IBeatmapSetOnlineInfo set;
        private readonly BeatmapSetCoverType type;

        private Texture? coverTexture;
        private Texture? placeholderTexture;

        private Bindable<bool>? showAnimeCovers;

        public OnlineBeatmapSetCover(IBeatmapSetOnlineInfo set, BeatmapSetCoverType type = BeatmapSetCoverType.Cover)
        {
            ArgumentNullException.ThrowIfNull(set);

            this.set = set;
            this.type = type;
        }

        [BackgroundDependencyLoader]
        private void load(LargeTextureStore textures, OsuConfigManager configManager, IAPIProvider api)
        {
            string? resource = null;

            switch (type)
            {
                case BeatmapSetCoverType.Cover:
                    resource = set.Covers.Cover;
                    break;

                case BeatmapSetCoverType.Card:
                    resource = set.Covers.Card;
                    break;

                case BeatmapSetCoverType.List:
                    resource = set.Covers.List;
                    break;
            }

            if (resource != null)
                coverTexture = textures.Get(resource);

            if (set.HasAnimeCover)
            {
                placeholderTexture = getPlaceholderTexture(textures, api);

                showAnimeCovers = configManager.GetBindable<bool>(OsuSetting.ShowAnimeCovers);
                showAnimeCovers.BindValueChanged(val => Texture = val.NewValue ? coverTexture : placeholderTexture, true);
            }
            else
                Texture = coverTexture;
        }

        // https://github.com/ppy/osu-web/blob/e44938dc71fcfb4818d1b3ca03011e932ab8e9a4/resources/css/layout.less#L24-L29
        private static readonly string[] placeholder_assets =
        [
            @"/assets/images/0.b3bb5a86.jpg",
            @"/assets/images/1.5f98695d.jpg",
            @"/assets/images/2.b1cc230d.jpg",
            @"/assets/images/3.e324329d.jpg",
            @"/assets/images/4.96855f05.jpg",
            @"/assets/images/5.a8262d19.jpg",
        ];

        private Texture? getPlaceholderTexture(LargeTextureStore textures, IAPIProvider api)
        {
            // https://github.com/ppy/osu-web/blob/6b8cf63f51551c15050c7e7225f42509775f3b5f/resources/js/utils/css.ts#L63-L65
            string placeholderAsset = placeholder_assets[set.OnlineID % placeholder_assets.Length];
            string fullUrl = string.Concat(api.Endpoints.WebsiteUrl, placeholderAsset);
            return textures.Get(fullUrl);
        }
    }

    public enum BeatmapSetCoverType
    {
        Cover,
        Card,
        List,
    }
}
