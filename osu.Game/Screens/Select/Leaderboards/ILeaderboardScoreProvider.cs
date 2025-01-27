// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;

namespace osu.Game.Screens.Select.Leaderboards
{
    public interface ILeaderboardScoreProvider
    {
        IBindableList<ScoreInfo> Scores { get; }
        IBindable<bool> Loading { get; }

        Bindable<BeatmapInfo> Beatmap { get; }
        Bindable<RulesetInfo> Ruleset { get; }
        Bindable<bool> ModFilterActive { get; }
        Bindable<IReadOnlyList<Mod>> Mods { get; }
    }
}
