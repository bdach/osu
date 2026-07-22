// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Configuration;

namespace osu.Game.Beatmaps.Drawables
{
    [LongRunningLoad]
    public partial class OnlineBeatmapSetCover : Sprite
    {
        private readonly IBeatmapSetOnlineInfo set;
        private readonly BeatmapSetCoverType type;

        private Texture? coverTexture;

        private Bindable<bool>? showAnimeCovers;

        public OnlineBeatmapSetCover(IBeatmapSetOnlineInfo set, BeatmapSetCoverType type = BeatmapSetCoverType.Cover)
        {
            ArgumentNullException.ThrowIfNull(set);

            this.set = set;
            this.type = type;
        }

        [BackgroundDependencyLoader]
        private void load(LargeTextureStore textures, OsuConfigManager? configManager)
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

            if (set.HasAnimeCover && configManager != null)
            {
                showAnimeCovers = configManager.GetBindable<bool>(OsuSetting.ShowAnimeCovers);
                showAnimeCovers.BindValueChanged(val => Texture = val.NewValue ? coverTexture : null, true);
            }
            else
                Texture = coverTexture;
        }
    }

    public enum BeatmapSetCoverType
    {
        Cover,
        Card,
        List,
    }
}
