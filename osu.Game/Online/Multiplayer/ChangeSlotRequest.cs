// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MessagePack;

namespace osu.Game.Online.Multiplayer
{
    [MessagePackObject]
    public class ChangeSlotRequest : MatchUserRequest
    {
        [Key(0)]
        public byte SlotID { get; set; }
    }
}
