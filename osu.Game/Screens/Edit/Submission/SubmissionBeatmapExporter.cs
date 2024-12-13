// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Screens.Edit.Submission
{
    public class SubmissionBeatmapExporter : LegacyBeatmapExporter
    {
        private readonly uint? beatmapSetId;
        private readonly HashSet<int>? beatmapIds;

        public SubmissionBeatmapExporter(Storage storage)
            : base(storage)
        {
        }

        public SubmissionBeatmapExporter(Storage storage, CreateBeatmapSetResponse createBeatmapSetResponse)
            : base(storage)
        {
            beatmapSetId = createBeatmapSetResponse.BeatmapSetId;
            beatmapIds = createBeatmapSetResponse.BeatmapIds.Select(id => (int)id).ToHashSet();
        }

        protected override void MutateBeatmap(IBeatmap playableBeatmap)
        {
            base.MutateBeatmap(playableBeatmap);

            if (beatmapSetId != null && beatmapIds != null)
            {
                playableBeatmap.BeatmapInfo.BeatmapSet!.OnlineID = (int)beatmapSetId;

                if (beatmapIds.Contains(playableBeatmap.BeatmapInfo.OnlineID))
                {
                    beatmapIds.Remove(playableBeatmap.BeatmapInfo.OnlineID);
                    return;
                }

                if (playableBeatmap.BeatmapInfo.OnlineID <= 0)
                {
                    if (beatmapIds.Count == 0)
                        throw new InvalidOperationException(@"Ran out of new beatmap IDs to assign to unsubmitted beatmaps!");

                    int newId = beatmapIds.First();
                    beatmapIds.Remove(newId);
                    playableBeatmap.BeatmapInfo.OnlineID = newId;
                }

                throw new InvalidOperationException(@"Encountered beatmap with ID that has not been assigned to it by the server!");
            }
        }
    }
}
