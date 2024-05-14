// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Scoring
{
    public static class ScoreInfoExtensions
    {
        /// <summary>
        /// A user-presentable display title representing this score.
        /// </summary>
        public static string GetDisplayTitle(this IScoreInfo scoreInfo) => $"{scoreInfo.User.Username} playing {scoreInfo.Beatmap?.GetDisplayTitle() ?? "unknown"}";

        /// <summary>
        /// Orders an array of <see cref="ScoreInfo"/>s by total score.
        /// </summary>
        /// <param name="scores">The array of <see cref="ScoreInfo"/>s to reorder.</param>
        /// <returns>The given <paramref name="scores"/> ordered by decreasing total score.</returns>
        public static IEnumerable<ScoreInfo> OrderByTotalScore(this IEnumerable<ScoreInfo> scores)
            => scores.OrderByDescending(s => s.TotalScore)
                     .ThenBy(s => s.OnlineID)
                     // Local scores may not have an online ID. Fall back to date in these cases.
                     .ThenBy(s => s.Date);

        /// <summary>
        /// Retrieves the maximum achievable combo for the provided score.
        /// </summary>
        /// <param name="score">The <see cref="ScoreInfo"/> to compute the maximum achievable combo for.</param>
        /// <returns>The maximum achievable combo.</returns>
        public static int GetMaximumAchievableCombo(this IScoreInfo score) => score.MaximumStatistics.Where(kvp => kvp.Key.AffectsCombo()).Sum(kvp => kvp.Value);

        public static Mod ToMod(this IConfiguredMod mod, Ruleset ruleset)
        {
            Mod? resultMod = ruleset.CreateModFromAcronym(mod.Acronym);

            if (resultMod == null)
            {
                Logger.Log($"There is no mod in the ruleset ({ruleset.ShortName}) matching the acronym {mod.Acronym}.");
                return new UnknownMod(mod.Acronym);
            }

            if (mod.Settings.Count > 0)
            {
                foreach (var (_, property) in resultMod.GetSettingsSourceProperties())
                {
                    if (!mod.Settings.TryGetValue(property.Name.ToSnakeCase(), out object? settingValue))
                        continue;

                    try
                    {
                        resultMod.CopyAdjustedSetting((IBindable)property.GetValue(resultMod)!, settingValue);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Failed to copy mod setting value '{settingValue}' to \"{property.Name}\": {ex.Message}");
                    }
                }
            }

            return resultMod;
        }

        public static IEnumerable<Mod> InstantiateMods(this IScoreInfo score, RulesetStore rulesets)
        {
            var ruleset = rulesets.GetRuleset(score.Ruleset.ShortName)!.CreateInstance();
            return score.Mods.Select(m => m.ToMod(ruleset));
        }
    }
}
