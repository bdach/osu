// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
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

        /// <summary>
        /// Whether to apply the game's currently selected mods as a filter when retrieving scores.
        /// </summary>
        public Bindable<bool> FilterMods { get; set; } = new Bindable<bool>();

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private ILeaderboardScoreProvider? scoreProvider;

        protected override bool IsOnlineScope => Scope != BeatmapLeaderboardScope.Local;

        protected override APIRequest? FetchScores(CancellationToken cancellationToken)
        {
            (scoreProvider as Component)?.RemoveAndDisposeImmediately();
            scoreProvider = null;

            var fetchBeatmapInfo = BeatmapInfo;

            if (fetchBeatmapInfo == null)
            {
                SetErrorState(LeaderboardState.NoneSelected);
                return null;
            }

            var fetchRuleset = ruleset.Value ?? fetchBeatmapInfo.Ruleset;

            if (Scope == BeatmapLeaderboardScope.Local)
            {
                var localScoreProvider = new LocalLeaderboardScoreProvider
                {
                    Beatmap = { Value = BeatmapInfo! },
                    Ruleset = { BindTarget = ruleset },
                    ModFilterActive = { BindTarget = FilterMods },
                    Mods = { BindTarget = mods },
                };
                localScoreProvider.Scores.BindCollectionChanged((e, _) => SetScores((IEnumerable<ScoreInfo>)e!));
                AddInternal(localScoreProvider);
                scoreProvider = localScoreProvider;
                return null;
            }

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

            if (fetchBeatmapInfo.OnlineID <= 0 || fetchBeatmapInfo.Status <= BeatmapOnlineStatus.Pending)
            {
                SetErrorState(LeaderboardState.BeatmapUnavailable);
                return null;
            }

            if (!api.LocalUser.Value.IsSupporter && (Scope != BeatmapLeaderboardScope.Global || FilterMods.Value))
            {
                SetErrorState(LeaderboardState.NotSupporter);
                return null;
            }

            var onlineScoreProvider = new OnlineLeaderboardScoreProvider(Scope)
            {
                Beatmap = { Value = BeatmapInfo! },
                Ruleset = { BindTarget = ruleset },
                ModFilterActive = { BindTarget = FilterMods },
                Mods = { BindTarget = mods },
            };
            onlineScoreProvider.Success += SetScores;
            onlineScoreProvider.Failure += () => SetErrorState(LeaderboardState.NetworkFailure);
            AddInternal(onlineScoreProvider);
            scoreProvider = onlineScoreProvider;
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
    }
}
