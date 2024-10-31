// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class PutBeatmapSetRequest : APIRequest<PutBeatmapSetResponse>
    {
        protected override string Uri => @"http://localhost:5089/beatmapsets";
        protected override string Target => throw new NotSupportedException();

        [JsonProperty("beatmapset_id")]
        public uint? BeatmapSetID { get; init; }

        [JsonProperty("beatmaps_to_create")]
        public uint BeatmapsToCreate { get; init; }

        [JsonProperty("beatmaps_to_keep")]
        public uint[] BeatmapsToKeep { get; init; } = [];

        private PutBeatmapSetRequest()
        {
        }

        public static PutBeatmapSetRequest CreateNew(uint beatmapCount) => new PutBeatmapSetRequest
        {
            BeatmapsToCreate = beatmapCount,
        };

        public static PutBeatmapSetRequest UpdateExisting(uint beatmapSetId, IEnumerable<uint> beatmapsToKeep, uint beatmapsToCreate) => new PutBeatmapSetRequest
        {
            BeatmapSetID = beatmapSetId,
            BeatmapsToKeep = beatmapsToKeep.ToArray(),
            BeatmapsToCreate = beatmapsToCreate,
        };

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
