// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using osu.Game.Screens.Select.Leaderboards;
using Realms;

namespace osu.Game.Online.Leaderboards
{
    public partial class LeaderboardManager : Component
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        public ILeaderboardProvider GetLeaderboardFor(LeaderboardCriteria criteria)
        {
            switch (criteria.Scope)
            {
                case BeatmapLeaderboardScope.Local:
                    return new LocalLeaderboardProvider(realm, criteria);

                default:
                    return new OnlineLeaderboardProvider(api, rulesets, criteria);
            }
        }

        private class LocalLeaderboardProvider : ILeaderboardProvider
        {
            public IBindable<(IEnumerable<ScoreInfo> topScores, ScoreInfo? userScore)?> Scores => scores;
            private readonly Bindable<(IEnumerable<ScoreInfo>, ScoreInfo?)?> scores = new Bindable<(IEnumerable<ScoreInfo>, ScoreInfo?)?>();

            public event Action? RetrievalFailed;

            private readonly LeaderboardCriteria criteria;
            private readonly IDisposable scoreSubscription;

            public LocalLeaderboardProvider(RealmAccess realm, LeaderboardCriteria criteria)
            {
                this.criteria = criteria;
                scoreSubscription = realm.RegisterForNotifications(r =>
                    r.All<ScoreInfo>().Filter($"{nameof(ScoreInfo.BeatmapInfo)}.{nameof(BeatmapInfo.ID)} == $0"
                                              + $" AND {nameof(ScoreInfo.BeatmapInfo)}.{nameof(BeatmapInfo.Hash)} == {nameof(ScoreInfo.BeatmapHash)}"
                                              + $" AND {nameof(ScoreInfo.Ruleset)}.{nameof(RulesetInfo.ShortName)} == $1"
                                              + $" AND {nameof(ScoreInfo.DeletePending)} == false"
                        , criteria.Beatmap.ID, criteria.Ruleset.ShortName), localScoresChanged);
            }

            private void localScoresChanged(IRealmCollection<ScoreInfo> sender, ChangeSet? changes)
            {
                // This subscription may fire from changes to linked beatmaps, which we don't care about.
                // It's currently not possible for a score to be modified after insertion, so we can safely ignore callbacks with only modifications.
                if (changes?.HasCollectionChanges() == false)
                    return;

                var newScores = sender.AsEnumerable();

                if (criteria.ExactMods != null)
                {
                    if (!criteria.ExactMods.Any())
                    {
                        // we need to filter out all scores that have any mods to get all local nomod scores
                        newScores = newScores.Where(s => !s.Mods.Any());
                    }
                    else
                    {
                        // otherwise find all the scores that have all of the currently selected mods (similar to how web applies mod filters)
                        // we're creating and using a string HashSet representation of selected mods so that it can be translated into the DB query itself
                        var selectedMods = criteria.ExactMods.Select(m => m.Acronym).ToHashSet();

                        newScores = newScores.Where(s => selectedMods.SetEquals(s.Mods.Select(m => m.Acronym)));
                    }
                }

                newScores = newScores.Detach().OrderByTotalScore();

                scores.Value = (newScores, null);
            }

            public void Refetch()
            {
            }

            public void Dispose()
            {
                scoreSubscription.Dispose();
            }
        }

        private class OnlineLeaderboardProvider : ILeaderboardProvider
        {
            public IBindable<(IEnumerable<ScoreInfo> topScores, ScoreInfo? userScore)?> Scores => scores;
            private readonly Bindable<(IEnumerable<ScoreInfo>, ScoreInfo?)?> scores = new Bindable<(IEnumerable<ScoreInfo>, ScoreInfo?)?>();

            public event Action? RetrievalFailed;

            private readonly IAPIProvider api;
            private readonly RulesetStore rulesets;
            private readonly LeaderboardCriteria criteria;

            private GetScoresRequest? inFlightRequest;

            public OnlineLeaderboardProvider(IAPIProvider api, RulesetStore rulesets, LeaderboardCriteria criteria)
            {
                this.api = api;
                this.rulesets = rulesets;
                this.criteria = criteria;

                Refetch();
            }

            public void Refetch()
            {
                if (inFlightRequest != null)
                    return;

                IReadOnlyList<Mod>? requestMods = null;

                if (criteria.ExactMods != null)
                {
                    if (!criteria.ExactMods.Any())
                        // add nomod for the request
                        requestMods = new Mod[] { new ModNoMod() };
                    else
                        requestMods = criteria.ExactMods;
                }

                var newRequest = new GetScoresRequest(criteria.Beatmap, criteria.Ruleset, criteria.Scope, requestMods);
                newRequest.Success += response =>
                {
                    if (inFlightRequest != null && !newRequest.Equals(inFlightRequest))
                        return;

                    scores.Value =
                    (
                        response.Scores.Select(s => s.ToScoreInfo(rulesets, criteria.Beatmap)).OrderByTotalScore(),
                        response.UserScore?.CreateScoreInfo(rulesets, criteria.Beatmap)
                    );
                    inFlightRequest = null;
                };
                newRequest.Failure += _ => RetrievalFailed?.Invoke();
                api.Queue(inFlightRequest = newRequest);
            }

            public void Dispose()
            {
                inFlightRequest?.Cancel();
                inFlightRequest = null;
            }
        }
    }

    public record LeaderboardCriteria(
        BeatmapLeaderboardScope Scope,
        BeatmapInfo Beatmap,
        RulesetInfo Ruleset,
        Mod[]? ExactMods
    );
}
