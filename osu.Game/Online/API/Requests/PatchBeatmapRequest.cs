// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net.Http;
using osu.Framework.IO.Network;

namespace osu.Game.Online.API.Requests
{
    public class PatchBeatmapRequest : APIUploadRequest
    {
        public uint BeatmapSetID { get; }
        public uint BeatmapID { get; }

        private readonly string filename;
        private readonly byte[] beatmapContents;

        protected override string Uri => $@"http://localhost:5089/beatmapsets/{BeatmapSetID}/beatmaps/{BeatmapID}";
        protected override string Target => throw new NotSupportedException();

        public PatchBeatmapRequest(uint beatmapSetId, uint beatmapId, string filename, byte[] beatmapContents)
        {
            BeatmapSetID = beatmapSetId;
            BeatmapID = beatmapId;
            this.filename = filename;
            this.beatmapContents = beatmapContents;
        }

        protected override WebRequest CreateWebRequest()
        {
            var request = base.CreateWebRequest();
            request.Method = HttpMethod.Patch;
            request.AddFile(@"beatmapContents", beatmapContents, filename);
            return request;
        }
    }
}
