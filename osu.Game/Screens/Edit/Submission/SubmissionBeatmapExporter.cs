// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Screens.Edit.Submission
{
    public class SubmissionBeatmapExporter : LegacyBeatmapExporter
    {
        private readonly uint? beatmapSetId;
        private readonly Queue<uint>? beatmapIds;

        public SubmissionBeatmapExporter(Storage storage)
            : base(storage)
        {
        }

        public SubmissionBeatmapExporter(Storage storage, CreateBeatmapSetResponse createBeatmapSetResponse)
            : base(storage)
        {
            beatmapSetId = createBeatmapSetResponse.BeatmapSetId;
            beatmapIds = new Queue<uint>(createBeatmapSetResponse.BeatmapIds);
        }

        protected override void MutateBeatmap(IBeatmap playableBeatmap)
        {
            base.MutateBeatmap(playableBeatmap);

            if (beatmapSetId != null && beatmapIds != null)
            {
                playableBeatmap.BeatmapInfo.OnlineID = (int)beatmapIds.Dequeue();
                playableBeatmap.BeatmapInfo.BeatmapSet!.OnlineID = (int)beatmapSetId;
            }
        }
    }
}
