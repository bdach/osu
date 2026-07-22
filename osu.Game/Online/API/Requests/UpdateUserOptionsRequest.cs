// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    [Serializable]
    public class UpdateUserOptionsRequest : APIRequest<APIMe>
    {
        [JsonProperty("user_profile_customization")]
        public UserPreferenceUpdate UserProfileCustomization { get; } = new UserPreferenceUpdate();

        [Serializable]
        [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        public class UserPreferenceUpdate
        {
            [JsonProperty("beatmapset_show_anime_cover")]
            public bool? ShowAnimeCovers { get; set; }
        }

        protected override string Target => @"me/options";

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();
            req.Method = HttpMethod.Put;
            req.ContentType = @"application/json";
            req.AddRaw(JsonConvert.SerializeObject(this));
            return req;
        }
    }
}
