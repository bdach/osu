// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;
using osu.Game.Users;

namespace osu.Game.Scoring
{
    public interface IScoreInfo : IHasOnlineID<long>, IEquatable<IScoreInfo>
    {
        IUser User { get; }

        /// <summary>
        /// The standardised total score.
        /// </summary>
        long TotalScore { get; }

        int MaxCombo { get; }

        double Accuracy { get; }

        long LegacyOnlineID { get; }

        DateTimeOffset Date { get; }

        double? PP { get; }

        IBeatmapInfo? Beatmap { get; }

        IRulesetInfo Ruleset { get; }

        ScoreRank Rank { get; }

        IEnumerable<IConfiguredMod> Mods { get; }

        IReadOnlyDictionary<HitResult, int> Statistics { get; }

        IReadOnlyDictionary<HitResult, int> MaximumStatistics { get; }

        bool IsLegacyScore { get; }
    }
}
