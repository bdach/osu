// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD;

namespace osu.Game.Screens.Select.Leaderboards
{
    public class SoloGameplayLeaderboardProvider : IGameplayLeaderboardProvider
    {
        public bool IsPartial { get; }
        public IBindableList<ILeaderboardScore> Scores => scores;
        private readonly BindableList<ILeaderboardScore> scores = new BindableList<ILeaderboardScore>();

        public SoloGameplayLeaderboardProvider(GameplayState gameplayState, IEnumerable<ScoreInfo> otherScores, bool isPartial)
        {
            IsPartial = isPartial;

            if (!otherScores.Any())
                return;

            foreach (var s in otherScores)
                scores.Add(new LeaderboardScoreData(s, false));

            scores.Add(new LeaderboardScoreData(gameplayState.Score.ScoreInfo.User, gameplayState.ScoreProcessor, true)
            {
                // Local score should always show lower than any existing scores in cases of ties.
                DisplayOrder = { Value = long.MaxValue }
            });
        }
    }
}
