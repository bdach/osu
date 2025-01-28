// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD;

namespace osu.Game.Screens.Select.Leaderboards
{
    public class SoloGameplayLeaderboardProvider : IGameplayLeaderboardProvider
    {
        public IEnumerable<ILeaderboardScore> Scores { get; } = [];

        public SoloGameplayLeaderboardProvider(GameplayState gameplayState, IEnumerable<ScoreInfo> otherScores)
        {
            if (!otherScores.Any())
                return;

            var scores = new List<ILeaderboardScore>();

            foreach (var s in otherScores)
                scores.Add(new LeaderboardScore(s, false));

            scores.Add(new LeaderboardScore(gameplayState.Score.ScoreInfo.User, gameplayState.ScoreProcessor, true)
            {
                // Local score should always show lower than any existing scores in cases of ties.
                DisplayOrder = { Value = long.MaxValue }
            });
        }
    }
}
