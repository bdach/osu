// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets;
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

        #region IScoreInfo

        DateTimeOffset IScoreInfo.Date => EndedAt;
        long IHasOnlineID<long>.OnlineID => ID;
        long IScoreInfo.LegacyOnlineID => LegacyScoreID ?? -1;
        IReadOnlyDictionary<HitResult, int> IScoreInfo.MaximumStatistics => MaximumStatistics;
        IEnumerable<IConfiguredMod> IScoreInfo.Mods => Mods;
        IReadOnlyDictionary<HitResult, int> IScoreInfo.Statistics => Statistics;
        IUser IScoreInfo.User => User;

        #endregion
    }
}
