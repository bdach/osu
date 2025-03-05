// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.Leaderboards;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;

namespace osu.Game.Screens.Select.Leaderboards
{
    public partial class BeatmapLeaderboard : Leaderboard<BeatmapLeaderboardScope, ScoreInfo>
    {
        public Action<ScoreInfo>? ScoreSelected;

        private BeatmapInfo? beatmapInfo;

        public BeatmapInfo? BeatmapInfo
        {
            get => beatmapInfo;
            set
            {
                if (beatmapInfo == null && value == null)
                    return;

                if (beatmapInfo?.Equals(value) == true)
                    return;

                beatmapInfo = value;

                // Refetch is scheduled, which can cause scores to be outdated if the leaderboard is not currently updating.
                // As scores are potentially used by other components, clear them eagerly to ensure a more correct state.
                SetScores(null);

                RefetchScores();
            }
        }

        private bool filterMods;

        /// <summary>
        /// Whether to apply the game's currently selected mods as a filter when retrieving scores.
        /// </summary>
        public bool FilterMods
        {
            get => filterMods;
            set
            {
                if (value == filterMods)
                    return;

                filterMods = value;

                RefetchScores();
            }
        }

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private LeaderboardManager leaderboardManager { get; set; } = null!;

        [Resolved]
        private Bindable<ILeaderboardProvider?> leaderboardProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            ruleset.ValueChanged += _ => RefetchScores();
            mods.ValueChanged += _ =>
            {
                if (filterMods)
                    RefetchScores();
            };
        }

        protected override bool IsOnlineScope => Scope != BeatmapLeaderboardScope.Local;

        protected override APIRequest? FetchScores(CancellationToken cancellationToken)
        {
            var fetchBeatmapInfo = BeatmapInfo;

            if (fetchBeatmapInfo == null)
            {
                SetErrorState(LeaderboardState.NoneSelected);
                return null;
            }

            var fetchRuleset = ruleset.Value ?? fetchBeatmapInfo.Ruleset;

            if (!api.IsLoggedIn)
            {
                SetErrorState(LeaderboardState.NotLoggedIn);
                return null;
            }

            if (!fetchRuleset.IsLegacyRuleset())
            {
                SetErrorState(LeaderboardState.RulesetUnavailable);
                return null;
            }

            if ((fetchBeatmapInfo.OnlineID <= 0 || fetchBeatmapInfo.Status <= BeatmapOnlineStatus.Pending) && IsOnlineScope)
            {
                SetErrorState(LeaderboardState.BeatmapUnavailable);
                return null;
            }

            if (!api.LocalUser.Value.IsSupporter && (Scope >= BeatmapLeaderboardScope.Country || filterMods))
            {
                SetErrorState(LeaderboardState.NotSupporter);
                return null;
            }

            leaderboardProvider.Value?.Dispose();
            leaderboardProvider.Value = null;

            var newProvider = leaderboardManager.GetLeaderboardFor(new LeaderboardCriteria(
                Scope,
                fetchBeatmapInfo,
                fetchRuleset,
                filterMods ? mods.Value.ToArray() : null
            ));
            newProvider.Scores.BindValueChanged(val =>
            {
                if (val.NewValue != null)
                    SetScores(val.NewValue.Value.topScores, val.NewValue.Value.userScore);
            }, true);
            newProvider.RetrievalFailed += () => Schedule(() => SetErrorState(LeaderboardState.NetworkFailure));
            return null;
        }

        protected override LeaderboardScore CreateDrawableScore(ScoreInfo model, int index) => new LeaderboardScore(model, index, IsOnlineScope, Scope != BeatmapLeaderboardScope.Friend)
        {
            Action = () => ScoreSelected?.Invoke(model)
        };

        protected override LeaderboardScore CreateDrawableTopScore(ScoreInfo model) => new LeaderboardScore(model, model.Position, false, Scope != BeatmapLeaderboardScope.Friend)
        {
            Action = () => ScoreSelected?.Invoke(model)
        };

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            leaderboardProvider.Value?.Dispose();
            leaderboardProvider.Value = null;
        }
    }
}
