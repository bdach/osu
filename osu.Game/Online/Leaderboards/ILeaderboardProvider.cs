// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Game.Scoring;

namespace osu.Game.Online.Leaderboards
{
    public interface ILeaderboardProvider : IDisposable
    {
        IBindable<(IEnumerable<ScoreInfo> topScores, ScoreInfo? userScore)?> Scores { get; }
        event Action? RetrievalFailed;

        void Refetch();
    }
}
