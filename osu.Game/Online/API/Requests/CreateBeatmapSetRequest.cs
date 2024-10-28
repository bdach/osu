// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class CreateBeatmapSetRequest : APIRequest<CreateBeatmapSetResponse>
    {
        protected override string Uri => @"http://localhost:5089/beatmapsets";
        protected override string Target => throw new NotSupportedException();

        [JsonProperty("beatmap_count")]
        public uint BeatmapCount { get; }

        public CreateBeatmapSetRequest(uint beatmapCount)
        {
            BeatmapCount = beatmapCount;
        }

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
