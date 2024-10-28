// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net.Http;
using osu.Framework.IO.Network;

namespace osu.Game.Online.API.Requests
{
    public class PutBeatmapSetRequest : APIUploadRequest
    {
        public uint BeatmapSetID { get; }

        private readonly byte[] oszPackage;

        public PutBeatmapSetRequest(uint beatmapSetID, byte[] oszPackage)
        {
            this.oszPackage = oszPackage;
            BeatmapSetID = beatmapSetID;
        }

        protected override WebRequest CreateWebRequest()
        {
            var request = base.CreateWebRequest();
            request.AddFile(@"beatmapArchive", oszPackage);
            request.Method = HttpMethod.Put;
            return request;
        }

        protected override string Uri => $@"http://localhost:5089/beatmapsets/{BeatmapSetID}";
        protected override string Target => throw new NotSupportedException();
    }
}
