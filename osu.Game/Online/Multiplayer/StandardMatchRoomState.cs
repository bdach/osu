// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MessagePack;

namespace osu.Game.Online.Multiplayer
{
    [MessagePackObject]
    public class StandardMatchRoomState : MatchRoomState
    {
        [Key(2)]
        public int?[]? Slots { get; set; }
    }
}
