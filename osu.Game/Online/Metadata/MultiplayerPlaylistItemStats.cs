// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using MessagePack;

namespace osu.Game.Online.Metadata
{
    [MessagePackObject]
    [Serializable]
    public class MultiplayerPlaylistItemStats
    {
        public const int TOTAL_SCORE_DISTRIBUTION_BINS = 13;

        [Key(0)]
        public long PlaylistItemID { get; set; }

        [Key(1)]
        public long[] TotalScoreDistribution { get; set; } = new long[TOTAL_SCORE_DISTRIBUTION_BINS];
    }
}
