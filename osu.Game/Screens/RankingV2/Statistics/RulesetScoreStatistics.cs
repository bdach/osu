// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;

namespace osu.Game.Screens.RankingV2.Statistics
{
    public record RulesetScoreStatistics(
        Func<Drawable[]> LeftColumn,
        Func<Drawable[]> RightColumn);
}
