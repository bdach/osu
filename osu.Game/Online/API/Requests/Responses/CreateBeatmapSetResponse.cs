// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    public class CreateBeatmapSetResponse
    {
        [JsonProperty("beatmapset_id")]
        public uint BeatmapSetId { get; set; }

        [JsonProperty("beatmap_ids")]
        public ICollection<uint> BeatmapIds { get; set; } = Array.Empty<uint>();
    }
}
