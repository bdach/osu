// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Scoring;
using osu.Game.Screens.Select.Leaderboards;

namespace osu.Game.Screens.Ranking
{
    public partial class SoloResultsScreen : ResultsScreen
    {
        private readonly IEnumerable<ScoreInfo>? leaderboardScores;

        [Resolved]
        private LeaderboardProvider leaderboardProvider { get; set; }

        public SoloResultsScreen(ScoreInfo score, IEnumerable<ScoreInfo>? leaderboardScores = null)
            : base(score)
        {
            this.leaderboardScores = leaderboardScores;
        }

        protected override APIRequest? FetchScores(Action<IEnumerable<ScoreInfo>> scoresCallback)
        {
            if (leaderboardScores != null)
                scoresCallback.Invoke(leaderboardScores.Where(s => !s.MatchesOnlineID(Score)));
            else
            {
                Debug.Assert(Score != null);
                leaderboardProvider.GetOnlineScoresAsync(Score.BeatmapInfo!, Score.Ruleset, null, BeatmapLeaderboardScope.Global)
                                   .ContinueWith(r =>
                                   {
                                       var scores = r.GetResultSafely().best.ToList();
                                       var toDisplay = new List<ScoreInfo>();

                                       for (int i = 0; i < scores.Count; ++i)
                                       {
                                           var score = scores[i];
                                           int position = i + 1;

                                           if (score.MatchesOnlineID(Score))
                                           {
                                               Debug.Assert(Score != null);

                                               // we don't want to add the same score twice, but also setting any properties of `Score` this late will have no visible effect,
                                               // so we have to fish out the actual drawable panel and set the position to it directly.
                                               var panel = ScorePanelList.GetPanelForScore(Score);
                                               Score.Position = panel.ScorePosition.Value = position;
                                           }
                                           else
                                           {
                                               score.Position = position;
                                               toDisplay.Add(score);
                                           }
                                       }

                                       scoresCallback.Invoke(toDisplay);
                                   });
            }

            return null;
        }
    }
}
