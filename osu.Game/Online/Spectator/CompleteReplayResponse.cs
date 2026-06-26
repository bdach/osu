// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using MessagePack;

namespace osu.Game.Online.Spectator
{
    [Serializable]
    [MessagePackObject]
    public record CompleteReplayResponse(
        [property: Key(0)] IEnumerable<FrameDataBundle> FrameBundles
    );
}
