// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Users;

namespace osu.Game.Online.API.Requests.Responses
{
    [Serializable]
    public class SoloScoreInfo : IScoreInfo
    {
        [JsonProperty("beatmap_id")]
        public int BeatmapID { get; set; }

        [JsonProperty("ruleset_id")]
        public int RulesetID { get; set; }

        [JsonProperty("build_id")]
        public int? BuildID { get; set; }

        [JsonProperty("passed")]
        public bool Passed { get; set; }

        [JsonProperty("total_score")]
        public long TotalScore { get; set; }

        [JsonProperty("total_score_without_mods")]
        public long TotalScoreWithoutMods { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("user_id")]
        public int UserID { get; set; }

        // TODO: probably want to update this column to match user stats (short)?
        [JsonProperty("max_combo")]
        public int MaxCombo { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        // ScoreRank is aligned to make 0 equal D. We still want to serialise this (even when DefaultValueHandling.Ignore is used).
        [JsonProperty("rank", DefaultValueHandling = DefaultValueHandling.Include)]
        public ScoreRank Rank { get; set; }

        [JsonProperty("started_at")]
        public DateTimeOffset? StartedAt { get; set; }

        [JsonProperty("ended_at")]
        public DateTimeOffset EndedAt { get; set; }

        [JsonProperty("mods")]
        public APIMod[] Mods { get; set; } = Array.Empty<APIMod>();

        [JsonIgnore]
        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonIgnore]
        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonIgnore]
        [JsonProperty("deleted_at")]
        public DateTimeOffset? DeletedAt { get; set; }

        [JsonProperty("statistics")]
        public Dictionary<HitResult, int> Statistics { get; set; } = new Dictionary<HitResult, int>();

        [JsonProperty("maximum_statistics")]
        public Dictionary<HitResult, int> MaximumStatistics { get; set; } = new Dictionary<HitResult, int>();

        /// <summary>
        /// Used to preserve the total score for legacy scores.
        /// </summary>
        [JsonProperty("legacy_total_score")]
        public int? LegacyTotalScore { get; set; }

        [JsonProperty("legacy_score_id")]
        public ulong? LegacyScoreId { get; set; }

        [JsonProperty("pauses")]
        public int[] Pauses { get; set; } = [];

        #region osu-web API additions (not stored to database).

        [JsonProperty("id")]
        public ulong? ID { get; set; }

        [JsonProperty("user")]
        public APIUser? User { get; set; }

        [JsonProperty("beatmap")]
        public APIBeatmap? Beatmap { get; set; }

        [JsonProperty("beatmapset")]
        public APIBeatmapSet? BeatmapSet
        {
            set
            {
                // in the deserialisation case we need to ferry this data across.
                // the order of properties returned by the API guarantees that the beatmap is populated by this point.
                if (!(Beatmap is APIBeatmap apiBeatmap))
                    throw new InvalidOperationException("Beatmap set metadata arrived before beatmap metadata in response");

                apiBeatmap.BeatmapSet = value;
            }
        }

        [JsonProperty("pp")]
        public double? PP { get; set; }

        [JsonProperty("has_replay")]
        public bool HasReplay { get; set; }

        [JsonProperty("ranked")]
        public bool Ranked { get; set; }

        [JsonProperty("preserve")]
        public bool Preserve { get; set; }

        [JsonProperty("processed")]
        public bool Processed { get; set; }

        // These properties are calculated or not relevant to any external usage.
        public bool ShouldSerializeID() => false;
        public bool ShouldSerializeUser() => false;
        public bool ShouldSerializeBeatmap() => false;
        public bool ShouldSerializeBeatmapSet() => false;
        public bool ShouldSerializePP() => false;
        public bool ShouldSerializeOnlineID() => false;
        public bool ShouldSerializeHasReplay() => false;
        public bool ShouldSerializePreserve() => false;
        public bool ShouldSerializeProcessed() => false;

        // These fields only need to be serialised if they hold values.
        // Generally this is required because this model may be used by server-side components, but
        // we don't want to bother sending these fields in score submission requests, for instance.
        public bool ShouldSerializeEndedAt() => EndedAt != default;
        public bool ShouldSerializeStartedAt() => StartedAt != null;
        public bool ShouldSerializeLegacyScoreId() => LegacyScoreId != null;
        public bool ShouldSerializeLegacyTotalScore() => LegacyTotalScore != null;
        public bool ShouldSerializeMods() => Mods.Length > 0;
        public bool ShouldSerializeUserID() => UserID > 0;
        public bool ShouldSerializeBeatmapID() => BeatmapID > 0;
        public bool ShouldSerializeBuildID() => BuildID != null;

        #endregion

        #region IScoreInfo

        public long OnlineID => (long?)ID ?? -1;

        IUser IScoreInfo.User => User!;
        DateTimeOffset IScoreInfo.Date => EndedAt;
        long IScoreInfo.LegacyOnlineID => (long?)LegacyScoreId ?? -1;

        // TODO: This passthrough is unsafe because `Beatmap` is not guaranteed to be populated.
        // An actual beatmap instance should be eventually injected here.
        IBeatmapInfo IScoreInfo.Beatmap => Beatmap!;

        // TODO: This passthrough is unsafe because `APIRuleset` does not implement many members.
        // An actual client-side ruleset instance should be eventually injected here.
        IRulesetInfo IScoreInfo.Ruleset => new APIBeatmap.APIRuleset { OnlineID = RulesetID };

        IEnumerable<IConfiguredMod> IScoreInfo.Mods => Mods;
        IReadOnlyDictionary<HitResult, int> IScoreInfo.Statistics => Statistics;
        IReadOnlyDictionary<HitResult, int> IScoreInfo.MaximumStatistics => MaximumStatistics;

        #endregion

        /// <summary>
        /// Whether this <see cref="ScoreInfo"/> represents a legacy (osu!stable) score.
        /// </summary>
        [JsonIgnore]
        public bool IsLegacyScore => LegacyScoreId != null;

        public override string ToString() => $"score_id: {ID} user_id: {UserID}";

        /// <summary>
        /// Creates a <see cref="SoloScoreInfo"/> from a local score for score submission.
        /// </summary>
        /// <param name="score">The local score.</param>
        public static SoloScoreInfo ForSubmission(ScoreInfo score) => new SoloScoreInfo
        {
            Rank = score.Rank,
            TotalScore = score.TotalScore,
            TotalScoreWithoutMods = score.TotalScoreWithoutMods,
            Accuracy = score.Accuracy,
            PP = score.PP,
            MaxCombo = score.MaxCombo,
            RulesetID = score.RulesetID,
            Passed = score.Passed,
            Mods = score.APIMods,
            Statistics = score.Statistics.Where(kvp => kvp.Value != 0).ToDictionary(),
            MaximumStatistics = score.MaximumStatistics.Where(kvp => kvp.Value != 0).ToDictionary(),
            Pauses = score.Pauses.ToArray(),
        };
    }
}
