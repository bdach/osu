// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Scoring;
using osu.Game.Screens.Select.Leaderboards;

namespace osu.Game.Screens.Ranking
{
    public partial class SoloResultsScreen : ResultsScreen
    {
        private ILeaderboardScoreProvider? scoreProvider;

        public SoloResultsScreen(ScoreInfo score, ILeaderboardScoreProvider? scoreProvider = null)
            : base(score)
        {
            this.scoreProvider = scoreProvider;
        }

        protected override APIRequest? FetchScores(Action<IEnumerable<ScoreInfo>> scoresCallback)
        {
            switch (scoreProvider)
            {
                case OnlineLeaderboardScoreProvider onlineScoreProvider:
                    onlineScoreProvider.Success += scoresReceived;
                    onlineScoreProvider.RefetchScores();
                    break;

                case null:
                {
                    var onlineScoreProvider = new OnlineLeaderboardScoreProvider(BeatmapLeaderboardScope.Global)
                    {
                        Beatmap = { Value = Beatmap.Value.BeatmapInfo },
                        Ruleset = { Value = Ruleset.Value },
                    };
                    onlineScoreProvider.Success += scoresReceived;
                    AddInternal(onlineScoreProvider);
                    scoreProvider = onlineScoreProvider;
                    break;
                }

                default:
                    scoresCallback.Invoke(scoreProvider.Scores.Where(s => !s.MatchesOnlineID(Score) && s.ID != Score?.ID));
                    break;
            }

            return null;

            void scoresReceived(ScoreInfo[] scores, ScoreInfo? _)
            {
                scoresCallback.Invoke(scores.Where(s => !s.MatchesOnlineID(Score)));
                ((OnlineLeaderboardScoreProvider)scoreProvider!).Success -= scoresReceived;
            }
        }
    }
}
