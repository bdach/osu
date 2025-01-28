// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Screens.Select.Leaderboards;
using osu.Game.Skinning;
using osu.Game.Users;

namespace osu.Game.Screens.Play.HUD
{
    public partial class SoloGameplayLeaderboard : GameplayLeaderboard, ISerialisableDrawable
    {
        private const int duration = 100;

        private readonly Bindable<bool> configVisibility = new Bindable<bool>();

        private readonly IBindableList<ScoreInfo> scores = new BindableList<ScoreInfo>();

        private IUser trackingUser = null!;

        [Resolved]
        private ScoreProcessor scoreProcessor { get; set; } = null!;

        [Resolved]
        private ILeaderboardScoreProvider leaderboardScoreProvider { get; set; } = null!;

        /// <summary>
        /// Whether the leaderboard should be visible regardless of the configuration value.
        /// This is true by default, but can be changed.
        /// </summary>
        public readonly Bindable<bool> AlwaysVisible = new Bindable<bool>(true);

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, GameplayState gameplayState)
        {
            config.BindWith(OsuSetting.GameplayLeaderboard, configVisibility);

            trackingUser = gameplayState.Score.ScoreInfo.User;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            scores.BindTo(leaderboardScoreProvider.Scores);
            scores.BindCollectionChanged((_, _) => Scheduler.AddOnce(showScores), true);

            // Alpha will be updated via `updateVisibility` below.
            Alpha = 0;

            AlwaysVisible.BindValueChanged(_ => updateVisibility());
            configVisibility.BindValueChanged(_ => updateVisibility(), true);
        }

        private void showScores()
        {
            Clear();

            if (!scores.Any())
                return;

            foreach (var s in scores)
            {
                var score = Add(s.User, false);

                score.GetDisplayScore = s.GetDisplayScore;
                score.TotalScore.Value = s.TotalScore;
                score.Accuracy.Value = s.Accuracy;
                score.Combo.Value = s.MaxCombo;
                score.DisplayOrder.Value = s.OnlineID > 0 ? s.OnlineID : s.Date.ToUnixTimeSeconds();
            }

            ILeaderboardScore local = Add(trackingUser, true);

            local.GetDisplayScore = scoreProcessor.GetDisplayScore;
            local.TotalScore.BindTarget = scoreProcessor.TotalScore;
            local.Accuracy.BindTarget = scoreProcessor.Accuracy;
            local.Combo.BindTarget = scoreProcessor.HighestCombo;

            // Local score should always show lower than any existing scores in cases of ties.
            local.DisplayOrder.Value = long.MaxValue;
        }

        protected override bool CheckValidScorePosition(GameplayLeaderboardScore score, int position)
        {
            // change displayed position to '-' when there are 50 already submitted scores and tracked score is last
            if (score.Tracked && leaderboardScoreProvider is OnlineLeaderboardScoreProvider)
            {
                if (position == Flow.Count && Flow.Count > GetScoresRequest.MAX_SCORES_PER_REQUEST)
                    return false;
            }

            return base.CheckValidScorePosition(score, position);
        }

        private void updateVisibility() =>
            this.FadeTo(AlwaysVisible.Value || configVisibility.Value ? 1 : 0, duration);

        public bool UsesFixedAnchor { get; set; }
    }
}
