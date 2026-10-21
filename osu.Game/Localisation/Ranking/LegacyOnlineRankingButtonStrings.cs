// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation.Ranking
{
    public static class LegacyOnlineRankingButtonStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.Ranking.LegacyOnlineRankingButton";

        /// <summary>
        /// "▼ Online Ranking ▼"
        /// </summary>
        public static LocalisableString OnlineRanking => new TranslatableString(getKey(@"online_ranking"), @"▼ Online Ranking ▼");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
