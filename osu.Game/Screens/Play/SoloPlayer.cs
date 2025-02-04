// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osu.Game.Online.Solo;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking;
using osu.Game.Screens.Select.Leaderboards;

namespace osu.Game.Screens.Play
{
    public partial class SoloPlayer : SubmittingPlayer
    {
        private readonly StateTrackingLeaderboardProvider? leaderboardScores;
        private DependencyContainer dependencies = null!;

        [Cached(typeof(IGameplayLeaderboardProvider))]
        private SoloGameplayLeaderboardProvider leaderboardProvider = null!;

        public SoloPlayer(StateTrackingLeaderboardProvider? leaderboardScores, PlayerConfiguration? configuration = null)
            : base(configuration)
        {
            this.leaderboardScores = leaderboardScores;
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        [BackgroundDependencyLoader]
        private void load(GameplayState gameplayState)
        {
            leaderboardProvider = new SoloGameplayLeaderboardProvider(gameplayState, leaderboardScores?.Scores.Value.best ?? [], leaderboardScores?.Scope.Value != BeatmapLeaderboardScope.Local);
            dependencies.CacheAs(leaderboardProvider);
        }

        protected override APIRequest<APIScoreToken>? CreateTokenRequest()
        {
            int beatmapId = Beatmap.Value.BeatmapInfo.OnlineID;
            int rulesetId = Ruleset.Value.OnlineID;

            if (beatmapId <= 0)
                return null;

            if (!Ruleset.Value.IsLegacyRuleset())
                return null;

            return new CreateSoloScoreRequest(Beatmap.Value.BeatmapInfo, rulesetId, Game.VersionHash);
        }

        protected override bool ShouldExitOnTokenRetrievalFailure(Exception exception) => false;

        protected override APIRequest<MultiplayerScore> CreateSubmissionRequest(Score score, long token)
        {
            IBeatmapInfo beatmap = score.ScoreInfo.BeatmapInfo!;

            Debug.Assert(beatmap.OnlineID > 0);

            return new SubmitSoloScoreRequest(score.ScoreInfo, token, beatmap.OnlineID);
        }

        protected override ResultsScreen CreateResults(ScoreInfo score) => new SoloResultsScreen(score, leaderboardScores?.Scores.Value.best ?? [])
        {
            AllowRetry = true,
            ShowUserStatistics = true,
        };
    }
}
