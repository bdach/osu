// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Scoring
{
    public interface IScoreWithReplayData : IScoreInfo
    {
        IReadOnlyList<HitEvent> HitEvents { get; }

        IReadOnlyList<HealthValue> HealthGraph { get; }
    }
}
