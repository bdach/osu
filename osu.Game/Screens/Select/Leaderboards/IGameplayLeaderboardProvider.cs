// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Game.Screens.Play.HUD;

namespace osu.Game.Screens.Select.Leaderboards
{
    public interface IGameplayLeaderboardProvider
    {
        public IBindableList<ILeaderboardScore> Scores { get; }

        bool IsPartial { get; }
    }
}
