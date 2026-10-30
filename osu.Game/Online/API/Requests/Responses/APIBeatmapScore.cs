// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Users;

namespace osu.Game.Online.API.Requests.Responses
{
    [Serializable]
    public class APIBeatmapScore : IScoreInfo
    {
        #region Properties populated directly from web

        [JsonProperty("classic_total_score")]
        public long ClassicTotalScore { get; set; }

        [JsonProperty("preserve")]
        public bool Preserve { get; set; }

        [JsonProperty("processed")]
        public bool Processed { get; set; }

        [JsonProperty("ranked")]
        public bool Ranked { get; set; }

        [JsonProperty("maximum_statistics")]
        public Dictionary<HitResult, int> MaximumStatistics { get; set; } = [];

        [JsonProperty("mods")]
        public APIMod[] APIMods { get; set; } = [];

        [JsonProperty("statistics")]
        public Dictionary<HitResult, int> Statistics { get; set; } = [];

        [JsonProperty("total_score_without_mods")]
        public long? TotalScoreWithoutMods { get; set; }

        [JsonProperty("beatmap_id")]
        public int BeatmapID { get; set; }

        // best_id

        [JsonProperty("id")]
        public long ID { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ScoreRank Rank { get; set; }

        // type

        [JsonProperty("user_id")]
        public int UserID { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("build_id")]
        public int? BuildID { get; set; }

        [JsonProperty("ended_at")]
        public DateTimeOffset EndedAt { get; set; }

        [JsonProperty("has_replay")]
        public bool HasReplay { get; set; }

        [JsonProperty("is_perfect_combo")]
        public bool IsPerfectCombo { get; set; }

        // legacy_perfect

        [JsonProperty("legacy_score_id")]
        public long? LegacyScoreID { get; set; }

        [JsonProperty("legacy_total_score")]
        public long LegacyTotalScore { get; set; }

        [JsonProperty("max_combo")]
        public int MaxCombo { get; set; }

        [JsonProperty("passed")]
        public bool Passed { get; set; }

        [JsonProperty("pp")]
        public double? PP { get; set; }

        [JsonProperty("ruleset_id")]
        public int RulesetID { get; set; }

        [JsonProperty("started_at")]
        public DateTimeOffset? StartedAt { get; set; }

        [JsonProperty("total_score")]
        public long TotalScore { get; set; }

        // replay
        // current_user_attributes

        [JsonProperty("user")]
        public APIUser User { get; set; } = new APIUser();

        #endregion

        #region Properties populated as part of request post-processing

        public IBeatmapInfo Beatmap { get; set; } = null!;
        public IRulesetInfo Ruleset { get; set; } = null!;

        #endregion

        #region Convenience properties

        public IEnumerable<Mod> Mods
        {
            get
            {
                var rulesetInstance = Ruleset.CreateInstance();
                return APIMods.Select(m => m.ToMod(rulesetInstance));
            }
        }

        #endregion

        #region IScoreInfo

        DateTimeOffset IScoreInfo.Date => EndedAt;
        long IHasOnlineID<long>.OnlineID => ID;
        long IScoreInfo.LegacyOnlineID => LegacyScoreID ?? -1;
        IReadOnlyDictionary<HitResult, int> IScoreInfo.MaximumStatistics => MaximumStatistics;
        IEnumerable<IConfiguredMod> IScoreInfo.Mods => APIMods;
        IReadOnlyDictionary<HitResult, int> IScoreInfo.Statistics => Statistics;
        IUser IScoreInfo.User => User;

        #endregion

        #region temporary passthroughs, remove when `LeaderboardManager` is weaned off `ScoreInfo`

        /// <summary>
        /// Create a <see cref="ScoreInfo"/> from an API score instance.
        /// </summary>
        /// <param name="rulesets">A ruleset store, used to populate a ruleset instance in the returned score.</param>
        /// <param name="beatmap">An optional beatmap, copied into the returned score (for cases where the API does not populate the beatmap).</param>
        /// <returns></returns>
        public ScoreInfo ToScoreInfo(RulesetStore rulesets, BeatmapInfo? beatmap = null)
        {
            var ruleset = rulesets.GetRuleset(RulesetID) ?? throw new InvalidOperationException($"Ruleset with ID of {RulesetID} not found locally");

            var rulesetInstance = ruleset.CreateInstance();

            var mods = Mods.Select(apiMod => apiMod.ToMod(rulesetInstance)).ToArray();

            var scoreInfo = ToScoreInfo(mods, beatmap);
            scoreInfo.Ruleset = ruleset;

            return scoreInfo;
        }

        /// <summary>
        /// Create a <see cref="ScoreInfo"/> from an API score instance.
        /// </summary>
        /// <param name="mods">The mod instances, resolved from a ruleset.</param>
        /// <param name="beatmap">The object to populate the scores' beatmap with.
        /// <list type="bullet">
        /// <item>If this is a <see cref="BeatmapInfo"/> type, then the score will be fully populated with the given object.</item>
        /// <item>Otherwise, if this is an <see cref="IBeatmapInfo"/> type (e.g. <see cref="APIBeatmap"/>), then only the beatmap ruleset will be populated.</item>
        /// <item>Otherwise, if this is <c>null</c>, then the beatmap ruleset will not be populated.</item>
        /// <item>The online beatmap ID is populated in all cases.</item>
        /// </list>
        /// </param>
        /// <returns>The populated <see cref="ScoreInfo"/>.</returns>
        public ScoreInfo ToScoreInfo(Mod[] mods, IBeatmapInfo? beatmap = null)
        {
            var score = new ScoreInfo
            {
                OnlineID = ID,
                LegacyOnlineID = LegacyScoreID ?? -1,
                IsLegacyScore = LegacyScoreID != null,
                User = User,
                BeatmapInfo = new BeatmapInfo { OnlineID = BeatmapID },
                Ruleset = new RulesetInfo { OnlineID = RulesetID },
                Passed = Passed,
                TotalScore = TotalScore,
                TotalScoreWithoutMods = TotalScoreWithoutMods ?? 0,
                LegacyTotalScore = LegacyTotalScore,
                Accuracy = Accuracy,
                MaxCombo = MaxCombo,
                Rank = Rank,
                Statistics = Statistics,
                MaximumStatistics = MaximumStatistics,
                Date = EndedAt,
                HasOnlineReplay = HasReplay,
                Mods = mods,
                PP = PP,
                Ranked = Ranked,
            };

            if (beatmap is BeatmapInfo realmBeatmap)
                score.BeatmapInfo = realmBeatmap;
            else if (beatmap != null)
            {
                score.BeatmapInfo.Ruleset.OnlineID = beatmap.Ruleset.OnlineID;
                score.BeatmapInfo.Ruleset.Name = beatmap.Ruleset.Name;
                score.BeatmapInfo.Ruleset.ShortName = beatmap.Ruleset.ShortName;
            }

            return score;
        }

        public static APIBeatmapScore FromScoreInfo(ScoreInfo score) => new APIBeatmapScore
        {
            Rank = score.Rank,
            TotalScore = score.TotalScore,
            TotalScoreWithoutMods = score.TotalScoreWithoutMods,
            Accuracy = score.Accuracy,
            PP = score.PP,
            MaxCombo = score.MaxCombo,
            RulesetID = score.RulesetID,
            Passed = score.Passed,
            APIMods = score.APIMods,
            Statistics = score.Statistics.Where(kvp => kvp.Value != 0).ToDictionary(),
            MaximumStatistics = score.MaximumStatistics.Where(kvp => kvp.Value != 0).ToDictionary(),
        };

        #endregion
    }
}
