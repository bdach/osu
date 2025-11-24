// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Users.Drawables
{
    public class AvatarStore
    {
        private readonly OnlineStore onlineStore;
        private readonly Storage avatarStorage;
        private readonly LargeTextureStore largeTextureStore;

        public AvatarStore(GameHost host)
        {
            onlineStore = new TrustedDomainOnlineStore();
            avatarStorage = host.CacheStorage.GetStorageForDirectory(@"avatars");

            largeTextureStore = new LargeTextureStore(host.Renderer, host.CreateTextureLoaderStore(new StorageBackedResourceStore(avatarStorage)));
            largeTextureStore.AddTextureSource(host.CreateTextureLoaderStore(onlineStore));
        }

        public Texture? GetTeamAvatar(APITeam team)
        {
            string flagUrl = team.FlagUrl;

            const string flag_url_prefix = "https://assets.ppy.sh/teams/flag/";

            if (!flagUrl.StartsWith(flag_url_prefix, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($@"Unrecognised team avatar URL format, serving online version from {flagUrl}.", LoggingTarget.Network);
                return largeTextureStore.Get(flagUrl);
            }

            string[] flagUrlParts = flagUrl[flag_url_prefix.Length..].Split('/');

            if (flagUrlParts.Length != 2)
            {
                Logger.Log($@"Unrecognised team avatar URL format, serving online version from {flagUrl}.", LoggingTarget.Network);
                return largeTextureStore.Get(flagUrl);
            }

            string teamId = flagUrlParts[0];
            string filename = flagUrlParts[1];
            string lookupKey = $"team_{teamId}_{filename}";

            if (!avatarStorage.Exists(lookupKey))
            {
                byte[] onlineResult = onlineStore.Get(flagUrl);

                using (var stream = avatarStorage.CreateFileSafely(lookupKey))
                    stream.Write(onlineResult, 0, onlineResult.Length);
            }

            Logger.Log($@"Serving team avatar from local cache ({flagUrl} -> {lookupKey}).", LoggingTarget.Network);
            return largeTextureStore.Get(lookupKey);
        }
    }
}
