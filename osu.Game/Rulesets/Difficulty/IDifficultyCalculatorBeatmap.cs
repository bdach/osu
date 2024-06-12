// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Difficulty
{
    public interface IDifficultyCalculatorBeatmap
    {
        /// <summary>
        /// The hitobjects contained by this beatmap.
        /// </summary>
        IReadOnlyList<HitObject> HitObjects { get; }

        /// <summary>
        /// This beatmap's difficulty settings.
        /// </summary>
        BeatmapDifficulty Difficulty { get; }
    }
}
