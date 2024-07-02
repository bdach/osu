// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Online.Rooms
{
    public class MultiplayerScore
    {
        [JsonProperty("id")]
        public long ID { get; set; }

        [JsonProperty("user")]
        public APIUser User { get; set; }

        [JsonProperty("rank")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ScoreRank Rank { get; set; }

        [JsonProperty("total_score")]
        public long TotalScore { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("max_combo")]
        public int MaxCombo { get; set; }

        [JsonProperty("mods")]
        public APIMod[] Mods { get; set; }

        [JsonProperty("statistics")]
        public Dictionary<HitResult, int> Statistics = new Dictionary<HitResult, int>();

        // TODO: check that this really exists, im spitballing rn
        [JsonProperty("maximum_statistics")]
        public Dictionary<HitResult, int> MaximumStatistics = new Dictionary<HitResult, int>();

        [JsonProperty("passed")]
        public bool Passed { get; set; }

        [JsonProperty("ended_at")]
        public DateTimeOffset EndedAt { get; set; }

        /// <summary>
        /// The position of this score, starting at 1.
        /// </summary>
        [JsonProperty("position")]
        public int? Position { get; set; }

        [JsonProperty("has_replay")]
        public bool HasReplay { get; set; }

        /// <summary>
        /// Any scores in the room around this score.
        /// </summary>
        [JsonProperty("scores_around")]
        [CanBeNull]
        public MultiplayerScoresAround ScoresAround { get; set; }

        public IScoreInfo CreateScoreInfo(ScoreManager scoreManager, RulesetStore rulesets, PlaylistItem playlistItem, [NotNull] BeatmapInfo beatmap)
        {
            var ruleset = rulesets.GetRuleset(playlistItem.RulesetID);
            if (ruleset == null)
                throw new InvalidOperationException($"Couldn't create score with unknown ruleset: {playlistItem.RulesetID}");

            return new SoloScoreInfo
            {
                ID = (ulong?)ID,
                TotalScore = TotalScore,
                MaxCombo = MaxCombo,
                Beatmap = playlistItem.Beatmap,
                RulesetID = playlistItem.RulesetID,
                Statistics = Statistics,
                User = User,
                Accuracy = Accuracy,
                EndedAt = EndedAt,
                HasReplay = HasReplay,
                Rank = Rank,
                Mods = Mods,
                Position = Position,
            };
        }
    }
}
