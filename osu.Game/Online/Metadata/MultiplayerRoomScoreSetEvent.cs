// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using MessagePack;

namespace osu.Game.Online.Metadata
{
    [Serializable]
    [MessagePackObject]
    public class MultiplayerRoomScoreSetEvent
    {
        [Key(0)]
        public long RoomID { get; set; }

        [Key(1)]
        public long PlaylistItemID { get; set; }

        [Key(2)]
        public long ScoreID { get; set; }

        [Key(3)]
        public int UserID { get; set; }

        [Key(4)]
        public long TotalScore { get; set; }
    }
}
