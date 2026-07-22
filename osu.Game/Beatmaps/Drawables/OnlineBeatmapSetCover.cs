// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Configuration;
using osu.Game.Online.API;
using osuTK;

namespace osu.Game.Beatmaps.Drawables
{
    [LongRunningLoad]
    public partial class OnlineBeatmapSetCover : CompositeDrawable
    {
        private readonly IBeatmapSetOnlineInfo set;
        private readonly BeatmapSetCoverType type;

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

            AddInternal(new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                FillMode = FillMode.Fill,
                Texture = textures.Get(resource),
                EdgeSmoothness = new Vector2(2),
            });

            if (set.HasAnimeCover)
            {
                Sprite placeholder;
                AddInternal(placeholder = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    FillMode = FillMode.Fill,
                    Texture = getPlaceholderTexture(textures, api),
                    EdgeSmoothness = new Vector2(2),
                });

                showAnimeCovers = configManager.GetBindable<bool>(OsuSetting.ShowAnimeCovers);
                showAnimeCovers.BindValueChanged(val => placeholder.Alpha = val.NewValue ? 0 : 1, true);
            }
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
