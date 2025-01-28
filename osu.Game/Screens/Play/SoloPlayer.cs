// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osu.Game.Online.Solo;
using osu.Game.Scoring;
using osu.Game.Screens.Play.HUD;
using osu.Game.Screens.Select.Leaderboards;

namespace osu.Game.Screens.Play
{
    public partial class SoloPlayer : SubmittingPlayer
    {
        private ILeaderboardScoreProvider? scoreProvider;
        private DependencyContainer dependencies = null!;
        private SoloGameplayLeaderboard leaderboard = null!;

        public SoloPlayer(ILeaderboardScoreProvider? scoreProvider, PlayerConfiguration? configuration = null)
            : base(configuration)
        {
            this.scoreProvider = scoreProvider;
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        [BackgroundDependencyLoader]
        private void load()
        {
            if (scoreProvider == null)
            {
                var localScoreProvider = new LocalLeaderboardScoreProvider
                {
                    Beatmap = { Value = Beatmap.Value.BeatmapInfo },
                    Ruleset = { Value = Ruleset.Value },
                };
                AddInternal(localScoreProvider);
                scoreProvider = localScoreProvider;
            }

            dependencies.CacheAs(scoreProvider);
            leaderboard.Scores.BindTo(scoreProvider.Scores);
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

        protected override GameplayLeaderboard CreateGameplayLeaderboard() =>
            leaderboard = new SoloGameplayLeaderboard(Score.ScoreInfo.User)
            {
                AlwaysVisible = { Value = false },
            };

        protected override bool ShouldExitOnTokenRetrievalFailure(Exception exception) => false;

        protected override Task ImportScore(Score score)
        {
            // Before importing a score, stop binding the leaderboard with its score source.
            // This avoids a case where the imported score may cause a leaderboard refresh
            // (if the leaderboard's source is local).
            leaderboard.Scores.UnbindBindings();

            return base.ImportScore(score);
        }

        protected override APIRequest<MultiplayerScore> CreateSubmissionRequest(Score score, long token)
        {
            IBeatmapInfo beatmap = score.ScoreInfo.BeatmapInfo!;

            Debug.Assert(beatmap.OnlineID > 0);

            return new SubmitSoloScoreRequest(score.ScoreInfo, token, beatmap.OnlineID);
        }
    }
}
